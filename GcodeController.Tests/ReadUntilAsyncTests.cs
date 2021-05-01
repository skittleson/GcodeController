using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GcodeController.Tests {
    public class ReadUntilAsyncTests {

        [Fact]
        public async Task Can_Get_Data_From_Stream() {

            // Arranage
            const string expectedResult = "some data comes back\nok";
            using var mockSerialStream = GenerateStreamFromString(expectedResult);
            var commandBytes = Encoding.UTF8.GetBytes("Data to be sent\n");
            var ct = new CancellationTokenSource(5000);


            // Act
            var actual = await Utils.ReadUntilAsync(mockSerialStream, commandBytes, ct);

            // Assert
            Assert.Equal(expectedResult, actual);
        }

        public static Stream GenerateStreamFromString(string s) {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
