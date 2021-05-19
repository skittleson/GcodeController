using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using GcodeController.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace GcodeController.Channels {

    [DisplayName(CommandHandler.PREFIX)]
    public class CommandApiController : WebApiController {
        private readonly ICommandHandler _commandHandler;

        public CommandApiController(IServiceScope scope) : base() {
            _commandHandler = scope.ServiceProvider.GetService<ICommandHandler>();
        }

        [Description("Execute command on serial port with response")]
        [Route(HttpVerbs.Post, "/", true)]
        public async Task ExecuteAsync([JsonData] CommandRequest commandRequest) {
            var dataBuffer = Encoding.Default.GetBytes(await _commandHandler.ExecuteAsync(commandRequest));
            using var stream = HttpContext.OpenResponseStream();
            await stream.WriteAsync(dataBuffer.AsMemory(0, dataBuffer.Length));
        }
    }
}
