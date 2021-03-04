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
            // TODO: Use the FM-index to do parsing in O(n)
            var stoken = dictionary.STokenIndex;
            var data = new int[buffer.Length];
            int wordCount = 0;
            for (int k = 0; k < data.Length; k++)
                data[k] = stoken; // Initialize with <s> token

            for (int i = 0; i < dictionary.WordCount; i++)
            {
                if (i == stoken) break; // Don't do this for <s> tokens, they'll be what's left behind
                var word = dictionary[i];
                for (int j = 0; j < buffer.Length; j++)
                {
                    // check if location is used
                    if (data[j] != stoken) continue;
                    if (j + word.Length - 1 >= buffer.Length) break; // can't fit word
                    var match = true;
                    for (int s = 0; s < word.Length; s++)
                        if (buffer.Span[j + s] != word.Span[s] || data[j+s] != stoken) { match = false; break; }

                    if (match == true)
                    {
                        wordCount++;
                        for (int k = 0; k < word.Length; k++)
                            data[j+k] = i;
                    }
                }
            }

            // var stream = new List<int>(capacity: 2 * wordCount);
            var stream = new List<int>();
            for (int k = 0; k < data.Length;)
            {
                stream.Add(data[k]);
                int offset;
                if (data[k] == stoken)
                {
                    for (offset = 0; k+offset >= data.Length ? false : data[k] == data[k+offset]; offset++);
                }
                else
                    offset = dictionary[data[k]].Length;
                k += offset;
            }

            foreach (var index in stream)
            {
                if (index == stoken)
                {
                    Console.WriteLine("<s> token");
                }
                else
                {
                    var word = dictionary[index];
                    var str = "";
                    foreach (var symbol in word.Span)
                    {
                        str += (char) symbol;
                    }
                    Console.WriteLine($"\"{str}\"");
                }
            }
            var s1 = dictionary[stoken];
            var str1 = "";
            foreach (var symbol in s1.Span)
            {
                if (symbol == 0xff)
                    str1 += '$';
                else
                    str1 += (char) symbol;
            }
            Console.WriteLine($"stoken: --- \"{str1}\"");
            Console.WriteLine("DICT:");
            for (int i = 0; i < dictionary.Length; i++)
            {
                var w = dictionary[i];
                var sstr = "";
                foreach (var symbol in w.Span)
                {
                    sstr += (char) symbol;
                }
                Console.WriteLine($"dict -- \"{sstr}\"");
            }

            this.Stream = stream.ToArray();
        }
    }
}