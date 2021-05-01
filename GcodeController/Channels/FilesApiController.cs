using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using GcodeController.Handlers;
using HttpMultipartParser;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace GcodeController.Channels {

    [DisplayName(FilesHandler.PREFIX)]
    public class FilesApiController : WebApiController {
        private IFilesHandler _filesHandler;

        public FilesApiController(IServiceScope scope) : base() {
            _filesHandler = scope.ServiceProvider.GetRequiredService<IFilesHandler>();
        }

        [Description("Delete a file")]
        [Route(HttpVerbs.Delete, "/{name}", true)]
        public void Delete(string name) => _filesHandler.Delete(name);

        [Description("Upload single file to be used in jobs")]
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

        [Description("Get a list of files")]
        [Route(HttpVerbs.Get, "/", true)]
        public IEnumerable<string> List() => _filesHandler.List();

    }
}
