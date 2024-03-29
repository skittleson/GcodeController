﻿using GcodeController.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace GcodeController.Handlers {

    public interface ISerialHandler {
        IEnumerable<SerialResponse> List();
        SerialResponse Get(string port);
        SerialResponse Delete(string port);
        Task<bool> OpenAsync(CreateNewSerialRequest createNewSerialRequest);
    }

    public class SerialHandler : AHandler, ISerialHandler {
        public const string PREFIX = "serial";
        public override string GetPrefix => PREFIX;
        private IDeviceService _serialDevice;
        private ILogger<SerialHandler> _logger;

        public SerialHandler(ILoggerFactory loggerFactory, IDeviceService serialDevice) {
            _serialDevice = serialDevice;
            _logger = loggerFactory.CreateLogger<SerialHandler>();
        }

        public SerialResponse Get(string port) {
            if (_serialDevice.PortName.Equals(port, StringComparison.OrdinalIgnoreCase)) {
                return new SerialResponse {
                    Port = _serialDevice.PortName,
                    BaudRate = _serialDevice.BaudRate,
                    IsOpen = _serialDevice.IsOpen
                };
            }
            return new SerialResponse { Port = port };
        }

        public async Task<bool> OpenAsync(CreateNewSerialRequest createNewSerialRequest) =>
            await _serialDevice.OpenAsync(createNewSerialRequest.Port, createNewSerialRequest.BaudRate);

        public IEnumerable<SerialResponse> List() => _serialDevice.GetPorts().Select(x => Get(x)).ToArray();

        public SerialResponse Delete(string port) {
            if (port.Equals(_serialDevice.PortName, StringComparison.OrdinalIgnoreCase)) {
                _serialDevice.Close();
            }
            return Get(port);
        }
    }


    [Description("Request to create a serial connection.")]
    public class CreateNewSerialRequest {

        [Description("The port on the device. Usually COMX or ttyUSBX")]
        public string Port {
            get; set;
        }

        [Description("Serial connection baud rates")]
        public int BaudRate {
            get; set;
        }

    }

    public class SerialResponse : CreateNewSerialRequest {

        [Description("Returns if connection is connected")]
        public bool IsOpen {
            get; set;
        }
    }

    public class SendSerialRequest {

        public string Command {
            get; set;
        }
    }
}
