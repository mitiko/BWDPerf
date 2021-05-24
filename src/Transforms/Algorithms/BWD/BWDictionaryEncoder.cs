using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    public class BWDictionaryEncoder : ICoder<BWDictionary, ReadOnlyMemory<byte>>
    {
        public async IAsyncEnumerable<ReadOnlyMemory<byte>> Encode(IAsyncEnumerable<BWDictionary> input)
        {
            await foreach (var dictionary in input)
                yield return dictionary.Serialize();
        }
    }
}