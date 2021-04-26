using GcodeController.GcodeFirmwares;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GcodeController.Tests {
    public class GrblFirmwareTests {
        private IGcodeFirmware _grblDevice;

        public GrblFirmwareTests() {
            _grblDevice = new GrblFirmware(new NullLoggerFactory());
        }

        [Fact]
        public void Can_Determine_Device_IsBusy() {
            var actual = _grblDevice.IsBusy("<Run,MPos:597.664,240.000,32.000,WPos:587.664,240.000,30.000>\nok");
            Assert.True(actual);
        }
    }
}
