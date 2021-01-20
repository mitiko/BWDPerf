using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Sources
{
    public class BufferedFileSource : ISource<byte[]>
    {
        public FileInfo File { get; }
        public int BufferSize { get; }

        public BufferedFileSource(string fileName, int bufferSize = -1)
        {
            this.File = new FileInfo(fileName);
            if (bufferSize < 0)
                bufferSize = (16 << 10); // 16Kb default buffer
            this.BufferSize = bufferSize;
        }

        public async IAsyncEnumerable<byte[]> Fetch()
        {
            var reader = PipeReader.Create(this.File.OpenRead(), new StreamPipeReaderOptions(bufferSize: this.BufferSize));

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                if (buffer.Length != 0)
                    yield return buffer.ToArray();

                buffer = buffer.Slice(buffer.End);
                reader.AdvanceTo(buffer.End, buffer.End);
                if (result.IsCompleted)
                    break;
            }
        }
    }
}