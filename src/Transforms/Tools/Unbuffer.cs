using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Tools
{
    public class Unbuffer<T> : ICoder<T[], T>, IDecoder<T[], T>
    {
        public async IAsyncEnumerable<T> Encode(IAsyncEnumerable<T[]> input)
        {
            await foreach (var seq in input)
            {
                foreach (var item in seq)
                {
                    yield return item;
                }
            }
        }

        public async IAsyncEnumerable<T> Decode(IAsyncEnumerable<T[]> input)
        {
            await foreach (var seq in input)
            {
                foreach (var item in seq)
                {
                    yield return item;
                }
            }
        }
    }
}