using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using GcodeController.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
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
        public async Task<string> ExecuteAsync([JsonData] CommandRequest commandRequest) => await _commandHandler.ExecuteAsync(commandRequest);
    }
}
