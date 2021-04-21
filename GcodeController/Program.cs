using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace GcodeController {

    internal class Program {

        private static void Main(string[] args) {
            var serviceProvider = new ServiceCollection()
               .AddLogging(configure => {
                   configure.AddConsole();
                   configure.SetMinimumLevel(LogLevel.Debug);
               })
               .AddSingleton<ISerialDevice, SerialDevice>()
               .AddSingleton<IEmbededWebServer, EmbededWebServer>()
               .AddSingleton<IJobFileService, JobFileService>()
               .AddSingleton<IJobRunnerService, JobRunnerService>()
               .BuildServiceProvider();
            Console.CancelKeyPress += (s, e) => { serviceProvider.GetService<ISerialDevice>().Close(); };
            serviceProvider.GetService<IEmbededWebServer>().Start(serviceProvider).GetAwaiter().GetResult();
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
            serviceProvider.Dispose();
        }
    }
}
