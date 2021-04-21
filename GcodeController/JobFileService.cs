using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace GcodeController {

    public interface IJobFileService {

        FileStream Get(string name);

        void Save(Stream stream, string name);

        void Delete(string name);

        string[] List();
    }

    public class JobFileService : IJobFileService {
        private readonly ILogger<JobFileService> _logger;
        private readonly string _appPath;

        public JobFileService(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<JobFileService>();
            _appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GcodeController");
            if (!Directory.Exists(_appPath)) {
                Directory.CreateDirectory(_appPath);
                _logger.LogInformation($"Create Directory {_appPath}");
            }
        }

        public void Save(Stream stream, string name) {
            var newFile = Path.Combine(_appPath, name);
            if (File.Exists(newFile)) {
                File.Delete(newFile);
            }
            using var fs = new FileStream(newFile, FileMode.OpenOrCreate);
            stream.CopyTo(fs);
            fs.Flush();
            _logger.LogInformation($"Created {name}");
        }

        public void Delete(string name) {
            var file = Path.Combine(_appPath, name);
            if (File.Exists(file)) {
                File.Delete(file);
                _logger.LogInformation($"Deleted {name}");
            }
        }

        public string[] List() => Directory.GetFiles(_appPath).Select(x => Path.GetFileName(x)).ToArray();

        public FileStream Get(string name) {
            var file = Path.Combine(_appPath, name);
            return File.OpenRead(file);
        }
    }
}
