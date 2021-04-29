using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using GcodeController.Handlers;
using HttpMultipartParser;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GcodeController.Channels {
    public class FilesApiController : WebApiController {
        private IFilesHandler _filesHandler;

        public FilesApiController(IServiceScope scope) : base() {
            _filesHandler = scope.ServiceProvider.GetRequiredService<IFilesHandler>();
        }

        [Route(HttpVerbs.Delete, "/{name}", true)]
        public void Delete(string name) => _filesHandler.Delete(name);

        [Route(HttpVerbs.Post, "/", true)]
        public async Task<bool> SaveAsync() {
            var parser = await MultipartFormDataParser.ParseAsync(Request.InputStream);
            if (parser?.Files is null) {
                throw HttpException.NotFound("No file uploaded");
            }
            if (parser?.Files.Count != 1) {
                throw HttpException.BadRequest("Only 1 file can be uploaded");
            }
            return await _filesHandler.SaveAsync(parser.Files[0].Data, parser.Files[0].FileName);
        }

        [Route(HttpVerbs.Get, "/", true)]
        public IEnumerable<string> List() => _filesHandler.List();

    }
}
