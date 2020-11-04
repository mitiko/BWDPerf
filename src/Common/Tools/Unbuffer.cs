using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Tools
{
    public class Unbuffer<T> : ICoder<IEnumerable<T>, T>
    {
        public async IAsyncEnumerable<T> Encode(IAsyncEnumerable<IEnumerable<T>> input)
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