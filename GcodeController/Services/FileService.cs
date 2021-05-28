using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GcodeController {

    public interface IFileService {

        FileResponse Get(string name);

        Task<FileResponse> SaveAsync(Stream stream, string name);

        void Delete(string name);
        IEnumerable<FileResponse> List();
    }

    public class FileResponse {
        private FileStream _fileStream;

        public FileResponse() {
        }

        public FileResponse(FileStream fileStream) {
            _fileStream = fileStream;
        }

        public string Name {
            get; set;
        }

        public FileStream GetStream() => _fileStream;

    }

    public class FileService : IFileService {
        private readonly ILogger<IFileService> _logger;
        private readonly string _appPath;

        public FileService(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<IFileService>();
            _appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(GcodeController));
            if (!Directory.Exists(_appPath)) {
                Directory.CreateDirectory(_appPath);
                _logger.LogInformation($"Create Directory {_appPath}");
            }
        }

        public async Task<FileResponse> SaveAsync(Stream stream, string name) {
            var destFileName = Path.Combine(_appPath, name);
            if (File.Exists(destFileName)) {
                File.Delete(destFileName);
            }
            using var rawFileStream = new FileStream(destFileName, FileMode.OpenOrCreate);
            await stream.CopyToAsync(rawFileStream);
            rawFileStream.Flush();
            stream.Close();
            rawFileStream.Position = 0;

            // Reopen the file, strip comments, write to a temp file
            using var rawFileReader = new StreamReader(rawFileStream);
            using var transformFileStream = new FileStream($"{destFileName}.tmp", FileMode.OpenOrCreate);
            string line;
            while ((line = await rawFileReader.ReadLineAsync()) != null) {
                line = line.Trim();

                // Skip comments
                if (line.StartsWith("(") || line.StartsWith("%") || line.StartsWith(";")) {
                    continue;
                }
                var bytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
                await transformFileStream.WriteAsync(bytes.AsMemory(0, bytes.Length));
            }
            transformFileStream.Close();
            rawFileReader.Close();
            File.Delete(destFileName);
            File.Move(transformFileStream.Name, destFileName);
            _logger.LogInformation($"Created {name}");
            if (!File.Exists(destFileName)) {
                throw new FileNotFoundException(destFileName);
            }
            return new FileResponse { Name = Path.GetFileName(destFileName) };
        }

        public void Delete(string name) {
            var file = Path.Combine(_appPath, name);
            if (File.Exists(file)) {
                File.Delete(file);
                _logger.LogInformation($"Deleted {name}");
            }
        }

        public IEnumerable<FileResponse> List() => Directory.GetFiles(_appPath)
            .Select(x => new FileResponse { Name = Path.GetFileName(x) })
            .ToArray();

        public FileResponse Get(string name) {
            var file = Path.Combine(_appPath, name);
            return new FileResponse(File.OpenRead(file)) { Name = file };
        }
    }
}
