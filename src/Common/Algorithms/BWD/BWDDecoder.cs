using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Algorithms.BWD
{
    public class BWDDecoder : IDecoder<byte, byte[]>
    {
        public async IAsyncEnumerable<byte[]> Decode(IAsyncEnumerable<byte> input)
        {
            var enumerator = input.GetAsyncEnumerator();
            while (true)
            {
                bool endOfStream = false;
                var int32Arr = new byte[4];

                byte[][] dictionary;
                for (int k = 0; k < 4; k++)
                {
                    try { await GetNextByte(); }
                    catch (Exception)
                    {
                        if (k == 0) { endOfStream = true; break; }
                        else throw;
                    }
                    int32Arr[k] = enumerator.Current;
                }

                if (endOfStream == true) break;
                dictionary = new byte[BitConverter.ToInt32(int32Arr, 0)][];
                var d = Convert.ToInt32(Math.Ceiling(Math.Log2(dictionary.Length))); // bits per token
                int stokenIndex = (1 << d) - 1;


                // Copy dictionary
                for (int i = 0; i < dictionary.Length; i++)
                {
                    await GetNextByte();
                    int count = enumerator.Current;
                    if (i == stokenIndex)
                    {
                        // This is an SToken. Read a count as int32 not as a byte
                        for (int k = 0; k < 4; k++)
                        {
                            int32Arr[k] = enumerator.Current;
                            if (k == 3) break; // Make sure we don't read 5 bytes, bc the stream count will be unaligned and too big
                            await GetNextByte();
                        }
                        count = BitConverter.ToInt32(int32Arr, 0);
                    }
                    dictionary[i] = new byte[count];
                    for (int j = 0; j < count; j++)
                    {
                        await GetNextByte();
                        dictionary[i][j] = enumerator.Current;
                    }
                }

                for (int i = 0; i < 64; i++)
                {
                    await GetNextByte();
                }

                for (int k = 0; k < 4; k++)
                {
                    await GetNextByte();
                    int32Arr[k] = enumerator.Current;
                }
                int streamLength = BitConverter.ToInt32(int32Arr, 0);
                var stream = new List<byte>();
                var bits = new Queue<bool>();
                int stokenStartIndex = 0;

                // Read from stream
                for (int i = 0; i < streamLength;)
                {
                    // Read from bit queue
                    while (bits.Count >= d)
                    {
                        int index = 0;
                        for (int j = 0; j < d; j++)
                        {
                            index <<= 1;
                            if (bits.Dequeue())
                                index++;
                        }
                        i++; // we read a word index

                        if (index == (1 << d) - 1)
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

                            yield return data.ToArray();
                        }
                        else yield return dictionary[index];
                    }

                    if (i >= streamLength) break;
                    // Write to bit queue
                    await GetNextByte();
                    for (int j = 7; j >= 0; j--)
                        bits.Enqueue((enumerator.Current & (1 << j)) != 0);
                    // Don't care about the bits we're discarding
                }
            }

            async Task GetNextByte()
            {
                if (! await enumerator.MoveNextAsync()) throw new Exception("Problem decoding");
            }
        }
    }
}