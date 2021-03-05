using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    public class BWDRawDecoder : IDecoder<byte, BWDBlock>
    {
        public async IAsyncEnumerable<BWDBlock> Decode(IAsyncEnumerable<byte> input)
        {
            var enumerator = input.GetAsyncEnumerator();
            while (true)
            {
                var dictionarySize = await ReadInteger(enumerator);
                if (dictionarySize == null) break; // We've reached end of stream

                var dictionary = await ReadDictionary(enumerator, (int) dictionarySize);
                int streamLength = (int) await ReadInteger(enumerator);

                var stream = new List<int>(capacity: streamLength);
                var bits = new Queue<bool>();

                // Read from stream
                for (int symbolsRead = 0; symbolsRead < streamLength;)
                {
                    // Read from bit queue
                    while (bits.Count >= dictionary.IndexSize && symbolsRead < streamLength)
                    {
                        int index = ReadFromBitQueue(ref bits, dictionary.IndexSize);
                        stream.Add(index);
                        symbolsRead++;
                    }

                    if (symbolsRead >= streamLength) break;
                    // Write to bit queue
                    var nextByte = await GetNextByte(enumerator);
                    for (int j = 7; j >= 0; j--)
                        bits.Enqueue((nextByte & (1 << j)) != 0);
                    // Don't care about the bits we're discarding, they were padding
                    // TODO: Try replace bit queues with an integer
                }

                // TODO: Fix everywhere we use arrays but can deal without - meaning, remove LINQ as a dependecy cause it slows us down
                yield return new BWDBlock(dictionary, new BWDStream(stream.ToArray()));
            }
        }

        private async Task<BWDictionary> ReadDictionary(IAsyncEnumerator<byte> enumerator, int dictionarySize)
        {
            var indexSize = Convert.ToInt32(Math.Ceiling(Math.Log2(dictionarySize)));
            var dictionary = new BWDictionary(indexSize);

            for (int i = 0; i < dictionarySize; i++)
            {
                int length = 0;
                if (i == dictionary.STokenIndex)
                    length = (int) await ReadInteger(enumerator);
                else
                    length = await GetNextByte(enumerator);
                var word = new byte[length];
                // Console.WriteLine($"Reading word length to be {length} and index is {i}");
                for (int j = 0; j < word.Length; j++)
                    word[j] = await GetNextByte(enumerator);
                dictionary[i] = word;
            }
            return dictionary;
        }

        private int ReadFromBitQueue(ref Queue<bool> bits, int bitsPerWord)
        {
            int n = 0;
            for (int j = 0; j < bitsPerWord; j++)
            {
                n <<= 1;
                if (bits.Dequeue())
                    n++;
            }
            return n;
        }

        private async Task<int?> ReadInteger(IAsyncEnumerator<byte> enumerator)
        {
            var int32Arr = new byte[4];
            for (int k = 0; k < 4; k++)
            {
                try { int32Arr[k] = await GetNextByte(enumerator); }
                catch
                {
                    if (k == 0) return null;
                    else throw;
                }
            }
            return BitConverter.ToInt32(int32Arr);
        }

        private async Task<byte> GetNextByte(IAsyncEnumerator<byte> enumerator)
        {
            if (! await enumerator.MoveNextAsync()) throw new Exception("Problem decoding");
            return enumerator.Current;
        }
    }
}