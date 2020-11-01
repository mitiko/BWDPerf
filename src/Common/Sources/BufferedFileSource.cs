using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Common.Sources
{
    public class BufferedFileSource : ISource<byte[]>
    {
        public FileInfo File { get; }
        public int BufferSize { get; }
        public bool UseProgressBar { get; }

        public BufferedFileSource(string fileName, int bufferSize = -1, bool useProgressBar = true)
        {
            this.File = new FileInfo(fileName);
            if (bufferSize < 0)
                bufferSize = (16 << 10); // 16Kb default buffer
            this.BufferSize = bufferSize;
            this.UseProgressBar = useProgressBar;
        }

        public async IAsyncEnumerable<byte[]> Fetch()
        {
            var reader = PipeReader.Create(this.File.OpenRead(), new StreamPipeReaderOptions(bufferSize: this.BufferSize));
            LinearProgressBar progressBar = null;
            if (this.UseProgressBar)
                progressBar = new LinearProgressBar(this.File.Length);

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                if (this.UseProgressBar)
                {
                    progressBar.UpdateProgress(buffer.Length);
                    progressBar.Print();
                }
                
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