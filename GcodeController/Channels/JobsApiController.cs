using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Microsoft.Extensions.DependencyInjection;

namespace GcodeController.Channels {
    public class JobsApiController : WebApiController {
        private IJobService _jobService;

        public JobsApiController(IServiceScope scope) : base() {
            _jobService = scope.ServiceProvider.GetService<IJobService>();
        }

        [Route(HttpVerbs.Put, "/", true)]
        public JobInfo PauseJob() => _jobService.PauseJob();

        [Route(HttpVerbs.Post, "/{filename}", true)]
        public JobInfo Start(string filename) => _jobService.StartJob(filename);

        [Route(HttpVerbs.Delete, "/", true)]
        public JobInfo StopJob() => _jobService.StopJob();
    }
}
