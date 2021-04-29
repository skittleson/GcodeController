using GcodeController.Channels;
using GcodeController.Handlers;
using GcodeController.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PubSub;
using System;

namespace GcodeController {
    internal class Program {

        private static void Main(string[] args) {
            var serviceProvider = new ServiceCollection()
               .AddLogging(configure => {
                   configure.AddConsole();
                   configure.SetMinimumLevel(LogLevel.Debug);
               })
               .AddSingleton<IDeviceService, DeviceService>()
               .AddSingleton<IEmbededWebServer, EmbededWebServer>()
               .AddSingleton<IFileService, FileService>()
               .AddSingleton<IJobService, JobService>()
               .AddScoped<IFilesHandler, FilesHandler>()
               .AddScoped<ISerialHandler, SerialHandler>()
               .AddSingleton<IEventHubService>(new EventHubService(Hub.Default))
               .AddSingleton<MqttChannel>()
               .BuildServiceProvider();
            Console.CancelKeyPress += (s, e) => { serviceProvider.GetService<IDeviceService>().Close(); };

            // Move connect when on configuration mqtt ip address is added.
            serviceProvider.GetService<MqttChannel>().Connect(System.Net.IPAddress.Parse("127.0.0.1"));

            // This is blocking!  Use cancellation token to kill this
            serviceProvider.GetService<IEmbededWebServer>().Start(serviceProvider).GetAwaiter().GetResult();
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
            serviceProvider.Dispose();
        }
    }
}
