using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace GcodeController.Tests {
    public class JobFileServiceTests : IDisposable {
        private IFileService _jobFileService;

        public JobFileServiceTests() {
            _jobFileService = new FileService(new NullLoggerFactory());
        }

        [Fact]
        public async Task Can_Save_File_With_No_Comments() {

            // Arrange
            var testFile = Path.Combine(Path.GetTempPath(), "test.txt");
            await File.WriteAllTextAsync(testFile, $"%{Environment.NewLine}foo{Environment.NewLine}; dsdf{Environment.NewLine}( another comment");
            var fs = new FileStream(testFile, FileMode.Open);
            var uploadedAsFilename = "testAct.gcode";

            // Act
            await _jobFileService.SaveAsync(fs, uploadedAsFilename);

            // Assert
            var modified = _jobFileService.Get(uploadedAsFilename);
            modified.GetStream()?.Close();
            var contents = await File.ReadAllTextAsync(modified.Name);
            Assert.Equal(contents, $"foo{Environment.NewLine}");
        }

        public void Dispose() {

        }
    }
}
