using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GcodeController {

    public interface ISerialDevice {

        Task<bool> OpenAsync(string port, int baudRate);

        void Close();

        Task<Guid> WriteAsync(string command);

        string PortName {
            get;
        }

        int BaudRate {
            get;
        }

        bool IsOpen {
            get;
        }
        Channel<KeyValuePair<Guid, string>> RequestChannel {
            get;
        }

        Channel<KeyValuePair<Guid, string>> ResponseChannel {
            get;
        }

        Task<KeyValuePair<Guid, string>> GetResponseAsync(Guid id);

        string[] GetPorts();
    }

    public class SerialDevice : ISerialDevice, IDisposable {
        private SerialPort _serialPort;
        private readonly ILogger<SerialDevice> _logger;
        public string PortName => _serialPort?.PortName;
        public int BaudRate => _serialPort?.BaudRate ?? 0;
        public bool IsOpen => _serialPort?.IsOpen ?? false;
        public Channel<KeyValuePair<Guid, string>> RequestChannel {
            get; private set;
        }

        public Channel<KeyValuePair<Guid, string>> ResponseChannel {
            get; private set;
        }

        public SerialDevice(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<SerialDevice>();
            RequestChannel = Channel.CreateBounded<KeyValuePair<Guid, string>>(1);
            ResponseChannel = Channel.CreateUnbounded<KeyValuePair<Guid, string>>();
            Background();
        }

        public void Close() {
            _serialPort?.DiscardInBuffer();
            _serialPort?.DiscardOutBuffer();
            _serialPort?.Close();
        }

        public string[] GetPorts() {
            return SerialPort.GetPortNames();
        }

        public async Task<bool> OpenAsync(string port, int baudRate) {
            Close();
            _serialPort = new SerialPort(port, baudRate);
            try {
                _serialPort.Open();
                await Task.Delay(100);

                // Wake up!
                await WriteAsync("\n");
                _logger.LogInformation($"Connected {port} @ {baudRate}");
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

        public void Background() {
            Task.Run(async () => {
                while (true) {
                    while (await RequestChannel.Reader.WaitToReadAsync()) {
                        var kv = await RequestChannel.Reader.ReadAsync();
                        await ResponseChannel.Writer.WriteAsync(await ProcessCommand(kv));
                    }
                    await Task.Delay(1000);
                }
            });
        }

        public async Task<Guid> WriteAsync(string command) {
            var kv = new KeyValuePair<Guid, string>(Guid.NewGuid(), command);
            await RequestChannel.Writer.WriteAsync(kv);
            return kv.Key;
        }

        public async Task<KeyValuePair<Guid, string>> GetResponseAsync(Guid id) {
            var response = new KeyValuePair<Guid, string>();
            while (response.Key != id) {
                await ResponseChannel.Reader.WaitToReadAsync();
                response = await ResponseChannel.Reader.ReadAsync();
            }
            return response;
        }

        public async Task<KeyValuePair<Guid, string>> ProcessCommand(KeyValuePair<Guid, string> idCommandKv) {
            if (!_serialPort.IsOpen) {
                throw new Exception("Not Open");
            }
            var command = idCommandKv.Value;
            var bytes = Encoding.ASCII.GetBytes(command + "\n");
            if (command.Equals("\\u0018")) {
                bytes = Encoding.ASCII.GetBytes("\u0018"); //Ctrl+x to reset
            }
            _serialPort.Write(bytes, 0, bytes.Length);
            var response = new StringBuilder();
            var counter = 0;
            for (var i = 0; i < 50; i++) {
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
            return new KeyValuePair<Guid, string>(idCommandKv.Key, response.ToString().Trim());
        }
    }
}
