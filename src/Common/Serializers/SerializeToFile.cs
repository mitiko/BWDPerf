using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Serializers
{
    public class SerializeToFile : ISerializer<byte>, ISerializer<byte[]>
    {
        public FileInfo File { get; }
        public int BufferSize { get; }

        public SerializeToFile(string fileName, int bufferSize = 256)
        {
            this.File = new FileInfo(fileName);
            this.File.Delete();
            this.BufferSize = bufferSize;
        }

        public async Task Complete(IAsyncEnumerable<byte> input)
        {
            using var writer = new BinaryWriter(this.File.OpenWrite());

            var buffer = new byte[this.BufferSize];
            var count = 0;

            await foreach (var symbol in input)
            {
                if (count == buffer.Length)
                    writer.Write(buffer, 0, count);
                else
                    buffer[count] = symbol;

                count = count == buffer.Length ? 0 : count + 1;
            }
            writer.Write(buffer, 0, count);
            writer.Flush();
            writer.Close();
        }

        public async Task Complete(IAsyncEnumerable<byte[]> input)
        {
            using var writer = new BinaryWriter(this.File.OpenWrite());

            await foreach (var symbol in input)
            {
                writer.Write(symbol, 0, symbol.Length);
            }
            writer.Flush();
            writer.Close();
        }
    }
}