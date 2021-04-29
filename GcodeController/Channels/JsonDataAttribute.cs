using EmbedIO;
using EmbedIO.WebApi;
using System;
using System.Threading.Tasks;

namespace GcodeController.Channels {
    [AttributeUsage(AttributeTargets.Parameter)]
    public class JsonDataAttribute : Attribute, IRequestDataAttribute<WebApiController> {
        public async Task<object?> GetRequestDataAsync(WebApiController controller, Type type, string parameterName) {
            string body;
            using (var reader = controller.HttpContext.OpenRequestText()) {
                body = await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            try {
                return System.Text.Json.JsonSerializer.Deserialize(body, type, Utils.JsonOptions());
            } catch (FormatException) {
                throw HttpException.BadRequest($"Expected request body to be deserializable to {type.FullName}.");
            }
        }
    }
}
