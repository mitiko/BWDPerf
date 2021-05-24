using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Serializers
{
    public class SerializeToFile :
        ISerializer<byte>,
        ISerializer<byte[]>,
        ISerializer<ReadOnlyMemory<byte>>,
        ISerializer<ReadOnlyMemory<ushort>>
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

        public async Task Complete(IAsyncEnumerable<ReadOnlyMemory<byte>> input)
        {
            using var writer = new BinaryWriter(this.File.OpenWrite());

            await foreach (var buffer in input)
                writer.Write(buffer.Span);

            writer.Flush();
            writer.Close();
        }

        public async Task Complete(IAsyncEnumerable<ReadOnlyMemory<ushort>> input)
        {
            using var writer = new BinaryWriter(this.File.OpenWrite());

            await foreach (var buffer in input)
            {
                for (int i = 0; i < buffer.Length; i++)
                    writer.Write(buffer.Span[i]);
            }

            writer.Flush();
            writer.Close();
        }
    }
}