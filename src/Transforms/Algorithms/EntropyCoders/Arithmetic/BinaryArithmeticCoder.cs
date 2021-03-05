using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.Arithmetic
{
    public class BinaryArithmeticCoder<TSymbol> : ICoder<ReadOnlyMemory<TSymbol>, byte>
    {
        public IModel<bool> Model { get; }

        public BinaryArithmeticCoder(IModel<bool> model) =>
            this.Model = model;

        public IAsyncEnumerable<byte> Encode(IAsyncEnumerable<ReadOnlyMemory<TSymbol>> input)
        {
            throw new NotImplementedException();
            // await foreach (var buffer in input)
            // {
            //     // uint low = 0u;
            //     // uint high = uint.MaxValue;
            //     InitializeModel();
            //     yield return 0xff; // TODO
            // }
        }

        private void InitializeModel()
        {
            var dict = new Dictionary<bool, int>();
            dict.Add(false, 1);
            dict.Add(true, 1);
            this.Model.Initialize(ref dict);
        }
    }
}