using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using GcodeController.Models;
using GcodeController.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace GcodeController.Channels {

    //[DisplayName(CommandHandler.PREFIX)]
    public class ConfigurationApiController : WebApiController {
        //private readonly ICommandHandler _commandHandler;

        public ConfigurationApiController(IServiceScope scope) : base() {
            //  _commandHandler = scope.ServiceProvider.GetService<ICommandHandler>();
        }

        [Description("Save Configuration")]
        [Route(HttpVerbs.Post, "/", true)]
        public void Save([JsonData] IAppConfig config) => AppConfigFactory.SaveConfig(config);

        [Description("Get Current Configuration")]
        [Route(HttpVerbs.Get, "/", true)]
        public IAppConfig Get() => AppConfigFactory.GetConfig();

    }
}
