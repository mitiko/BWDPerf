using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Modeling;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.RANSNibbled
{
    public class RANSNibbledEncoder<TSymbol> : ICoder<ReadOnlyMemory<TSymbol>, byte>
    {
        public INibbleAlphabet<TSymbol> Alphabet { get; }
        public IQuantizer Model { get; }

        // Normalization range is [L, bL), where L = kM to ensure b-uniqueness
        // M is the denominator = sum_{i=s} (f_s)
        // log2(b) is how many bits at a time we write to the stream.
        // This implementation is byte aligned, so b = 256
        // L is 1<<23, so bL = (1<<23)*256 = 1<<31 < uint.MaxValue
        public const uint _L = 1u << 23;
        public const uint _bMask = 255; // mask to get the last logB bits
        public const int _logB = 8; // b = 256, so we emit a byte when normalizing

        public RANSNibbledEncoder(INibbleAlphabet<TSymbol> alphabet, IQuantizer model)
        {
            this.Alphabet = alphabet;
            this.Model = model;
        }

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<ReadOnlyMemory<TSymbol>> input)
        {
            await foreach (var buffer in input)
            {
                var cdfs = new uint[2 * buffer.Length];
                var freqs = new uint[2 * buffer.Length];

                uint state = _L;
                int n = this.Model.Accuracy;
                int tenPercent = buffer.Length / 10;
                for (int i = 0; i < buffer.Length; i++)
                {
                    // Get the 2 nibbles (or symbol locations)
                    var (s1, s2) = this.Alphabet[buffer.Span[i]];

                    var pred = this.Model.Predict();
                    pred.Normalize();
                    var (cdf1, freq1) = this.Model.Encode(s1, pred);                    
                    var (cdf2, freq2) = this.Model.Encode(s2, pred);
                    
                    this.Model.Update(s1);
                    this.Model.Update(s2);
                    
                    cdfs[2 * i]      = cdf1;
                    cdfs[2 * i + 1]  = cdf2;
                    freqs[2 * i]     = freq1;
                    freqs[2 * i + 1] = freq2;

                    if (i % tenPercent == 0) Console.WriteLine($"[RANS] Predicted {10 * i / tenPercent}%");
                }

                Console.WriteLine("[RANS] Started coding");
                var stream = new Stack<byte>(capacity: buffer.Length / 2);
                for (int i = 2 * buffer.Length - 1; i >= 0 ; i--)
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
                Console.WriteLine("[RANS] Ended coding");

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