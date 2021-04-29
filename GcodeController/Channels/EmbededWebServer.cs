using EmbedIO;
using EmbedIO.WebApi;
using GcodeController.Channels;
using GcodeController.Handlers;
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
            Func<IServiceScope> scopeFactory = () => {
                return serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            };
            var assembly = Assembly.GetExecutingAssembly();
            _server = new WebServer(o => o
                    .WithUrlPrefix("http://*:8081")
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                .WithModule(new WebSocketModuleChannel("/socket", serviceProvider))
                .WithWebApi($"/service", m => m.WithController(() => new OpenApiController()))
                //.WithWebApi($"/api/{FilesHandler.PREFIX}", m => m.WithController(() => new FilesApiController(serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())))
                //.WithWebApi($"/api/{JobService.PREFIX}", m => m.WithController(() => new JobsApiController(serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())))
                //.WithWebApi($"/api/{SerialHandler.PREFIX}", m => m.WithController(() => new SerialApiController(serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())))
                .WithWebApi($"/api/{SerialHandler.PREFIX}", CustomResponseSerializer.None(false), m => m.WithController(() => new SerialApiController(scopeFactory())))
                .WithWebApi($"/api/{JobService.PREFIX}", CustomResponseSerializer.None(false), m => m.WithController(() => new JobsApiController(scopeFactory())))
                .WithWebApi($"/api/{FilesHandler.PREFIX}", CustomResponseSerializer.None(false), m => m.WithController(() => new FilesApiController(scopeFactory())))
                .WithEmbeddedResources("/", assembly, "GcodeController.web");

            var ipAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault();
            _logger.LogInformation($"Go to http://{ipAddress}/8081");

            _server.StateChanged += (s, e) => _logger.LogDebug($"WebServer New State - {e.NewState}");
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

        public void Dispose() => _server?.Dispose();

    }


    //NOTE: the ONLY goal with this is to keep a standard of serialization with all channels (api, mqtt, )
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
                using var text = context.OpenResponseText(context.Response.ContentEncoding, bufferResponse, preferCompression);
                await text.WriteAsync(responseString).ConfigureAwait(false);
            };
    }

}
