using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    public class BWDictionaryDecoder : IDecoder<byte, BWDictionary>
    {
        public async IAsyncEnumerable<BWDictionary> Decode(IAsyncEnumerable<byte> input)
        {
            var enumerator = input.GetAsyncEnumerator();
            var dictionary = new BWDictionary();
            var dictionarySize = await ReadInteger(enumerator);

            for (ushort i = 0; i < dictionarySize; i++)
            {
                int length = await GetNextByte(enumerator);
                var word = new byte[length];
                for (int j = 0; j < word.Length; j++)
                    word[j] = await GetNextByte(enumerator);
                dictionary[i] = word;
            }
            yield return dictionary;
        }

        private static async Task<int> ReadInteger(IAsyncEnumerator<byte> enumerator)
        {
            var int32Arr = new byte[4];
            for (int k = 0; k < 4; k++) int32Arr[k] = await GetNextByte(enumerator);
            return BitConverter.ToInt32(int32Arr);
        }

        private static async Task<byte> GetNextByte(IAsyncEnumerator<byte> enumerator)
        {
            if (!await enumerator.MoveNextAsync()) throw new Exception("Problem decoding dictionary");
            return enumerator.Current;
        }
    }
}