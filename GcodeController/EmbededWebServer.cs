using EmbedIO;
using EmbedIO.WebApi;
using GcodeController.web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GcodeController {

    public interface IEmbededWebServer : IDisposable {

        Task Start(ServiceProvider serviceProvider);
    }

    public class EmbededWebServer : IEmbededWebServer {
        private WebServer _server;
        private readonly ILogger<EmbededWebServer> _logger;

        public EmbededWebServer(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<EmbededWebServer>();
        }

        public Task Start(ServiceProvider serviceProvider) {
            var assembly = Assembly.GetExecutingAssembly();
            _server = new WebServer(o => o
                    .WithUrlPrefix("http://*:8081")
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                .WithWebApi("/api", m => m.WithController(() => {
                    var resolvedLogger = serviceProvider.GetService<ILoggerFactory>();
                    var resolvedSerialDevice = serviceProvider.GetService<ISerialDevice>();
                    var resolvedJobFileService = serviceProvider.GetService<IJobFileService>();
                    var resolvedJobRunnerService = serviceProvider.GetService<IJobRunnerService>();
                    var controller = new ApiController(resolvedLogger, resolvedSerialDevice, resolvedJobFileService, resolvedJobRunnerService);
                    return controller;
                }
            )).WithEmbeddedResources("/", assembly, "GcodeController.web");
            var ipAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault();
            _logger.LogInformation($"Go to http://{ipAddress}/8081");

            // Listen for state changes.
            _server.StateChanged += (s, e) => _logger.LogInformation($"WebServer New State - {e.NewState}");
            _server.HandleHttpException(async (context, exception) => {
                context.Response.StatusCode = exception.StatusCode;
                switch (exception.StatusCode) {
                    case 404:
                        await context.SendStringAsync("Not Found", "text/html", Encoding.UTF8);
                        break;
                    default:
                        await HttpExceptionHandler.Default(context, exception);
                        break;
                }
            });
            return _server.RunAsync();
        }

        public void Dispose() {
            _server?.Dispose();
        }
    }
}
