using Easy.Common.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GcodeController {

    public interface IJobRunnerService {

        void StartJob(string filename);

        void PauseJob();

        void StopJob();

        int CompletePercentage {
            get;
        }
        JobStates State {
            get;
        }
        string FileName {
            get;
        }
    }

    public enum JobStates {
        Stop = 0,
        Stopping = 1,
        Running = 2,
        Pause = 3,
        Complete = 4
    }

    public class JobRunnerService : IJobRunnerService, IDisposable {
        private readonly ILogger<JobRunnerService> _logger;
        private readonly IJobFileService _jobFileService;
        private readonly ISerialDevice _serialDevice;
        private FileStream _fileStream;
        private long _linesTotal;
        private long _linesAt;
        private readonly Thread _backgroundProcessThread;
        public JobStates State {
            get; private set;
        }
        public int CompletePercentage {
            get {
                if (_linesTotal < 1) { return 0; }
                return (int)((double)_linesAt / _linesTotal * 100);
            }
        }
        public string FileName => Path.GetFileName(_fileStream?.Name ?? "");

        public JobRunnerService(ILoggerFactory loggerFactory, IJobFileService jobFileService, ISerialDevice serialDevice) {
            _logger = loggerFactory.CreateLogger<JobRunnerService>();
            _jobFileService = jobFileService;
            _serialDevice = serialDevice;
            _fileStream = null;
            State = JobStates.Stop;
            _backgroundProcessThread = new Thread(async () => await Running()) { IsBackground = true };
            _backgroundProcessThread.Start();
        }

        public void PauseJob() {
            if (State == JobStates.Pause) {
                State = JobStates.Running;
                _logger.LogInformation("Job Unpaused");
                return;
            }
            State = JobStates.Pause;
            _logger.LogInformation("Job Paused");
        }

        public void StartJob(string filename) {
            if (_fileStream is null) {
                _fileStream = _jobFileService.Get(filename);
                State = JobStates.Running;
                _linesTotal = 0;
                _linesAt = 0;
                _logger.LogInformation($"Job Started {filename}");
            } else {
                _logger.LogInformation($"Cannot start new job while one is running.");
            }
        }

        public void StopJob() {
            State = JobStates.Stopping;
        }

        private async Task Running() {

            //  Background thread is always running
            while (true) {
                if (_fileStream != null) {
                    _linesTotal = _fileStream.CountLines();
                    _fileStream.Position = 0;
                    using var reader = new StreamReader(_fileStream, Encoding.UTF8);
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null) {
                        line = line.Trim();
                        ++_linesAt;
                        if (line.StartsWith("(") || line.StartsWith("%")) {
                            continue;
                        }
                        await _serialDevice.SendAsync(line);
                        if (State == JobStates.Pause) {
                            await Task.Delay(100);
                        } else if (State == JobStates.Stopping) {
                            State = JobStates.Stop;
                            break;
                        }
                    }
                    State = JobStates.Complete;
                    _fileStream.Close();
                    _logger.LogInformation($"Job Completed {Path.GetFileName(_fileStream.Name)}");
                    _fileStream = null;
                    await Task.Delay(500);
                }
            }
        }

        public void Dispose() {
            StopJob();
            _backgroundProcessThread?.Abort();
        }
    }
}
