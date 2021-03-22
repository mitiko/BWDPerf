using System;
using System.Collections.Generic;

namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public class BWDStream
    {
        private ReadOnlyMemory<int> Stream { get; set; }
        public int Length => this.Stream.Length;
        public int this[int index] => this.Stream.Span[index];

        public BWDStream(ReadOnlyMemory<int> stream) =>
            this.Stream = stream;

        public BWDStream(ReadOnlyMemory<byte> buffer, BWDictionary dictionary)
        {
            // TODO: without s token, we can do parsing in O(n), by finding the first word that matches 
            // NO TODO: Use the FM-index to do parsing in O(n)
            var data = new int[buffer.Length];
            for (int k = 0; k < data.Length; k++)
                data[k] = -1;

            for (int i = 0; i < dictionary.Count; i++)
            {
                var word = dictionary[i];
                for (int j = 0; j < buffer.Length; j++)
                {
                    // check if location is used
                    if (data[j] != -1) continue;
                    if (j + word.Length - 1 >= buffer.Length) break; // can't fit word
                    var match = true;
                    for (int s = 0; s < word.Length; s++)
                        if (buffer.Span[j + s] != word.Span[s] || data[j+s] != -1) { match = false; break; }

                    if (match == true)
                    {
                        for (int k = 0; k < word.Length; k++)
                            data[j+k] = i;
                    }
                }
            }

            var stream = new List<int>();
            for (int k = 0; k < data.Length;)
            {
                if (data[k] == -1) throw new Exception("Dictionary doesn't cover the whole stream!");
                stream.Add(data[k]);
                k += dictionary[data[k]].Length;
            }

            this.Stream = stream.ToArray();
        }
    }
}