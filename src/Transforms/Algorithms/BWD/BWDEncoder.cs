using System;
using System.Collections.Generic;
using BWDPerf.Transforms.Entities;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    // Encode the buffer and pass it on as individual symbols or as blocks of indices
    public class BWDEncoder : ICoder<ReadOnlyMemory<byte>, (ReadOnlyMemory<byte>, ReadOnlyMemory<DictionaryIndex>)>
    {
        private BWD BWD { get; }

        public BWDEncoder(Options options) =>
            this.BWD = new BWD(options);

        public async IAsyncEnumerable<(ReadOnlyMemory<byte>, ReadOnlyMemory<DictionaryIndex>)> Encode(IAsyncEnumerable<ReadOnlyMemory<byte>> input)
        {
            await foreach (var buffer in input)
            {
                var dictionarySize = this.BWD.CalculateDictionary(buffer);
                byte[] dictionary = EncodeDictionary(dictionarySize);
                DictionaryIndex[] stream = EncodeStream(buffer, dictionarySize);

                yield return (dictionary, stream);
            }
        }

        private byte[] EncodeDictionary(int dictionarySize)
        {
            var buffer = new List<byte>(capacity: dictionarySize);
            buffer.AddRange(BitConverter.GetBytes(dictionarySize));
            for (int i = 0; i < dictionarySize; i++)
            {
                if (i == dictionarySize - 1 && this.BWD.STokenData.Length > 0)
                    buffer.AddRange(BitConverter.GetBytes(this.BWD.STokenData.Length));
                else
                    buffer.Add((byte) this.BWD.Dictionary[i].Length);

                foreach (var symbol in this.BWD.Dictionary[i])
                    buffer.Add(symbol);
            }
            return buffer.ToArray();
        }

        private DictionaryIndex[] EncodeStream(ReadOnlyMemory<byte> buffer, int dictionarySize)
        {
            // TODO: Use the FM-index to do parsing in O(n)
            var stoken = new DictionaryIndex(this.BWD.Dictionary.Length - 1);
            var data = new int[buffer.Length];
            int wordCount = 0;
            for (int k = 0; k < data.Length; k++)
                data[k] = stoken.Index; // Initialize with <s> token

            for (int i = 0; i < dictionarySize; i++)
            {
                if (i == stoken.Index) break; // Don't do this for <s> tokens, they'll be what's left behind
                var word = this.BWD.Dictionary[i];
                for (int j = 0; j < buffer.Length; j++)
                {
                    // check if location is used
                    if (data[j] != stoken.Index) continue;
                    if (j + word.Length - 1 >= buffer.Length) break; // can't fit word
                    var match = true;
                    for (int s = 0; s < word.Length; s++)
                        if (buffer.Span[j + s] != word[s] || data[j+s] != stoken.Index) { match = false; break; }

                    if (match == true)
                    {
                        wordCount++;
                        for (int k = 0; k < word.Length; k++)
                            data[j+k] = i;
                    }
                }
            }

            var stream = new List<DictionaryIndex>(capacity: wordCount);
            for (int k = 0; k < data.Length;)
            {
                stream.Add(new DictionaryIndex((int) data[k]));
                int offset;
                if (data[k] == stoken.Index)
                {
                    for (offset = 0; k+offset >= data.Length ? false : data[k] == data[k+offset]; offset++);
                }
                else
                    offset = this.BWD.Dictionary[data[k]].Length;
                k += offset;
            }

            // int a = 0;
            // foreach (var word in stream)
            // {
            //     var str = "";
            //     if (word.Index == stoken.Index)
            //     {
            //         str = "<s>";
            //     }
            //     else
            //     {
            //         foreach (var s in this.BWD.Dictionary[word.Index])
            //         {
            //             if (s == (byte) '\n')
            //                 str += (char) s;
            //             else
            //                 str += (char) s;
            //         }
            //         str = $"\"{str}\"";
            //     }
            //     Console.WriteLine($"{a} -- {word.Index} -- {str}");
            //     a++;
            // }

            return stream.ToArray();
        }
    }
}