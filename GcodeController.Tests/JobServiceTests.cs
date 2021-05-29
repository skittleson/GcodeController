using GcodeController.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading;

namespace GcodeController.Tests {
    public class JobServiceTests : IDisposable {
        private Mock<IFileService> _fileServiceMock;
        private Mock<IDeviceService> _device;
        private Mock<IEventHubService> _hubService;
        private CancellationToken _cancelToken;
        private JobService _jobService;

        public JobServiceTests() {

            _fileServiceMock = new Mock<IFileService>(MockBehavior.Strict);
            _device = new Mock<IDeviceService>(MockBehavior.Strict);
            _hubService = new Mock<IEventHubService>(MockBehavior.Strict);
            _cancelToken = new CancellationTokenSource().Token;
            _jobService = new JobService(new NullLoggerFactory(), _fileServiceMock.Object, _device.Object, _hubService.Object, _cancelToken);

            /*
             * G17 G20 G90 G94 G54
G0 Z0.25
X-0.5 Y0.
Z0.1
G01 Z0. F5.
G02 X0. Y0.5 I0.5 J0. F2.5
X0.5 Y0. I0. J-0.5
X0. Y-0.5 I-0.5 J0.
X-0.5 Y0. I0. J0.5
G01 Z0.1 F5.
G00 X0. Y0. Z0.25
             */
        }

        public void Can_start_job() {

            // Arrange

            // Act
            _jobService.StartJob("foo.nc");

            // Assert

        }

        public void Dispose() {
            throw new NotImplementedException();
        }
    }
}
