using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Sources
{
    public class BWDFileSource : ISource<ushort>
    {
        public FileInfo File { get; }
        private byte[] Bytes { get; }

        public BWDFileSource(string fileName)
        {
            this.File = new FileInfo(fileName);
            this.Bytes = new byte[2];
        }

        public async IAsyncEnumerable<ushort> Fetch()
        {
            var reader = PipeReader.Create(this.File.OpenRead());

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                int i = 0;
                foreach (var symbol in buffer.ToArray())
                {
                    if (i++ % 2 == 0)
                    {
                        this.Bytes[0] = symbol;
                    }
                    else
                    {
                        this.Bytes[1] = symbol;
                        yield return BitConverter.ToUInt16(this.Bytes);
                    }
                }

                buffer = buffer.Slice(buffer.End);
                reader.AdvanceTo(buffer.End, buffer.End);
                if (result.IsCompleted)
                    break;
            }
        }
    }
}