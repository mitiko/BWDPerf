using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders
{
    public class rANS<TSymbol> : ICoder<TSymbol[], byte>
    {
        public IRANSModel<TSymbol> Model { get; }
        // Normalization range is [L, bL), where L = kM to ensure b-uniqueness
        // M is the denominator = sum_{i=s} (f_s)
        // log2(b) is how many bits at a time we write to the stream.
        // This implementation is byte aligned, so b = 256
        // L is 1<<23, so bL = (1<<23)*256 = 1<<31 < uint.MaxValue
        public const uint _L = 1u << 23;
        public const int _logB = 8; // b = 256, so we emit a byte when normalizing
        public const int _bMask = 255; // mask to get the last logB bits

        public rANS(IRANSModel<TSymbol> model) =>
            this.Model = model;

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<TSymbol[]> input)
        {
            await foreach (var buffer in input)
            {
                // Initialize the model
                InitializeModel(buffer.AsSpan());

                uint state = _L;
                for (int i = buffer.Length - 1; i >= 0 ; i--)
                {
                    var symbol = buffer[i];
                    int freq = this.Model.GetFrequency(symbol);
                    int cdf = this.Model.GetCumulativeFrequency(symbol);
                    int n = this.Model.LogDenominator;
                    // Renormalize state by emitting a byte
                    var state_max = ((_L >> n) << _logB) * freq;
                    while(state >= state_max)
                    {
                        yield return (byte) (state & _bMask);
                        state >>= _logB;
                    }
                    state = (uint) (((state / freq) << n) + state % freq + cdf);
                }
                // Output the state at the end. There are optimizations for using log(state) bits, but for now 32 bits is ok
                foreach (var b in BitConverter.GetBytes(state)) yield return b;
            }
        }

        public void InitializeModel(Span<TSymbol> buffer)
        {
            var dict = new Dictionary<TSymbol, int>();
            foreach (var symbol in buffer)
            {
                if (!dict.ContainsKey(symbol))
                    dict.Add(symbol, 1);
                else
                    dict[symbol]++;
            }
            this.Model.Initialize(ref dict);
            // TODO: write this data as a header, since it's static probabilities
            // Or we can use a dynamic approach where we use an extra symbol that is followed by an unseen literal, which we encode raw
        }
    }
}