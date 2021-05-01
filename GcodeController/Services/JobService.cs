using Easy.Common.Extensions;
using GcodeController.GcodeFirmwares;
using GcodeController.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
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
        public string FileName {
            get; set;
        }
    }

    public class JobService : IJobService, IDisposable {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<JobService> _logger;
        private readonly IFileService _fileService;
        private readonly IDeviceService _deviceService;
        private FileStream _fileStream;
        private long _linesTotal;
        private long _linesAt;
        public JobStates State {
            get; private set;
        }
        public const string PREFIX = "jobs";

        private readonly IEventHubService _hubService;

        private JobInfo JobInfoFactory() {
            return new JobInfo() {
                Percentage = CompletePercentage,
                State = State,
                FileName = FileName
            };
        }

        public int CompletePercentage {
            get {
                if (_linesTotal < 1) { return 0; }
                return (int)((double)_linesAt / _linesTotal * 100);
            }
        }
        public string FileName => Path.GetFileName(_fileStream?.Name ?? "");

        public JobService(ILoggerFactory loggerFactory, IFileService fileService, IDeviceService deviceService, IEventHubService hubService) {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<JobService>();
            _fileService = fileService;
            _deviceService = deviceService;
            _fileStream = null;
            _hubService = hubService;
            State = JobStates.Stop;
            Background();
        }

        public JobInfo PauseJob() {
            if (State == JobStates.Pause) {
                State = JobStates.Running;
                _logger.LogInformation("Job Unpaused");
                return _hubService.Publish(JobInfoFactory());
            }
            State = JobStates.Pause;
            _logger.LogInformation("Job Paused");
            return _hubService.Publish(JobInfoFactory());
        }

        public JobInfo StartJob(string filename) {
            if (_fileStream is null) {
                _fileStream = _fileService.Get(filename);
                State = JobStates.Running;
                _linesTotal = 0;
                _linesAt = 0;
                _logger.LogInformation($"Job Started {filename}");
            } else {
                _logger.LogInformation($"Cannot start new job while one is running.");
            }
            return _hubService.Publish(JobInfoFactory());
        }

        public JobInfo StopJob() {
            State = JobStates.Stopping;
            return _hubService.Publish(JobInfoFactory());
        }

        private void Background() {
            Task.Run(async () => {
                while (true) {
                    if (_fileStream != null) {
                        _linesTotal = _fileStream.CountLines();
                        _fileStream.Position = 0;
                        var firmware = new GrblFirmware(_loggerFactory);
                        using var reader = new StreamReader(_fileStream, Encoding.ASCII);
                        string line;
                        var progress = 0;
                        while ((line = await reader.ReadLineAsync()) != null) {
                            line = line.Trim();
                            ++_linesAt;
                            await _deviceService.WriteAsync(line);
                            while (firmware.IsBusy(await _deviceService.CommandResponseAsync("?"))) {
                                await Task.Delay(1000);
                            }
                            while (State == JobStates.Pause) {
                                await Task.Delay(1000);
                            }
                            if (State == JobStates.Stopping) {
                                State = JobStates.Stop;
                                _hubService.Publish(JobInfoFactory());
                                break;
                            }
                            if (progress != CompletePercentage) {
                                progress = CompletePercentage;
                                _hubService.Publish(JobInfoFactory());
                            }
                        }
                        State = JobStates.Complete;
                        _fileStream.Close();
                        _logger.LogInformation($"Job Completed {Path.GetFileName(_fileStream.Name)}");
                        _hubService.Publish(JobInfoFactory());
                        _fileStream = null;
                        await Task.Delay(500);
                    }
                }
            });
        }

        public void Dispose() => StopJob();

        public JobInfo GetJobs() => JobInfoFactory();
    }
}
