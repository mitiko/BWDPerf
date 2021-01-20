using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Sources
{
    public class FileSource : ISource<byte>
    {
        public FileInfo File { get; }

        public FileSource(string fileName)
        {
            this.File = new FileInfo(fileName);
        }

        public async IAsyncEnumerable<byte> Fetch()
        {
            var reader = PipeReader.Create(this.File.OpenRead());

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                foreach (var symbol in buffer.ToArray())
                    yield return symbol;

                buffer = buffer.Slice(buffer.End);
                reader.AdvanceTo(buffer.End, buffer.End);
                if (result.IsCompleted)
                    break;
            }
        }
    }
}