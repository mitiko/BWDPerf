using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    public class BWDParser : ICoder<ReadOnlyMemory<byte>, ReadOnlyMemory<ushort>>
    {
        public BWDictionary Dictionary { get; }

        public BWDParser(BWDictionary dictionary) => Dictionary = dictionary;

        // TODO: Rewrite the parser
        public async IAsyncEnumerable<ReadOnlyMemory<ushort>> Encode(IAsyncEnumerable<ReadOnlyMemory<byte>> input)
        {
            await foreach (var buffer in input)
            {
                // TODO: without s token, we can do parsing in O(n), by finding the first word that matches
                // NO TODO: Use the FM-index to do parsing in O(n)
                var data = new ushort[buffer.Length];
                data.AsSpan().Fill(ushort.MaxValue);

                for (ushort i = 0; i < this.Dictionary.Count; i++)
                {
                    var word = this.Dictionary[i];
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
                                data[j+k] = i;
                        }
                    }
                }

                var stream = new List<ushort>();
                for (int k = 0; k < data.Length;)
                {
                    if (data[k] == ushort.MaxValue) throw new Exception("Dictionary doesn't cover the whole stream!");
                    stream.Add(data[k]);
                    k += this.Dictionary[data[k]].Length;
                }

                Console.WriteLine("[BWD] Parsed a block");
                yield return stream.ToArray();
            }
        }
    }
}