using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    public class BWDDecoder : IDecoder<byte, ReadOnlyMemory<byte>>
    {
        public async IAsyncEnumerable<ReadOnlyMemory<byte>> Decode(IAsyncEnumerable<byte> input)
        {
            var enumerator = input.GetAsyncEnumerator();
            while (true)
            {
                var (dictionarySize, endOfStream) = await ReadDictionarySize(enumerator);

                if (endOfStream == true) break;
                var bitsPerWord = Convert.ToInt32(Math.Ceiling(Math.Log2(dictionarySize))); // bits per token
                int stokenIndex = (1 << bitsPerWord) - 1;
                Console.WriteLine($"Read dictioary size to be {dictionarySize}");
                Console.WriteLine($"Calculated bitsPerWord to be {bitsPerWord}");
                Console.WriteLine($"Calculated stokenIndex to be {stokenIndex}");

                var dictionary = await CopyDictionary(enumerator, dictionarySize, stokenIndex);

                int streamLength = await ReadStreamLength(enumerator);
                Console.WriteLine($"Read stream size to be {streamLength}");
                var stream = new List<byte>();
                var bits = new Queue<bool>();
                int stokenStartIndex = 0;

                // Read from stream
                for (int i = 0; i < streamLength;)
                {
                    // Read from bit queue
                    while (bits.Count >= bitsPerWord)
                    {
                        // We'll read 1 word, by looking up the index in the dictionary
                        int index = ReadFromBitQueue(bits, bitsPerWord); i++;
                        if (i > streamLength) break;

                        if (index == stokenIndex)
                        {
                            var data = ReadFromSToken(stokenIndex, ref stokenStartIndex, ref dictionary);
                            yield return data.ToArray();
                        }
                        else yield return dictionary[index];
                    }

                    if (i >= streamLength) break;
                    // Write to bit queue
                    await GetNextByte(enumerator);
                    for (int j = 7; j >= 0; j--)
                        bits.Enqueue((enumerator.Current & (1 << j)) != 0);
                    // Don't care about the bits we're discarding
                }
            }

        }

        private async Task<byte[][]> CopyDictionary(IAsyncEnumerator<byte> enumerator, int dictionarySize, int stokenIndex)
        {
            await GetNextByte(enumerator); // read r
            var r = enumerator.Current;
            byte[][] dictionary = new byte[dictionarySize][];
            var int32Arr = new byte[4];
            for (int i = 0; i < dictionary.Length; i++)
            {
                await GetNextByte(enumerator);
                int count = enumerator.Current;
                // if the options used were for a bigger dictionary but we couldn't fill it, the stoken is actually at another index
                if (i == stokenIndex && (1 << r) - 1 == stokenIndex)
                {
                    // This is an SToken. Read a count as int32 not as a byte
                    for (int k = 0; k < 4; k++)
                    {
                        int32Arr[k] = enumerator.Current;
                        if (k == 3) break; // Make sure we don't read 5 bytes, bc the stream count will be unaligned and too big
                        await GetNextByte(enumerator);
                    }
                    count = BitConverter.ToInt32(int32Arr, 0);
                }
                dictionary[i] = new byte[count];
                for (int j = 0; j < count; j++)
                {
                    await GetNextByte(enumerator);
                    dictionary[i][j] = enumerator.Current;
                }
            }
            return dictionary;
        }

        private async Task<(int dictionarySize, bool endOfStream)> ReadDictionarySize(IAsyncEnumerator<byte> enumerator)
        {
            var endOfStream = false;
            var int32Arr = new byte[4];
            for (int k = 0; k < 4; k++)
            {
                try { await GetNextByte(enumerator); }
                catch (Exception)
                {
                    if (k == 0) { endOfStream = true; break; }
                    else throw;
                }
                int32Arr[k] = enumerator.Current;
            }
            return (BitConverter.ToInt32(int32Arr, 0), endOfStream);
        }

        private async Task<int> ReadStreamLength(IAsyncEnumerator<byte> enumerator)
        {
            var int32Arr = new byte[4];
            for (int k = 0; k < 4; k++)
            {
                await GetNextByte(enumerator);
                int32Arr[k] = enumerator.Current;
            }
            return BitConverter.ToInt32(int32Arr, 0);
        }

        private int ReadFromBitQueue(Queue<bool> bits, int bitsPerWord)
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

        private List<byte> ReadFromSToken(int stokenIndex, ref int stokenStartIndex, ref byte[][] dictionary)
        {
            var data = new List<byte>();
            // This is an SToken. Only read from SToken to stream until escape char.
            for (int j = stokenStartIndex; j < dictionary[stokenIndex].Length; j++)
            {
                if (dictionary[stokenIndex][j] == 0xff)
                {
                    if (j + 1 >= dictionary[stokenIndex].Length) break;

                    if (dictionary[stokenIndex][j + 1] == 0xff)
                        { data.Add(0xff); j++; }
                    else
                        { stokenStartIndex = j + 1; break; }
                }
                else data.Add(dictionary[stokenIndex][j]);
            }
            return data;
        }

        private async Task GetNextByte(IAsyncEnumerator<byte> enumerator)
        {
            if (! await enumerator.MoveNextAsync()) throw new Exception("Problem decoding");
        }
    }
}