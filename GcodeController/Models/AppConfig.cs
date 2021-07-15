namespace GcodeController.Models {

    public enum AppConfigLocationType {
        Default = 0,
        CurrentDirectory = 1,
        UserDirectory = 2
    }
    public class AppConfig {
        public string MqttServer {
            get; set;
        }

        public int Port {
            get; set;
        }

        public AppConfigLocationType LocationType {
            get; set;
        }

        public string WebcamUrl {
            get; set;
        }
    }
}
