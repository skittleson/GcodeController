using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace GcodeController.Channels {

    [DisplayName(JobService.PREFIX)]
    public class JobsApiController : WebApiController {
        private IJobService _jobService;

        public JobsApiController(IServiceScope scope) : base() {
            _jobService = scope.ServiceProvider.GetService<IJobService>();
        }

        [Description("Get jobs")]
        [Route(HttpVerbs.Get, "/", true)]
        public JobInfo GetJobs() => _jobService.GetJobs();

        [Description("Pause all jobs")]
        [Route(HttpVerbs.Put, "/", true)]
        public JobInfo PauseJob() => _jobService.PauseJob();

        [Description("Start a job by file name")]
        [Route(HttpVerbs.Post, "/{filename}", true)]
        public JobInfo Start(string filename) => _jobService.StartJob(filename);

        [Description("Stop all jobs")]
        [Route(HttpVerbs.Delete, "/", true)]
        public JobInfo StopJob() => _jobService.StopJob();
    }
}
