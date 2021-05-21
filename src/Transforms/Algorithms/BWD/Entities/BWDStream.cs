using System;
using System.Collections.Generic;

namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public class BWDStream
    {
        private ReadOnlyMemory<ushort> Stream { get; set; }
        public int Length => this.Stream.Length;
        public ushort this[int index] => this.Stream.Span[index];

        public BWDStream(ReadOnlyMemory<ushort> stream) =>
            this.Stream = stream;

        public BWDStream(ReadOnlyMemory<byte> buffer, BWDictionary dictionary)
        {
            // TODO: without s token, we can do parsing in O(n), by finding the first word that matches
            // NO TODO: Use the FM-index to do parsing in O(n)
            var data = new ushort[buffer.Length];
            data.AsSpan().Fill(ushort.MaxValue);

            for (int i = 0; i < dictionary.Count; i++)
            {
                var word = dictionary[i];
                for (int j = 0; j < buffer.Length; j++)
                {
                    // check if location is used
                    if (data[j] != ushort.MaxValue) continue;
                    if (j + word.Length - 1 >= buffer.Length) break; // can't fit word
                    var match = true;
                    for (int s = 0; s < word.Length; s++)
                        if (buffer.Span[j + s] != word.Span[s] || data[j+s] != ushort.MaxValue) { match = false; break; }

                    if (match == true)
                    {
                        for (int k = 0; k < word.Length; k++)
                            data[j+k] = (ushort) i;
                    }
                }
            }

            var stream = new List<ushort>();
            for (int k = 0; k < data.Length;)
            {
                if (data[k] == ushort.MaxValue) throw new Exception("Dictionary doesn't cover the whole stream!");
                stream.Add(data[k]);
                k += dictionary[data[k]].Length;
            }

            this.Stream = stream.ToArray();
        }
    }
}