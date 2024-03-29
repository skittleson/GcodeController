﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GcodeController.Handlers {

    public interface IFilesHandler {
        Task<FileResponse> SaveAsync(Stream stream, string fileName);
        IEnumerable<FileResponse> List();
        void Delete(string fileName);


    }
    public class FilesHandler : AHandler, IFilesHandler {

        //--- Fields ---
        private IFileService _fileService;

        //--- Properties ---
        public const string PREFIX = "files";
        public override string GetPrefix => PREFIX;

        //--- Constructors ---
        public FilesHandler(IFileService fileService) {
            _fileService = fileService;
        }

        //--- Methods ---
        public void Delete(string fileName) => _fileService.Delete(fileName);

        public IEnumerable<FileResponse> List() => _fileService.List();

        public async Task<FileResponse> SaveAsync(Stream stream, string fileName) =>
            await _fileService.SaveAsync(stream, fileName);

        public Stream Get(string fileName) {
            var stream = _fileService.Get(fileName);
            return stream?.GetStream();
        }
    }
}
