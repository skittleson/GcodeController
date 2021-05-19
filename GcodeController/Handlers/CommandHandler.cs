using GcodeController.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Threading.Tasks;

namespace GcodeController.Handlers {

    public interface ICommandHandler {
        Task<string> ExecuteAsync(CommandRequest commandRequest);

    }
    public class CommandHandler : AHandler, ICommandHandler {
        public const string PREFIX = "cmnd";
        public override string GetPrefix => PREFIX;
        private readonly IDeviceService _serialDevice;
        private readonly ILogger<SerialHandler> _logger;

        public CommandHandler(ILoggerFactory loggerFactory, IDeviceService serialDevice) {
            _serialDevice = serialDevice;
            _logger = loggerFactory.CreateLogger<SerialHandler>();
        }
        public async Task<string> ExecuteAsync(CommandRequest commandRequest) {
            if (_serialDevice.PortName.Equals(commandRequest?.Port, System.StringComparison.OrdinalIgnoreCase)) {
                return await _serialDevice.CommandResponseAsync(commandRequest.Command);
            }
            _logger.LogWarning("No matching device connected");
            return string.Empty;
        }
    }

    public class CommandRequest {
        public string Port {
            get; set;
        }

        [Description("Command to execute on serial device")]
        public string Command {
            get; set;
        }
    }

}
