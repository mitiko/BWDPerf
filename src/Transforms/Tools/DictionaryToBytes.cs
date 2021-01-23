using System;
using System.Collections.Generic;
using BWDPerf.Transforms.Entities;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Tools
{
    public class DictionaryToBytes : ICoder<(byte[], DictionaryIndex[]), byte[]>
    {
        public async IAsyncEnumerable<byte[]> Encode(IAsyncEnumerable<(byte[], DictionaryIndex[])> input)
        {
            await foreach (var (dictionary, stream) in input)
            {
                // Write out the dictionary
                yield return dictionary;
                var dictionarySize = BitConverter.ToInt32(dictionary[0..4]);
                var bitsPerWord = Convert.ToInt32(Math.Ceiling(Math.Log2(dictionarySize)));

                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(stream.Length));
                Console.WriteLine($"Stream size is {stream.Length} and dict size is {dictionarySize}");
                var bits = new Queue<bool>();
                foreach (var index in stream)
                {
                    // Write bits of the current index to the queue
                    for (int i = bitsPerWord - 1; i >= 0; i--)
                        bits.Enqueue((index.Index & (1 << i)) != 0);

                    // Flush out buffered bytes if any
                    while (bits.Count >= 8) bytes.Add(ReadFromBitQueue());
                }

                // If we still have bits, write the rest and pad them with zeroes
                if (bits.Count > 0) bytes.Add(ReadFromBitQueue());

                yield return bytes.ToArray();

                byte ReadFromBitQueue()
                {
                    byte n = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        n <<= 1;
                        if (bits.TryDequeue(out bool checkBit))
                            if (checkBit) n += 1;
                    }
                    return n;
                }
            }
        }
    }
}