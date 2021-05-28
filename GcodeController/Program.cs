using GcodeController.Channels;
using GcodeController.Handlers;
using GcodeController.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PubSub;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GcodeController {
    internal class Program {

        private static void Main(string[] args) {
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

            // Move connect when on configuration mqtt ip address is added.
            //serviceProvider.GetService<MqttChannel>().Connect(System.Net.IPAddress.Parse("127.0.0.1"));
            Task.Run(async () => {
                await serviceProvider.GetService<IEmbededWebServer>().Start(serviceProvider);
            }, ct.Token);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
            serviceProvider.Dispose();
        }
    }
}
