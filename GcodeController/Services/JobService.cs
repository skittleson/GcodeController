﻿using Easy.Common.Extensions;
using GcodeController.GcodeFirmwares;
using GcodeController.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GcodeController {

    public interface IJobService {
        JobInfo GetJobs();
        JobInfo StartJob(string filename);
        JobInfo PauseJob();
        JobInfo StopJob();
    }

    public enum JobStates {
        Stop = 0,
        Stopping = 1,
        Running = 2,
        Pause = 3,
        Complete = 4
    }

    public class JobInfo {
        public int Percentage {
            get; set;
        }
        public JobStates State {
            get; set;
        }
        public string? FileName {
            get; set;
        }
        public long Elapsed {
            get; set;
        }
    }

    public class JobService : IJobService, IDisposable {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<JobService> _logger;
        private readonly IFileService _fileService;
        private readonly IDeviceService _deviceService;
        private FileStream? _fileStream;
        private long _linesTotal;
        private long _linesAt;
        public JobStates State {
            get; private set;
        }
        public const string PREFIX = "jobs";
        private readonly IEventHubService _hubService;
        private readonly CancellationToken _cancellationToken;

        private JobInfo CreateJobInfo() {
            return new JobInfo() {
                Percentage = CompletePercentage,
                State = State,
                FileName = FileName,
                Elapsed = (long)((JobEnd ?? DateTime.UtcNow) - (JobStart ?? DateTime.UtcNow)).TotalSeconds
            };
        }

        public int CompletePercentage {
            get {
                if (_linesTotal < 1) { return 0; }
                return (int)((double)_linesAt / _linesTotal * 100);
            }
        }
        public string FileName => Path.GetFileName(_fileStream?.Name ?? "");

        public DateTime? JobStart;
        public DateTime? JobEnd;

        public JobService(ILoggerFactory loggerFactory, IFileService fileService, IDeviceService deviceService, IEventHubService hubService, CancellationToken cancellationToken) {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<JobService>();
            _fileService = fileService;
            _deviceService = deviceService;
            _fileStream = null;
            _hubService = hubService;
            _cancellationToken = cancellationToken;
            State = JobStates.Stop;
            Task.Run(async () => {
                try {
                    await Background();
                } catch (Exception ex) {
                    _logger.LogError(ex.Message);
                    StopJob();
                }
            }, _cancellationToken);
        }

        public JobInfo PauseJob() {
            if (State == JobStates.Pause) {
                State = JobStates.Running;
                _logger.LogInformation("Job Unpaused");
                return _hubService.Publish(CreateJobInfo());
            }
            State = JobStates.Pause;
            _logger.LogInformation("Job Paused");
            return _hubService.Publish(CreateJobInfo());
        }

        public JobInfo StartJob(string filename) {
            if (_fileStream is null) {
                _fileStream = _fileService.Get(filename).GetStream();
                State = JobStates.Running;
                _linesTotal = 0;
                _linesAt = 0;
                _logger.LogInformation($"Job Started {filename}");
            } else {
                _logger.LogInformation($"Cannot start new job while one is running.");
            }
            return _hubService.Publish(CreateJobInfo());
        }

        public JobInfo StopJob() {
            State = JobStates.Stopping;
            return _hubService.Publish(CreateJobInfo());
        }

        private async Task Background() {
            while (!_cancellationToken.IsCancellationRequested) {
                if (_fileService is null) {
                    await Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken);
                    continue;
                }
                _linesTotal = _fileStream.CountLines();
                _fileStream.Position = 0;
                JobStart = DateTime.UtcNow;
                JobEnd = null;
                var firmware = new GrblFirmware(_loggerFactory);
                using var reader = new StreamReader(_fileStream, Encoding.UTF8);
                string line;
                var progress = 0;
                var verifyMoveCommandCheckpoint = 0;
                while ((line = await reader.ReadLineAsync()) != null) {
                    line = line.Trim();
                    ++_linesAt;
                    await _deviceService.WriteAsync(line);
                    if (verifyMoveCommandCheckpoint >= 10) {
                        _hubService.Publish(CreateJobInfo());

                        // IsBusy is blocking.
                        while (firmware.IsBusy(await _deviceService.CommandResponseAsync("?"))) {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }
                        verifyMoveCommandCheckpoint = 0;
                        _hubService.Publish(CreateJobInfo());
                    }
                    while (State == JobStates.Pause) {
                        await Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken);
                    }
                    if (State == JobStates.Stopping) {
                        State = JobStates.Stop;
                        _hubService.Publish(CreateJobInfo());
                        break;
                    }
                    if (progress != CompletePercentage) {
                        progress = CompletePercentage;
                        _hubService.Publish(CreateJobInfo());
                    }
                }
                State = JobStates.Complete;
                _fileStream.Close();
                _logger.LogInformation($"Job Completed {Path.GetFileName(_fileStream.Name)}");
                _hubService.Publish(CreateJobInfo());
                _fileStream = null;
                JobEnd = DateTime.UtcNow;                
            }
        }

        public void Dispose() => StopJob();

        public JobInfo GetJobs() => CreateJobInfo();
    }
}
