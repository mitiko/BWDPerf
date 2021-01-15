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

                var bytes = new List<byte>();
                var bits = new Queue<bool>();
                foreach (var index in stream)
                {
                    // Write bits of the current index to the queue
                    for (int i = index.BitsToUse - 1; i >= 0; i--)
                        bits.Enqueue((index.Index & (1 << i)) != 0);

                    // Flush out buffered bytes if any
                    while (bits.Count >= 8) bytes.Add(ReadFromBitBuffer());
                }

                // If we still have bits, write the rest and pad them with zeroes
                if (bits.Count > 0) bytes.Add(ReadFromBitBuffer());

                yield return bytes.ToArray();

                byte ReadFromBitBuffer()
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