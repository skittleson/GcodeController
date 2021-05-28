using GcodeController.Models;
using Microsoft.Extensions.Configuration;
using System;


namespace GcodeController.Services {
    public class AppConfigFactory {
        public const string CONFIG_FILE = ".gcode-controller.json";

        public static IAppConfig GetConfig() {

            // Attempt to restore settings until defaults apply
            var localConfig = TryGetConfig(AppDomain.CurrentDomain.BaseDirectory);
            if (localConfig is not null) return localConfig;
            var userConfig = TryGetConfig(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            if (userConfig is not null) return userConfig;
            var config = new AppConfig {
                Port = 8081,
                LocationType = AppConfigLocationType.Default
            };
            return config;
        }

        public static void SaveConfig(IAppConfig config) {
            if (config.LocationType == AppConfigLocationType.Default) {

            }
        }

        private static IAppConfig? TryGetConfig(string path) {
            var config = new ConfigurationBuilder()
              .SetBasePath(path)
              .AddJsonFile(CONFIG_FILE, optional: true).Build();
            if (config is null) {
                return null;
            }
            var section = config.GetSection(nameof(AppConfig));
            if (section is null) {
                return null;
            }
            IAppConfig appConfig = section.Get<AppConfig>();
            return appConfig;
        }

    }
}
