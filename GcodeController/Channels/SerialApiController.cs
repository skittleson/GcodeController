using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using GcodeController.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace GcodeController.Channels {

    [DisplayName(SerialHandler.PREFIX)]
    public class SerialApiController : WebApiController {
        private readonly ISerialHandler _serialHandler;

        public SerialApiController(IServiceScope scope) : base() {
            _serialHandler = scope.ServiceProvider.GetService<ISerialHandler>();
        }

        [Description("Close serial device connection")]
        [Route(HttpVerbs.Delete, "/{port}", true)]
        public void Delete(string port) => _serialHandler.Delete(port);

        [Description("Get serial device")]
        [Route(HttpVerbs.Get, "/{port}", true)]
        public SerialResponse Get(string port) => _serialHandler.Get(port);

        [Description("Get a list of serial devices")]
        [Route(HttpVerbs.Get, "/", true)]
        public IEnumerable<SerialResponse> List() => _serialHandler.List();

        [Description("Open a connection to a serial device")]
        [Route(HttpVerbs.Post, "/{port}", true)]
        public async Task<bool> OpenAsync(string port, [JsonData] CreateNewSerialRequest createNewSerialRequest) {
            if (createNewSerialRequest is null) {
                throw HttpException.BadRequest("Request is empty");
            }
            if (createNewSerialRequest.BaudRate < 1) {
                throw HttpException.BadRequest("Use a standard baud rate.");
            }
            createNewSerialRequest.Port = port;
            return await _serialHandler.OpenAsync(createNewSerialRequest);
        }
    }
}
