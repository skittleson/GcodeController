using EmbedIO;
using EmbedIO.WebApi;
using GcodeController.web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace GcodeController {

    public interface IEmbededWebServer : IDisposable {

        Task Start(ServiceProvider serviceProvider);
    }

    public class EmbededWebServer : IEmbededWebServer {
        private WebServer webServer;
        private readonly ILogger<EmbededWebServer> _logger;

        public EmbededWebServer(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<EmbededWebServer>();
        }

        public Task Start(ServiceProvider serviceProvider) {
            var assembly = Assembly.GetExecutingAssembly();
            webServer = new WebServer(o => o
                    .WithUrlPrefix("http://*:8081")
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                .WithWebApi("/api", m => m.WithController(() => {
                    var resolvedLogger = serviceProvider.GetService<ILoggerFactory>();
                    var resolvedSerialDevice = serviceProvider.GetService<ISerialDevice>();
                    var resolvedJobFileService = serviceProvider.GetService<IJobFileService>();
                    var resolvedJobRunnerService = serviceProvider.GetService<IJobRunnerService>();
                    return new ApiController(resolvedLogger, resolvedSerialDevice, resolvedJobFileService, resolvedJobRunnerService);
                }
            )).WithEmbeddedResources("/", assembly, "GcodeController.web");
            //.WithModule(new WebSocketChatModule("/chat"))
            //.WithModule(new WebSocketTerminalModule("/terminal"))
            //.WithStaticFolder("/", HtmlRootPath, true, m => m
            //  .WithContentCaching(UseFileCache)) // Add static files after other modules to avoid conflicts
            //.WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

            // Listen for state changes.
            webServer.StateChanged += (s, e) => _logger.LogInformation($"WebServer New State - {e.NewState}");
            return webServer.RunAsync();
        }

        public void Dispose() {
            webServer?.Dispose();
        }
    }
}
