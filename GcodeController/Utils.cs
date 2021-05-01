using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GcodeController {
    public static class Utils {
        public static string RunCommandWithResponse(string processName, string arguments) {
            var process = new Process {
                StartInfo = {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    Arguments = arguments,
                    FileName = processName
              }
            };
            process.Start();
            var standardOutput = string.Empty;
            while (!process.HasExited) {
                standardOutput += process.StandardOutput.ReadToEnd();
            }
            return standardOutput;
        }

        public async static Task<string> ReadUntilAsync(Stream stream, byte[] write) {
            await stream.WriteAsync(write.AsMemory(0, write.Length));
            await Task.Delay(100);
            var buffer = new byte[4096];
            var ct = new CancellationTokenSource(1000).Token;
            await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            var response = Encoding.ASCII.GetString(buffer);
            return response.Substring(0, response.IndexOf('\0')).Trim();
        }

        public static JsonSerializerOptions JsonOptions() {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            options.IgnoreNullValues = true;
            options.DictionaryKeyPolicy = null;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.IgnoreReadOnlyProperties = false;
            options.PropertyNameCaseInsensitive = true;
            return options;
        }

        public static Dictionary<string, OpenApiSchema> CreateProperties(Type sourceType) {
            var result = new Dictionary<string, OpenApiSchema>();
            var props = sourceType.GetProperties();
            foreach (var prop in props) {
                if (prop.GetAccessors().Any(x => x.IsStatic)) {
                    continue;
                }
                var schema = new OpenApiSchema {
                    Type = "object"
                };
                if (prop.PropertyType == typeof(string)) {
                    var stringProperty = new OpenApiString("");
                    schema.Type = stringProperty.PrimitiveType.ToString();
                    schema.Default = stringProperty;
                } else if (prop.PropertyType == typeof(bool)) {
                    var boolJsonProperty = new OpenApiBoolean(false);
                    schema.Type = boolJsonProperty.PrimitiveType.ToString();
                    schema.Default = boolJsonProperty;
                } else if (prop.PropertyType == typeof(int)) {
                    var intPropertyType = new OpenApiInteger(0);
                    schema.Type = intPropertyType.PrimitiveType.ToString();
                    schema.Default = intPropertyType;
                    schema.Format = "Int32";
                }
                schema.Description = GetAttributeValue<DescriptionAttribute>(prop.PropertyType)?.Description;
                result.Add(prop.Name, schema);
            }
            return result;
        }

        public static T GetAttributeValue<T>(Type sourceType) {
            var attribute = sourceType.GetCustomAttributes(typeof(T), false).FirstOrDefault();
            return attribute is null ? default : (T)attribute;
        }
        public static T GetAttributeValue<T>(MethodInfo methodInfo) {
            var attribute = methodInfo.GetCustomAttributes(typeof(T), false).FirstOrDefault();
            return attribute is null ? default : (T)attribute;
        }
    }
}
