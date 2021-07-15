using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using GcodeController.Models;
using GcodeController.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Threading.Tasks;

namespace GcodeController.Channels {

    [DisplayName(PREFIX)]
    public class ConfigurationApiController : WebApiController {
        public const string PREFIX = "configuration";

        public ConfigurationApiController(IServiceScope scope) : base() {
        }

        [Description("Save Configuration")]
        [Route(HttpVerbs.Post, "/", true)]
        public async Task SaveAsync([JsonData] AppConfig config) => await AppConfigFactory.SaveConfigAsync(config);

        [Description("Get Current Configuration from local directory, user directory, or system defaults")]
        [Route(HttpVerbs.Get, "/", true)]
        public AppConfig Get() => AppConfigFactory.GetConfig();
    }
}
