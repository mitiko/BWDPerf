using System;
using System.Collections.Generic;
using System.Diagnostics;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.RANS
{
    public class RANSDecoder<TSymbol> : IDecoder<byte, TSymbol>
    {
        public IAlphabet<TSymbol> Alphabet { get; }
        public IQuantizer Model { get; }

        // See encoder for explanation of these constants
        public const uint _L = 1u << 23;
        public const int _logB = 8;

        public RANSDecoder(IAlphabet<TSymbol> alphabet, IQuantizer model)
        {
            this.Alphabet = alphabet;
            this.Model = model;
        }

        public async IAsyncEnumerable<TSymbol> Decode(IAsyncEnumerable<byte> input)
        {
            var enumerator = input.GetAsyncEnumerator();
            var byteQueue = new Queue<byte>(capacity: 8);
            uint state = await ByteStreamHelper.GetUInt32Async(enumerator);
            int n = this.Model.Accuracy;
            uint mask = (uint) (1 << n) - 1;

            while (true)
            {
                Debug.Assert(_L <= state, "State was under bound [L, bL)");
                Debug.Assert(state < (_L << _logB), "State was over bound [L, bl)");

                // Decode
                uint cdf = state & mask;
                var prediction = this.Model.Predict();
                var symbol = this.Model.Decode(cdf, prediction);
                yield return this.Alphabet[symbol];

                // Update model, update state
                var (eCDF, eFreq) = this.Model.Encode(symbol, prediction);
                state = eFreq * (state >> n) + (state & mask) - eCDF;
                this.Model.Update(symbol);

                // Check if EOF and read bytes into the queue
                while (byteQueue.Count <= 8 && await enumerator.MoveNextAsync())
                    byteQueue.Enqueue(enumerator.Current);
                if (byteQueue.Count == 0)
                    break;

                // Read bytes into the state (renormalization)
                while (state < _L)
                    state = (state << _logB) | byteQueue.Dequeue();
            }
        }
    }
}