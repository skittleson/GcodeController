using System;

namespace GcodeController.RequestResponseDTOs {

    public class CreateNewSerialRequest {

        public string Port {
            get; set;
        }

        public int BaudRate {
            get; set;
        }
    }

    public class GetSerialResponse : CreateNewSerialRequest {

        public bool IsOpen {
            get; set;
        }
        public string[] Ports {
            get;
            internal set;
        }
    }

    public class SendSerialRequest {

        public string Command {
            get; set;
        }
    }

    public class SendSerialResponse : SendSerialRequest {
        public string Message {
            get; set;
        }

        public DateTime Timestamp {
            get; set;
        }
    }
}
