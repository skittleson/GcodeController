using System.Collections.Generic;
using System.IO;

namespace GcodeController {
    public static class FileStreamBatchExtension {

        public static IEnumerable<IEnumerable<string>> ReadLineBatches(this StreamReader reader, int batchCount) {
            var batchItems = new List<string>();
            while (!reader.EndOfStream) {
                // clear the batch list
                batchItems.Clear();

                // read file in batches of `batchCount`
                for (var i = 0; i < batchCount; i++) {
                    if (reader.EndOfStream)
                        break;

                    batchItems.Add(reader.ReadLine());
                }
                yield return batchItems;
            }
        }
    }
}
