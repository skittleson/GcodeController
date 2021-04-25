using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GcodeController.GcodeFirmwares {
    public class GrblFirmware : GcodeFirmwareBase {
        private ILogger<GrblFirmware> _logger;

        public GrblFirmware(ILoggerFactory loggerFactory) {
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

        public override void Stop() {
            throw new NotImplementedException();
        }
    }
}
