using System.Collections.Generic;
using System.Diagnostics;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.RANSNibbled
{
    public class RANSNibbledDecoder<TSymbol> : IDecoder<byte, TSymbol>
    {
        public INibbleAlphabet<TSymbol> Alphabet { get; }
        public IQuantizer Model { get; }

        // See encoder for explanation of these constants
        public const uint _L = 1u << 23;
        public const int _logB = 8;

        public RANSNibbledDecoder(INibbleAlphabet<TSymbol> alphabet, IQuantizer model)
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

                // Predict
                var prediction = this.Model.Predict();
                
                // Decode first nibble (symbol)
                uint cdf = state & mask;
                var s1 = this.Model.Decode(cdf, prediction);
                var (eCDF1, eFreq1) = this.Model.Encode(s1, prediction);
                state = eFreq1 * (state >> n) + (state & mask) - eCDF1;

                // Update the state

                // Check if EOF and read bytes from the stream to write into the queue
                while (byteQueue.Count <= 8 && await enumerator.MoveNextAsync())
                    byteQueue.Enqueue(enumerator.Current);
                if (byteQueue.Count == 0 && state <= _L)
                    break;

                // Read bytes into the state (renormalization)
                while (state < _L)
                    state = (state << _logB) | byteQueue.Dequeue();
                
                // Decode the second nibble (symbol)
                cdf = state & mask;
                var s2 = this.Model.Decode(cdf, prediction);
                var (eCDF2, eFreq2) = this.Model.Encode(s2, prediction);
                state = eFreq2 * (state >> n) + (state & mask) - eCDF2;

                // Return the symbol that the two nibbles represent
                yield return this.Alphabet[(s1, s2)];

                // Update the model
                this.Model.Update(s1);
                this.Model.Update(s2);

                // Update the state again (after the secodn nibble decode)

                // Check if EOF and read bytes from the stream to write into the queue
                while (byteQueue.Count <= 8 && await enumerator.MoveNextAsync())
                    byteQueue.Enqueue(enumerator.Current);
                if (byteQueue.Count == 0 && state <= _L)
                    break;

                // Read bytes into the state (renormalization)
                while (state < _L)
                    state = (state << _logB) | byteQueue.Dequeue();
            }
        }
    }
}