using GcodeController.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GcodeController.Services {

    public class AppConfigFactory {
        public const string CONFIG_FILE = "gcode.json";
        private static string LocalDirectory => Directory.GetCurrentDirectory();
        private static string UserProfileDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static AppConfig GetConfig() {
            var localConfig = TryGetConfig(LocalDirectory);
            if (localConfig is not null) return localConfig;
            var userConfig = TryGetConfig(UserProfileDirectory);
            if (userConfig is not null) return userConfig;
            var defaultConfig = new AppConfig {
                Port = 8081,
                LocationType = AppConfigLocationType.Default,
                WebcamUrl = string.Empty,
                MqttServer = string.Empty
            };
            return defaultConfig;
        }

        public async static Task SaveConfigAsync(AppConfig config) {
            if (config.LocationType == AppConfigLocationType.Default) {
                config.LocationType = AppConfigLocationType.CurrentDirectory;
            }
            var markedLocalDirectoryFileDelete = false;
            if (config.LocationType == AppConfigLocationType.UserDirectory) {
                markedLocalDirectoryFileDelete = File.Exists(Path.Combine(LocalDirectory, CONFIG_FILE));
            }
            var saveConfigToFilePath = config.LocationType == AppConfigLocationType.UserDirectory ? UserProfileDirectory : LocalDirectory;
            var saveConfigToFile = Path.Combine(saveConfigToFilePath, CONFIG_FILE);
            var appConfig = new AppConfigSectionModel(config);
            var jsonText = JsonSerializer.Serialize(appConfig, Utils.JsonOptions());
            await File.WriteAllTextAsync(saveConfigToFile, jsonText);
            if (markedLocalDirectoryFileDelete) {
                File.Delete(Path.Combine(LocalDirectory, CONFIG_FILE));
            }
        }

        private static AppConfig? TryGetConfig(string path) {

            // reload on change would be nice but restarting app be would b3eter
            var builder = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile(CONFIG_FILE, optional: true, reloadOnChange: false);
            var config = builder.Build();
            if (config is null) {
                return null;
            }
            var appConfig = config.GetSection(nameof(AppConfig)).Get<AppConfig>();
            return appConfig;
        }

        internal class AppConfigSectionModel {
            public AppConfigSectionModel(AppConfig config) {
                AppConfig = config;
            }
            public AppConfig? AppConfig {
                get;
            }
        }
    }
}
