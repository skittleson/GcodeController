using Microsoft.Extensions.Logging;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace GcodeController {

    public interface ISerialDevice {

        Task<bool> OpenAsync(string port, int baudRate);

        void Close();

        Task<string> SendAsync(string command);

        string PortName {
            get;
        }

        int BaudRate {
            get;
        }

        bool IsOpen {
            get;
        }
    }

    /// <summary>
    /// SRP: Maintain serial communications
    /// </summary>
    public class SerialDevice : ISerialDevice, IDisposable {
        private SerialPort _serialPort;
        private readonly ILogger<SerialDevice> _logger;
        public string PortName => _serialPort?.PortName;
        public int BaudRate => _serialPort?.BaudRate ?? 0;
        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public SerialDevice(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<SerialDevice>();
        }

        public void Close() {
            Dispose();
        }

        public async Task<bool> OpenAsync(string port, int baudRate) {
            _serialPort?.Close();
            _serialPort = new SerialPort(port, baudRate);
            try {
                _serialPort.Open();
                await Task.Delay(100);

                // Wake up!
                _serialPort.WriteLine("\r\n\r\n");
                _logger.LogInformation("Connected");
            } catch {
                _logger.LogWarning($"Unable to open port {port} @ {baudRate}");
                return false;
            }
            return _serialPort?.IsOpen ?? false;
        }

        public void Dispose() {
            _serialPort?.Close();
            _serialPort?.Dispose();
        }

        public async Task<string> SendAsync(string command) {
            if (!_serialPort.IsOpen) {
                throw new Exception("Not Open");
            }
            var bytes = Encoding.UTF8.GetBytes(command + "\n");
            _serialPort.Write(bytes, 0, bytes.Length);
            var response = new StringBuilder();
            var counter = 0;
            for (var i = 0; i < 100; i++) {
                ++counter;
                await Task.Delay(100);
                var line = _serialPort.ReadExisting().Trim();
                if (!string.IsNullOrEmpty(line)) {
                    response.AppendLine(line);
                }
                var error = line.StartsWith("error:", StringComparison.OrdinalIgnoreCase);
                if (line.EndsWith("ok", StringComparison.OrdinalIgnoreCase) || error) {
                    if (error) {
                        _logger.LogWarning(line);
                    }
                    break;
                }
            }
            return response.ToString().Trim();
        }
    }
}
