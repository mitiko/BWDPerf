using System;
using System.Collections.Generic;
using BWDPerf.Common.Entities;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Tools
{
    public class DictionaryToBytes : ICoder<(byte[], DictionaryIndex[]), byte[]>
    {
        public async IAsyncEnumerable<byte[]> Encode(IAsyncEnumerable<(byte[], DictionaryIndex[])> input)
        {
            await foreach (var (dictionary, stream) in input)
            {
                // Write out the dictionary
                yield return dictionary;
                // TODO: Remove BitsToUse property, by checking the first 4 bytes of the dictionary for the dictionry size

                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(stream.Length));
                Console.WriteLine($"Stream size is {stream.Length}");
                var bits = new Queue<bool>();
                foreach (var index in stream)
                {
                    // Write bits of the current index to the queue
                    for (int i = index.BitsToUse - 1; i >= 0; i--)
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