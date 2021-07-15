using EmbedIO;
using EmbedIO.WebApi;
using GcodeController.Channels;
using GcodeController.Handlers;
using GcodeController.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GcodeController {

    public interface IEmbededWebServer : IDisposable {
        Task Start(ServiceProvider serviceProvider);
    }

    public class EmbededWebServer : IEmbededWebServer {
        private WebServer _server;
        private readonly ILogger<EmbededWebServer> _logger;
        private readonly AppConfig _config;
        private readonly CancellationToken _cancellationToken;

        public EmbededWebServer(ILoggerFactory loggerFactory, AppConfig config, CancellationToken cancellationToken) {
            _logger = loggerFactory.CreateLogger<EmbededWebServer>();
            _config = config;
            _cancellationToken = cancellationToken;
        }

        public Task Start(ServiceProvider serviceProvider) {
            IServiceScope scopeFactory() {
                return serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            }
            var assembly = Assembly.GetExecutingAssembly();
            _server = new WebServer(o => o
                    .WithUrlPrefix($"http://*:{_config.Port}")
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                .WithModule(new WebSocketModuleChannel("/socket", serviceProvider))
                .WithWebApi($"/service", m => m.WithController(() => new OpenApiController()))
                .WithWebApi($"/api/{SerialHandler.PREFIX}", CustomResponseSerializer.None(false), m => m.WithController(() => new SerialApiController(scopeFactory())))
                .WithWebApi($"/api/{JobService.PREFIX}", CustomResponseSerializer.None(false), m => m.WithController(() => new JobsApiController(scopeFactory())))
                .WithWebApi($"/api/{FilesHandler.PREFIX}", CustomResponseSerializer.None(false), m => m.WithController(() => new FilesApiController(scopeFactory())))
                .WithWebApi($"/api/{CommandHandler.PREFIX}", CustomResponseSerializer.None(false), m => m.WithController(() => new CommandApiController(scopeFactory())))
                .WithWebApi($"/api/{ConfigurationApiController.PREFIX}", CustomResponseSerializer.None(false), m => m.WithController(() => new ConfigurationApiController(scopeFactory())))
                .WithEmbeddedResources("/", assembly, "GcodeController.web");
            var ipv4Address = Dns
                .GetHostAddresses(Dns.GetHostName())
                .Select(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .FirstOrDefault();
            _logger.LogInformation($"Go to http://{ipv4Address}:{_config.Port}");
            _server.StateChanged += (s, e) => _logger.LogDebug($"WebServer New State - {e.NewState}");
            _server.HandleHttpException(async (context, exception) => {
                context.Response.StatusCode = exception.StatusCode;
                switch (exception.StatusCode) {
                    case 404:
                        await context.SendStringAsync("Not Found", "plain/text", Encoding.UTF8);
                        break;
                    default:
                        await HttpExceptionHandler.Default(context, exception);
                        break;
                }
            });
            return _server.RunAsync(_cancellationToken);
        }

        public void Dispose() => _server?.Dispose();

    }


    //NOTE: the ONLY goal with this is to keep a standard of serialization with all channels (api, websocket, mqtt)
    public static class CustomResponseSerializer {
        private static readonly ResponseSerializerCallback ChunkedEncodingBaseSerializer = GetBaseSerializer(false);
        private static readonly ResponseSerializerCallback BufferingBaseSerializer = GetBaseSerializer(true);

        public static ResponseSerializerCallback None(bool bufferResponse)
            => bufferResponse ? BufferingBaseSerializer : ChunkedEncodingBaseSerializer;

        private static ResponseSerializerCallback GetBaseSerializer(bool bufferResponse)
            => async (context, data) => {
                if (data is null) {
                    return;
                }
                if (!context.TryDetermineCompression(context.Response.ContentType, out var preferCompression)) {
                    preferCompression = true;
                }
                var responseString = System.Text.Json.JsonSerializer.Serialize(data, Utils.JsonOptions());
                context.Response.ContentType = "application/json; charset=utf-8";
                using var text = context.OpenResponseText(context.Response.ContentEncoding, bufferResponse, preferCompression);
                await text.WriteAsync(responseString).ConfigureAwait(false);
            };
    }

}
