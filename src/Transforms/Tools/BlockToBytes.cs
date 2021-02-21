using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Tools
{
    public class BlockToBytes : ICoder<BWDBlock, ReadOnlyMemory<byte>>, IDecoder<BWDBlock, ReadOnlyMemory<byte>>
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

        public async IAsyncEnumerable<ReadOnlyMemory<byte>> Decode(IAsyncEnumerable<BWDBlock> input)
        {
            await foreach (var block in input)
            {
                int stokenStartIndex = 0;
                for (int i = 0; i < block.Stream.Length; i++)
                {
                    var index = block.Stream[i];
                    if (index != block.Dictionary.STokenIndex)
                    {
                        yield return block.Dictionary[index];
                        continue;
                    }
                    var data = new List<byte>();
                    // TODO: We can use .Slice if we measure which index the current stoken ends at
                    for (int j = stokenStartIndex; j < block.Dictionary.SToken.Length; j++)
                    {
                        if (block.Dictionary.SToken.Span[j] == 0xff)
                        {
                            if (j + 1 >= block.Dictionary.SToken.Length) break;

                            if (block.Dictionary.SToken.Span[j + 1] == 0xff)
                                { data.Add(0xff); j++; }
                            else
                                { stokenStartIndex = j + 1; break; }
                        }
                        else data.Add(block.Dictionary.SToken.Span[j]);
                    }
                    yield return data.ToArray();
                }
            }
        }
    }
}