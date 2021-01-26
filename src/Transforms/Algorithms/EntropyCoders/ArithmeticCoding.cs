using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders
{
    public class ArithmeticCoding : ICoder<byte[], byte[]>
    {
        public IModel<byte> Model { get; }

        public ArithmeticCoding(IModel<byte> model) =>
            this.Model = model;

        public IAsyncEnumerable<byte[]> Encode(IAsyncEnumerable<byte[]> input)
        {
            throw new System.NotImplementedException();
        }
    }
}