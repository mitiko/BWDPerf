using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Modeling;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.StaticRANS
{
    public class RANSDecoder<TSymbol> : IDecoder<byte, TSymbol>
    {
        public Alphabet<TSymbol> Alphabet { get; }
        public IQuantizer Model { get; }

        // See encoder for explanation of these constants
        public const uint _L = 1u << 23;
        public const int _logB = 8;

        public RANSDecoder(Alphabet<TSymbol> alphabet, IQuantizer model)
        {
            this.Alphabet = alphabet;
            this.Model = model;
        }

        public async IAsyncEnumerable<TSymbol> Decode(IAsyncEnumerable<byte> input)
        {
            var enumerator = input.GetAsyncEnumerator();

            var uint32Arr = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                await enumerator.MoveNextAsync();
                uint32Arr[i] = enumerator.Current;
            }

            uint state = BitConverter.ToUInt32(uint32Arr);
            int n = this.Model.Accuracy;
            var mask = (1 << n) - 1;
            while (true)
            {
                // Decode
                var symbol = this.Model.GetSymbolIndex((int) (state & mask));
                yield return this.Alphabet[symbol];
                var (cdf, freq) = this.Model.GetPrediction(symbol);
                this.Model.Update(symbol);

                state = (uint) (freq * (state >> n) + (state & mask) - cdf);
                // Renormalize
                if (state == _L)
                    break;
                bool end = false;
                while (state < _L)
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        // Stream ended
                        if (state == _L) end = true;
                        else throw new Exception("Incorrect decode. Checksum not matching");
                    }
                    else
                    {
                        var b = enumerator.Current;
                        state = (state << _logB) + b;
                    }
                }
                if (end) break;
            }
        }
    }
}