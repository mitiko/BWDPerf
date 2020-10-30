using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Channels;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Common.Sources
{
    public class FileSource : ISource
    {
        public FileInfo File { get; }
        public int BufferSize { get; }

        public FileSource(string fileName, int bufferSize = -1)
        {
            this.File = new FileInfo(fileName);
            this.BufferSize = bufferSize;
        }

        public async IAsyncEnumerable<byte> Fetch()
        {
            System.Console.WriteLine("Started reading");
            var reader = PipeReader.Create(this.File.OpenRead());
            var progressBar = new LinearProgressBar(this.File.Length);

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                // progressBar.UpdateProgress(buffer.Length);
                // progressBar.Print();

                foreach (var symbol in buffer.ToArray())
                    // yield return symbol;
                {
                    // System.Console.WriteLine($"Writing {(char) symbol}");
                    yield return symbol;
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted)
                    break;
            }
            System.Console.WriteLine("Ended reading");
        }
    }
}