using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    public class BWDDecoder : IDecoder<ushort, ReadOnlyMemory<byte>>
    {
        public BWDictionary Dictionary { get; }

        public BWDDecoder(BWDictionary dictionary) => this.Dictionary = dictionary;

        public async IAsyncEnumerable<ReadOnlyMemory<byte>> Decode(IAsyncEnumerable<ushort> input)
        {
            await foreach(var index in input)
                yield return this.Dictionary[index];
        }
    }
}