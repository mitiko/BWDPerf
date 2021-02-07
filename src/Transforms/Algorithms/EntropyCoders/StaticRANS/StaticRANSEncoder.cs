using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.StaticRANS
{
    public class StaticRANSEncoder<TSymbol> : ICoder<ReadOnlyMemory<TSymbol>, byte>
    {
        public IRANSModel<TSymbol> Model { get; }
        public IConverter<TSymbol> Converter { get; }

        // Normalization range is [L, bL), where L = kM to ensure b-uniqueness
        // M is the denominator = sum_{i=s} (f_s)
        // log2(b) is how many bits at a time we write to the stream.
        // This implementation is byte aligned, so b = 256
        // L is 1<<23, so bL = (1<<23)*256 = 1<<31 < uint.MaxValue
        public const uint _L = 1u << 23;
        public const int _logB = 8; // b = 256, so we emit a byte when normalizing
        public const int _bMask = 255; // mask to get the last logB bits

        public StaticRANSEncoder(IRANSModel<TSymbol> model, IConverter<TSymbol> converter)
        {
            this.Model = model;
            this.Converter = converter;
        }

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<ReadOnlyMemory<TSymbol>> input)
        {
            await foreach (var buffer in input)
            {
                // Initialize the model
                var header = InitializeModel(buffer.Span);
                // Print header (static order0 distribution)
                foreach (var b in header) yield return b;
                var stream = new byte[buffer.Length];
                int pos = stream.Length;

                uint state = _L;
                for (int i = buffer.Length - 1; i >= 0 ; i--)
                {
                    var symbol = buffer.Span[i];
                    int freq = this.Model.GetFrequency(symbol);
                    int cdf = this.Model.GetCumulativeFrequency(symbol);
                    int n = this.Model.LogDenominator;
                    // Renormalize state by emitting a byte
                    var state_max = ((_L << _logB) >> n) * freq;
                    while(state >= state_max)
                    {
                        stream[--pos] = (byte) (state & _bMask);
                        state >>= _logB;
                    }
                    state = (uint) (((state / freq) << n) + state % freq + cdf);
                }
                // Output the state at the end. There are optimizations for using log(state) bits, but for now 32 bits is ok
                var bytes = BitConverter.GetBytes(state);
                foreach (var b in bytes)
                    yield return b;
                for (int i = pos; i < stream.Length; i++)
                    yield return stream[i];
            }
        }

        public byte[] InitializeModel(ReadOnlySpan<TSymbol> buffer)
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
            var list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(dict.Count));
            foreach (var kv in dict)
            {
                list.AddRange(this.Converter.Convert(kv.Key));
                list.AddRange(BitConverter.GetBytes(kv.Value));
            }
            return list.ToArray();
        }
    }
}