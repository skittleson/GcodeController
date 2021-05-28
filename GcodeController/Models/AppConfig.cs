using System;

namespace GcodeController.Models {

    public enum AppConfigLocationType {
        Default,
        CurrentDirectory,
        UserDirectory
    }

    public interface IAppConfig {
        Uri MqttServer {
            get;
        }

        int Port {
            get;
        }

        AppConfigLocationType LocationType {
            get;
        }
    }
    public class AppConfig : IAppConfig {
        public Uri MqttServer {
            get; set;
        }

        public int Port {
            get; set;
        }

        public AppConfigLocationType LocationType {
            get; set;
        }
    }
}
