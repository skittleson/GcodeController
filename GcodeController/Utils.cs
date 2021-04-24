using System.Diagnostics;

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
    }
}
