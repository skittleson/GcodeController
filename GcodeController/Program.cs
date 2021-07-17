using GcodeController.Channels;
using GcodeController.Handlers;
using GcodeController.Models;
using GcodeController.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PubSub;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GcodeController {
    internal class Program {

        private static async Task Main(string[] args) {
            var ct = new CancellationTokenSource();
            var serviceProvider = new ServiceCollection()
               .AddLogging(configure => {
                   configure.AddConsole();
                   configure.SetMinimumLevel(LogLevel.Debug);
               })
               .AddSingleton(typeof(CancellationToken), ct.Token)
               .AddSingleton(AppConfigFactory.GetConfig())
               .AddSingleton<IDeviceService, DeviceService>()
               .AddSingleton<IEmbededWebServer, EmbededWebServer>()
               .AddSingleton<IFileService, FileService>()
               .AddSingleton<IJobService, JobService>()
               .AddScoped<IFilesHandler, FilesHandler>()
               .AddScoped<ISerialHandler, SerialHandler>()
               .AddScoped<ICommandHandler, CommandHandler>()
               .AddSingleton<IEventHubService>(new EventHubService(Hub.Default))
               .AddSingleton<MqttChannel>()
               .BuildServiceProvider();
            Console.CancelKeyPress += (s, e) => {
                ct.Cancel();
                serviceProvider.GetService<IDeviceService>().Close();
            };
            var config = serviceProvider.GetService<AppConfig>();
            if (config is not null && config.MqttServer.Length > 0) {
                await serviceProvider.GetService<MqttChannel>().ConnectAsync(config.MqttServer);
            }

            await serviceProvider.GetService<IEmbededWebServer>().StartAsync(serviceProvider);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
            serviceProvider.Dispose();
        }
    }
}
