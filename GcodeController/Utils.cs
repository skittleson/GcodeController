using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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
    }
}
