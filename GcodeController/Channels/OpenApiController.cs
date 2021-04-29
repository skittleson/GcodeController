using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using GcodeController.Handlers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace GcodeController.Channels {
    public class OpenApiController : WebApiController {
        const string MIME_TYPE = "application/json";

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

        public static OpenApiDocument GenerateFromControllers() {
            var doc = new OpenApiDocument();

            return doc;
        }

        public static OpenApiDocument GetDoc() {

            // NOTE: while a manual creation works, this should be automated by looping through each controller for methods
            var responses = new OpenApiResponses {
                ["200"] = new OpenApiResponse { Description = "OK" },
                ["404"] = new OpenApiResponse { Description = "Not Found" }
            };
            Func<string, OpenApiParameter> getFileParam = (description) => new OpenApiParameter {
                Name = "name",
                In = ParameterLocation.Path,
                Description = description,
                Required = true,
                Schema = new OpenApiSchema { Type = "string" },
                Example = new OpenApiString("foo.nc")
            };
            Func<string, OpenApiParameter> getPortParam = (description) => new OpenApiParameter {
                Name = "port",
                In = ParameterLocation.Path,
                Description = description,
                Required = true,
                Schema = new OpenApiSchema { Type = "string" },
                Example = new OpenApiString("ttyUSB0")
            };
            return new OpenApiDocument {
                Info = new OpenApiInfo {
                    Version = "1.0.1",
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
                Paths = new OpenApiPaths {
                    [$"/api/{FilesHandler.PREFIX}/" + "{name}"] = new OpenApiPathItem {
                        Operations = new Dictionary<OperationType, OpenApiOperation> {
                            [OperationType.Get] = new OpenApiOperation {
                                Description = "Get file by name",
                                Parameters = new List<OpenApiParameter> { getFileParam("Name of file") },
                                Responses = new OpenApiResponses {
                                    ["200"] = new OpenApiResponse {
                                        Content = {
                                            [MIME_TYPE] = new OpenApiMediaType { Schema = new OpenApiSchema { Type = "string" } }
                                        }
                                    }
                                }
                            },
                            [OperationType.Delete] = new OpenApiOperation {
                                Description = "Delete file by name",
                                Parameters = new List<OpenApiParameter> { getFileParam("Name of file") },
                                Responses = responses
                            }
                        }
                    },
                    [$"/api/{FilesHandler.PREFIX}"] = new OpenApiPathItem {
                        Operations = new Dictionary<OperationType, OpenApiOperation> {
                            [OperationType.Get] = new OpenApiOperation {
                                Description = "Get Files",
                                Responses = new OpenApiResponses {
                                    ["200"] = new OpenApiResponse {
                                        Description = "OK",
                                        Content = {
                                            [MIME_TYPE] = new OpenApiMediaType {
                                                Schema = new OpenApiSchema { Type = "array", Items = new OpenApiSchema {  Type = "string" } }
                                            }
                                        }
                                    },
                                    ["404"] = new OpenApiResponse { Description = "Not Found" }
                                }
                            },
                            [OperationType.Post] = new OpenApiOperation {
                                OperationId = "saveFile",
                                Description = "Upload a gcode file to be used in a job.",
                                RequestBody = new OpenApiRequestBody {
                                    Required = true,
                                    Content = {
                                        ["application/x-www-form-urlencoded"] = new OpenApiMediaType {
                                            Schema = new OpenApiSchema {
                                                Type = "object",
                                                Description = "Form file upload i.e. see: https://www.w3.org/TR/html401/interact/forms.html"
                                            }
                                        }
                                    }
                                },
                                Responses = new OpenApiResponses {
                                    ["200"] = new OpenApiResponse {
                                        Content = {
                                            [MIME_TYPE] = new OpenApiMediaType { Schema = new OpenApiSchema {
                                                Type = "bool",
                                                Description =  "Returns true if file was successfully uploaded" }
                                            }
                                        }
                                    },
                                    ["400"] = new OpenApiResponse {
                                        Description = "Invalid form data"
                                    }
                                }
                            },
                        }
                    },
                    [$"/api/{SerialHandler.PREFIX}"] = new OpenApiPathItem {
                        Operations = new Dictionary<OperationType, OpenApiOperation> {
                            [OperationType.Get] = new OpenApiOperation {
                                OperationId = "getPorts",
                                Responses = new OpenApiResponses {
                                    ["200"] = new OpenApiResponse {
                                        Description = "OK",
                                        Content = {
                                            [MIME_TYPE] = new OpenApiMediaType {
                                                Schema = new OpenApiSchema { Type = "array", Items = SerialResponse.Schema  }
                                            }
                                        }
                                    },
                                }
                            }
                        }
                    },
                    [$"/api/{SerialHandler.PREFIX}/" + "{port}"] = new OpenApiPathItem {
                        Operations = new Dictionary<OperationType, OpenApiOperation> {
                            [OperationType.Get] = new OpenApiOperation {
                                Description = "Get connection by port",
                                Parameters = new List<OpenApiParameter> { getPortParam("port") },
                                Responses = new OpenApiResponses {
                                    ["200"] = new OpenApiResponse {
                                        Content = {
                                            [MIME_TYPE] = new OpenApiMediaType { Schema = SerialResponse.Schema }
                                        }
                                    }
                                }
                            },
                            [OperationType.Post] = new OpenApiOperation {
                                Description = "Create connection",
                                Parameters = new List<OpenApiParameter> { getPortParam("port") },
                                RequestBody = new OpenApiRequestBody {
                                    Content = {
                                            [MIME_TYPE] = new OpenApiMediaType { Schema = CreateNewSerialRequest.Schema }
                                        }
                                },
                                Responses = new OpenApiResponses {
                                    ["200"] = new OpenApiResponse {
                                        Content = {
                                            [MIME_TYPE] = new OpenApiMediaType { Schema = SerialResponse.Schema }
                                        }
                                    }
                                }
                            },
                            [OperationType.Delete] = new OpenApiOperation {
                                Description = "Close connection by port ",
                                Parameters = new List<OpenApiParameter> { getPortParam("Port") },
                                Responses = new OpenApiResponses {
                                    ["200"] = new OpenApiResponse {
                                        Content = {
                                            [MIME_TYPE] = new OpenApiMediaType { Schema = SerialResponse.Schema }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

    }
}
