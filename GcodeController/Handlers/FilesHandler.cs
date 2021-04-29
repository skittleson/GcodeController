using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GcodeController.Handlers {

    public interface IFilesHandler {
        Task<bool> SaveAsync(Stream stream, string fileName);
        IEnumerable<string> List();
        void Delete(string fileName);


    }
    public class FilesHandler : AHandler, IFilesHandler {
        private IFileService _fileService;

        public FilesHandler(IFileService fileService) {
            _fileService = fileService;
        }

        public const string PREFIX = "files";
        public override string GetPrefix => PREFIX;

        public void Delete(string fileName) => _fileService.Delete(fileName);

        public IEnumerable<string> List() => _fileService.List();

        public async Task<bool> SaveAsync(Stream stream, string fileName) =>
            await _fileService.SaveAsync(stream, fileName);


        public Task Get(string fileName) {
            var stream = _fileService.Get(fileName);

            //_fileService.Get(fileName);
            return Task.Delay(100);
        }
    }
}
