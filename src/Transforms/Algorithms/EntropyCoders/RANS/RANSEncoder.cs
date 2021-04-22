using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.RANS
{
    public class RANSEncoder<TSymbol> : ICoder<ReadOnlyMemory<TSymbol>, byte>
    {
        public IAlphabet<TSymbol> Alphabet { get; }
        public IQuantizer Model { get; }

        // Normalization range is [L, bL), where L = kM to ensure b-uniqueness
        // M is the denominator = sum_{i=s} (f_s)
        // log2(b) is how many bits at a time we write to the stream.
        // This implementation is byte aligned, so b = 256
        // L is 1<<23, so bL = (1<<23)*256 = 1<<31 < uint.MaxValue
        public const uint _L = 1u << 23;
        public const uint _bMask = 255; // mask to get the last logB bits
        public const int _logB = 8; // b = 256, so we emit a byte when normalizing

        public RANSEncoder(IAlphabet<TSymbol> alphabet, IQuantizer model)
        {
            this.Alphabet = alphabet;
            this.Model = model;
        }

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<ReadOnlyMemory<TSymbol>> input)
        {
            await foreach (var buffer in input)
            {
                var cdfs = new uint[buffer.Length];
                var freqs = new uint[buffer.Length];

                uint state = _L;
                int n = this.Model.Accuracy;
                for (int i = 0; i < buffer.Length; i++)
                {
                    var symbol = this.Alphabet[buffer.Span[i]];
                    var (cdf, freq) = this.Model.Encode(symbol, this.Model.Predict());
                    cdfs[i] = cdf;
                    freqs[i] = freq;
                    this.Model.Update(symbol);
                }

                var stream = new Stack<byte>(capacity: buffer.Length / 2);
                for (int i = buffer.Length - 1; i >= 0 ; i--)
                {
                    uint cdf = cdfs[i]; uint freq = freqs[i];

                    // Renormalize state by emitting a byte
                    uint state_max = ((_L << _logB) >> n) * freq;
                    while(state >= state_max)
                    {
                        stream.Push((byte) (state & _bMask));
                        state >>= _logB;
                    }
                    state = ((state / freq) << n) + state % freq + cdf;
                }

                // Output the state at the end. There are optimizations for using log(state) bits, but for now 32 bits is ok
                var bytes = BitConverter.GetBytes(state);
                foreach (var b in bytes)
                    yield return b;
                while (stream.Count > 0)
                    yield return stream.Pop();
            }
        }
    }
}