using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Serializers
{
    public class SerializeToFile : ISerializer
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
            System.Console.WriteLine("Started writing");
            using var writer = new BinaryWriter(this.File.OpenWrite());
            var buffer = new byte[this.BufferSize];
            var count = 0;
            await foreach (var symbol in input)
            {
                if (count == buffer.Length)
                {
                    writer.Write(buffer, 0, count);
                    count = 0;
                }
                else
                {
                    buffer[count] = symbol;
                    count++;
                }
            }
            writer.Write(buffer, 0, count);
            System.Console.WriteLine("Ended writing");
        }
    }
}