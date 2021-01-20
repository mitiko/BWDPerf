using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Serializers
{
    public class SerializeToFile : ISerializer<byte>, ISerializer<byte[]>
    {
        public FileInfo File { get; }

        public SerializeToFile(string fileName)
        {
            this.File = new FileInfo(fileName);
            this.File.Delete();
        }

        public async Task Complete(IAsyncEnumerable<byte> input)
        {
            using var writer = new BinaryWriter(this.File.OpenWrite());

            await foreach (var symbol in input)
                writer.Write(symbol);

            writer.Flush();
            writer.Close();
        }

        public async Task Complete(IAsyncEnumerable<byte[]> input)
        {
            using var writer = new BinaryWriter(this.File.OpenWrite());

            await foreach (var buffer in input)
                writer.Write(buffer, 0, buffer.Length);

            writer.Flush();
            writer.Close();
        }
    }
}