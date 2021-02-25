using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    // Encode the buffer and pass it on as individual symbols or as blocks of indices
    public class BWDEncoder : ICoder<ReadOnlyMemory<byte>, BWDBlock>
    {
        private BWD BWD { get; }

        public BWDEncoder(Options options, IBWDRanking ranking) =>
            this.BWD = new BWD(options, ranking);

        public async IAsyncEnumerable<BWDBlock> Encode(IAsyncEnumerable<ReadOnlyMemory<byte>> input)
        {
            await foreach (var buffer in input)
            {
                var dictionary = this.BWD.CalculateDictionary(buffer);
                var stream = new BWDStream(buffer, dictionary);

                yield return new BWDBlock(dictionary, stream);
            }
        }
    }
}