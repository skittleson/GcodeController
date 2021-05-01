using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GcodeController.Channels {

    public class OpenApiController : WebApiController {
        private const string MIME_TYPE = "application/json";

        public OpenApiController() : base() {
        }

        [Route(HttpVerbs.Get, "/schema.json", true)]
        public async Task Get() {
            var outputStringWriter = new StringWriter(CultureInfo.InvariantCulture);
            var writer = new OpenApiJsonWriter(outputStringWriter);
            GetDoc().SerializeAsV3(writer);
            writer.Flush();
            await HttpContext.SendStringAsync(outputStringWriter.GetStringBuilder().ToString(), "application/json", System.Text.Encoding.UTF8);
        }

        public static OpenApiDocument GetDoc() {
            var responses = new OpenApiResponses {
                ["200"] = new OpenApiResponse { Description = "OK" },
                ["404"] = new OpenApiResponse { Description = "Not Found" }
            };
            //TODO: Could these be auto discovered?
            var controllers = new[] {
                typeof(SerialApiController),
                typeof(JobsApiController),
                typeof(FilesApiController),
                typeof(CommandApiController)
            };
            var doc = new OpenApiDocument {
                Info = new OpenApiInfo {
                    Version = "1.0.1", // TODO: use assembly version
                    Title = "Gcode Controller",
                    License = new OpenApiLicense {
                        Url = new Uri("https://github.com/skittleson/GcodeController/blob/master/LICENSE"),
                        Name = "MIT"
                    },
                    Contact = new OpenApiContact {
                        Name = "Spencer Kittleson",
                        Url = new Uri("https://github.com/skittleson/GcodeController")
                    }
                },
                Paths = new OpenApiPaths()
            };
            foreach (var controller in controllers) {
                var controllerMethods = controller.GetMethods();

                // Populate all possible routes
                var routes = controllerMethods
                    .Select(x => GetRouteAttribute(x))
                    .Where(x => x != null)
                    .ToArray();
                var prefix = Utils.GetAttributeValue<DisplayNameAttribute>(controller)?.DisplayName;
                var controllerApiEndpoint = $"/{prefix}";
                foreach (var route in routes) {
                    if (!doc.Paths.ContainsKey($"{controllerApiEndpoint}{route.Route}")) {
                        doc.Paths.Add($"{controllerApiEndpoint}{route.Route}", new OpenApiPathItem() {
                            Description = Utils.GetAttributeValue<DescriptionAttribute>(controller)?.Description
                        });
                    }
                }

                // Fill in information about each endpoint
                foreach (var method in controllerMethods) {
                    var route = GetRouteAttribute(method);
                    if (!method.IsPublic || method.IsStatic || route is null) continue;
                    var openApiRoute = new OpenApiOperation() {
                        Description = Utils.GetAttributeValue<DescriptionAttribute>(method)?.Description,
                        OperationId = $"{prefix}.{method.Name}",
                        Extensions = { }
                    };
                    foreach (var inputParam in method.GetParameters()) {
                        var openApiParam = new OpenApiParameter {
                            Name = inputParam.Name
                        };
                        var schema = new OpenApiSchema { Type = "object" };
                        if (inputParam.ParameterType == typeof(string)) {
                            schema.Type = "string";
                            openApiParam.In = ParameterLocation.Path;
                            openApiParam.Required = true;
                        } else if (inputParam.ParameterType == typeof(bool)) {
                            schema.Type = "bool";
                            openApiParam.In = ParameterLocation.Path;
                            openApiParam.Required = true;
                        } else if (inputParam.ParameterType == typeof(int)) {
                            schema.Type = "int";
                            schema.Format = "Int32";
                            openApiParam.In = ParameterLocation.Path;
                        }
                        openApiParam.Required = true;
                        openApiParam.Schema = schema;

                        // Attribute would be better here
                        if (openApiParam.Name.Equals("port", StringComparison.OrdinalIgnoreCase)) {
                            openApiParam.Example = new OpenApiString("ttyUSB0");
                        }
                        openApiRoute.Parameters.Add(openApiParam);
                    }
                    foreach (var inputParam in method.GetParameters()) {
                        var schema = new OpenApiSchema { Type = "object" };
                        if (inputParam.ParameterType.Name == "String" || inputParam.ParameterType.BaseType.Name != "Object") continue;
                        var desc = Utils.GetAttributeValue<DescriptionAttribute>(method);
                        schema.Description = desc.Description;
                        schema.Properties = Utils.CreateProperties(inputParam.ParameterType);
                        schema.Required = schema.Properties.Select(x => x.Key).ToHashSet();
                        openApiRoute.RequestBody = new OpenApiRequestBody {
                            Content = {
                                    [MIME_TYPE] = new OpenApiMediaType { Schema = schema }
                                }
                        };
                    }

                    // Handle response objects
                    if (method.ReturnType.BaseType?.Assembly?.GetName()?.Name == "GcodeController") {
                    }
                    openApiRoute.Responses = responses;
                    //openApiRoute.Extensions.Add(new KeyValuePair<string, I>)

                    doc.Paths[$"{controllerApiEndpoint}{route.Route}"]
                        .AddOperation(RouteVerbToOpenApiOperation(route.Verb), openApiRoute);
                }
            }

            // Special path update
            doc.Paths["/files/"].Operations[OperationType.Post].RequestBody = new OpenApiRequestBody {
                Required = true,
                Content = {
                    ["application/x-www-form-urlencoded"] = new OpenApiMediaType {
                        Schema = new OpenApiSchema {
                            Type = "object",
                            Description = "Form file upload i.e. see: https://www.w3.org/TR/html401/interact/forms.html"
                        }
                    }
                }
            };
            return doc;
        }

        public static RouteAttribute GetRouteAttribute(MethodInfo method) {
            return (method.GetCustomAttributes(typeof(RouteAttribute), false).FirstOrDefault() as RouteAttribute);
        }

        public static OperationType RouteVerbToOpenApiOperation(HttpVerbs httpVerb) {
            return httpVerb switch {
                HttpVerbs.Delete => OperationType.Delete,
                HttpVerbs.Get => OperationType.Get,
                HttpVerbs.Head => OperationType.Head,
                HttpVerbs.Options => OperationType.Options,
                HttpVerbs.Patch => OperationType.Patch,
                HttpVerbs.Post => OperationType.Post,
                HttpVerbs.Put => OperationType.Put,
                _ => OperationType.Trace,
            };
        }
    }
}
