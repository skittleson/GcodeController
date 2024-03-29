﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GcodeController.Services {

    public interface IDeviceService {
        Task<bool> OpenAsync(string port, int baudRate);
        void Close();
        Task<Guid> WriteAsync(string command, bool responseRequired = false);
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
        Task<string> CommandResponseAsync(string command);
        string[] GetPorts();
    }

    public class DeviceService : IDeviceService, IDisposable {
        private SerialPort _serialPort;
        private readonly ILogger<DeviceService> _logger;
        public string PortName => _serialPort?.PortName ?? string.Empty;
        public int BaudRate => _serialPort?.BaudRate ?? 0;
        public bool IsOpen => _serialPort?.IsOpen ?? false;
        public Channel<KeyValuePair<Guid, string>> RequestChannel {
            get; private set;
        }
        public MemoryCache ResponseCache {
            get;
        }

        private Guid _statusId = new Guid("79bbad7a-3f92-4ea8-ad3a-84cfc4ce1d7a");

        public DeviceService(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<DeviceService>();
            RequestChannel = Channel.CreateBounded<KeyValuePair<Guid, string>>(1);
            ResponseCache = new MemoryCache(new MemoryCacheOptions());
            Background();
        }

        public void Close() {
            if (_serialPort != null && _serialPort.IsOpen) {
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                _serialPort.Close();
            }
        }

        public string[] GetPorts() => SerialPort.GetPortNames();

        public async Task<bool> OpenAsync(string port, int baudRate) {
            Close();
            _serialPort = new SerialPort(port, baudRate);
            try {
                _serialPort.ReadTimeout = 10000;
                _serialPort.WriteTimeout = 10000;
                _serialPort.Open();
                await Task.Delay(TimeSpan.FromSeconds(1));

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
            Close();
            _serialPort?.Dispose();
        }

        public void Background() {
            Task.Run(async () => {
                while (true) {
                    while (await RequestChannel.Reader.WaitToReadAsync()) {
                        var kv = await RequestChannel.Reader.ReadAsync();
                        var response = await ProcessCommand(kv);
                        if (response.Key != Guid.Empty) {
                            ResponseCache.Set(response.Key, response.Value, TimeSpan.FromSeconds(30));
                        }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            });
        }

        public async Task<Guid> WriteAsync(string command, bool responseRequired = false) {
            var id = responseRequired ? Guid.NewGuid() : Guid.Empty;

            // special for certain commands to prevent calling too many times
            //if (command.Trim() == "?") {
            //    id = _statusId;
            //}
            var kv = new KeyValuePair<Guid, string>(id, command);
            await RequestChannel.Writer.WriteAsync(kv);
            return kv.Key;
        }

        public async Task<string> CommandResponseAsync(string command) {
            var id = await WriteAsync(command, true);
            var ct = new CancellationTokenSource(5000).Token;
            return await WaitForResponseAsync(id, ct);
        }

        private async Task<string> WaitForResponseAsync(Guid id, CancellationToken cancellationToken) {
            if (ResponseCache.TryGetValue<string>(id, out var response)) {
                return response;
            }
            await Task.Delay(500, cancellationToken);
            return await WaitForResponseAsync(id, cancellationToken);
        }

        public async Task<KeyValuePair<Guid, string>> ProcessCommand(KeyValuePair<Guid, string> idCommandKv) {
            if (!_serialPort.IsOpen) {
                throw new Exception("Not Open");
            }
            var command = idCommandKv.Value;
            var bytes = Encoding.UTF8.GetBytes(command + "\n");
            if (command.Equals("\\u0018")) {
                bytes = Encoding.UTF8.GetBytes("\u0018"); //Ctrl+x to reset
            }
            var result = string.Empty;
            var ct = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try {

                // https://stackoverflow.com/a/12651059/2414540
                // Time to send = bytes x bits_per_character / bits_per_second
                await _serialPort.BaseStream.WriteAsync(bytes.AsMemory(0, bytes.Length), ct.Token);

                // This makes many assumptions but its safe that the entire response will be back within this time
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct.Token);
                var buffer = new byte[4096];
                await _serialPort.BaseStream.ReadAsync(buffer.AsMemory(0, _serialPort.BytesToRead), ct.Token);
                result = Encoding.Default.GetString(buffer).Replace("\u0000", string.Empty).Trim();
            } catch (TaskCanceledException _) {
                _logger.LogWarning("command has timed out on: " + command);
            } catch (Exception ex) {
                _logger.LogCritical(ex, "Unable to get a success response: ");
            }
            return new KeyValuePair<Guid, string>(idCommandKv.Key, result.Trim());
        }
    }
}
