using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using GcodeController.RequestResponseDTOs;
using HttpMultipartParser;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GcodeController.web {

    public class ApiController : WebApiController {
        private readonly ILogger _logger;
        private readonly ISerialDevice _serialDevice;
        private readonly IJobFileService _jobFileService;
        private readonly IJobRunnerService _jobRunnerService;

        public ApiController(ILoggerFactory loggerFactory, ISerialDevice serialDevice, IJobFileService jobFileService, IJobRunnerService jobRunnerService) : base() {
            _logger = loggerFactory.CreateLogger<ApiController>();
            _serialDevice = serialDevice;
            _jobFileService = jobFileService;
            _jobRunnerService = jobRunnerService;
        }

        [Route(HttpVerbs.Get, "/ping")]
        public async Task<string> GetPong() {
            await Task.Delay(500);
            return "pong";
        }

        [Route(HttpVerbs.Get, "/serial")]
        public CreateNewSerialRequest GetSerialConnection() {
            return new GetSerialResponse {
                Port = _serialDevice.PortName,
                BaudRate = _serialDevice.BaudRate,
                IsOpen = _serialDevice.IsOpen
            };
        }

        [Route(HttpVerbs.Post, "/serial")]
        public async Task<bool> CreateSerialConnectionAsync() {
            var data = await HttpContext.GetRequestDataAsync<CreateNewSerialRequest>();

            return _serialDevice.Open(data.Port, data.BaudRate);
        }

        [Route(HttpVerbs.Put, "/serial")]
        public async Task<SendSerialResponse> SendSerialCommandsAsync() {
            var data = await HttpContext.GetRequestDataAsync<SendSerialRequest>();
            var serialResponse = await _serialDevice.SendAsync(data.Command);
            _logger.LogInformation(serialResponse);
            return new SendSerialResponse {
                Command = data.Command,
                Timestamp = DateTime.Now,
                Message = serialResponse
            };
        }

        [Route(HttpVerbs.Post, "/files")]
        public async Task UploadFile() {
            var parser = await MultipartFormDataParser.ParseAsync(Request.InputStream);
            var file = parser.Files.FirstOrDefault();
            if (file is null) {
                throw new ArgumentNullException("No file uploaded");
            }
            _jobFileService.Save(file.Data, file.FileName);
        }

        [Route(HttpVerbs.Get, "/files")]
        public string[] GetFiles() => _jobFileService.List();

        [Route(HttpVerbs.Delete, "/files")]
        public async Task DeleteFile() {
            var data = await HttpContext.GetRequestDataAsync<DeleteFileRequest>();
            if (data is null || string.IsNullOrEmpty(data.Name)) {
                throw new ArgumentNullException(nameof(data.Name));
            }

            // Cannot delete if job is currently using file
            if (!(_jobRunnerService.FileName == data.Name
                && (_jobRunnerService.State == JobStates.Stop || _jobRunnerService.State == JobStates.Complete))) {
                _jobFileService.Delete(data.Name);
            }
        }

        [Route(HttpVerbs.Post, "/job")]
        public async Task StartJob() {
            var data = await HttpContext.GetRequestDataAsync<CreateNewJobRequest>();
            if (data != null && !string.IsNullOrEmpty(data.Name))
                _jobRunnerService.StartJob(data.Name);
        }

        [Route(HttpVerbs.Delete, "/job")]
        public void StopJob() => _jobRunnerService.StopJob();

        [Route(HttpVerbs.Put, "/job")]
        public void PauseJob() => _jobRunnerService.PauseJob();

        [Route(HttpVerbs.Get, "/job")]
        public async Task<JobStatusResponse> GetJob() {
            var queryParams = HttpContext.GetRequestQueryData();

            // long polling
            if (int.TryParse(queryParams.Get("percentage"), out var clientLastPercentage)) {
                var status = getJobStatus();
                var timeout = 10;
                while (status.Percentage <= clientLastPercentage
                    && status.State == JobStates.Running) {
                    await Task.Delay(500);
                    status = getJobStatus();
                    if (timeout <= 0) { break; }
                    timeout--;
                }
                return status;
            } else {
                return getJobStatus();
            }
            JobStatusResponse getJobStatus() {
                return new JobStatusResponse {
                    Percentage = _jobRunnerService.CompletePercentage,
                    State = _jobRunnerService.State,
                    FileName = _jobRunnerService.FileName
                };
            }
        }
    }

    public class DeleteFileRequest {
        public string Name {
            get; set;
        }
    }

    public class CreateNewJobRequest {
        public string Name {
            get; set;
        }
    }

    public class JobStatusResponse {
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
}
