using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        public static JsonSerializerOptions JsonOptions() {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            options.IgnoreNullValues = false;
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

        public static T? GetAttributeValue<T>(Type sourceType) {
            var attribute = sourceType.GetCustomAttributes(typeof(T), false).FirstOrDefault();
            return attribute is null ? default : (T)attribute;
        }
        public static T? GetAttributeValue<T>(MethodInfo methodInfo) {
            var attribute = methodInfo.GetCustomAttributes(typeof(T), false).FirstOrDefault();
            return attribute is null ? default : (T)attribute;
        }
    }
}
