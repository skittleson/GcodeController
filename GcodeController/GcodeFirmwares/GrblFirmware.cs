using Microsoft.Extensions.Logging;
using System;

namespace GcodeController.GcodeFirmwares {
    public class GrblFirmware : GcodeFirmwareBase {
        private ILogger<GrblFirmware> _logger;

        public GrblFirmware(ILoggerFactory loggerFactory) : base() {
            _logger = loggerFactory.CreateLogger<GrblFirmware>();
        }
        public override bool EndOfCommand(string line) {
            var error = line.StartsWith("error:", StringComparison.OrdinalIgnoreCase);
            if (line.EndsWith("ok", StringComparison.OrdinalIgnoreCase) || error) {
                if (error) {
                    _logger.LogWarning(line);
                }
                return true;
            }
            return false;
        }

        public override bool IsBusy(string data) {
            var startIndex = data.IndexOf('<') + 1;
            var firstCommandDelimitedIndex = data.IndexOf(',');
            return data[startIndex..firstCommandDelimitedIndex].Equals("Run", StringComparison.OrdinalIgnoreCase);
        }

        public override void Stop() {
            throw new NotImplementedException();
        }
    }
}
