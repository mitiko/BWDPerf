using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Tools
{
    public class DictionaryToBytes : ICoder<BWDBlock, ReadOnlyMemory<byte>>
    {
        public async IAsyncEnumerable<ReadOnlyMemory<byte>> Encode(IAsyncEnumerable<BWDBlock> input)
        {
            await foreach (var block in input)
            {
                // Write out the dictionary
                yield return block.Dictionary.Serialize();
                var bitsPerWord = Convert.ToInt32(Math.Ceiling(Math.Log2(block.Dictionary.WordCount)));

                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(block.Stream.Length));
                var bits = new Queue<bool>();
                for (int k = 0; k < block.Stream.Length; k++)
                {
                    // Write bits of the current index to the queue
                    for (int i = bitsPerWord - 1; i >= 0; i--)
                        bits.Enqueue((block.Stream[k] & (1 << i)) != 0);

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