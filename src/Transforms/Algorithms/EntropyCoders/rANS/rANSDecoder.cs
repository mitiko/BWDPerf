using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.rANS
{
    public class rANSDecoder<TSymbol> : IDecoder<byte, TSymbol>
    {
        public IRANSModel<TSymbol> Model { get; }
        public IConverter<TSymbol> Converter { get; }

        // See encoder for explanation of these constants
        public const uint _L = 1u << 23;
        public const int _logB = 8;

        public rANSDecoder(IRANSModel<TSymbol> model, IConverter<TSymbol> converter)
        {
            this.Model = model;
            this.Converter = converter;
        }

        public async IAsyncEnumerable<TSymbol> Decode(IAsyncEnumerable<byte> input)
        {
            var enumerator = input.GetAsyncEnumerator();
            var intitial = await ReadHeader(enumerator);
            this.Model.Initialize(ref intitial);

            var uint32Arr = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                await enumerator.MoveNextAsync();
                uint32Arr[i] = enumerator.Current;
            }

            uint state = BitConverter.ToUInt32(uint32Arr);
            Console.WriteLine($"Initial state: {state}");
            var mask = (1 << this.Model.LogDenominator) - 1;

            while (true)
            {
                // Decode
                var symbol = this.Model.GetSymbol((int) (state & mask));
                yield return symbol;
                int freq = this.Model.GetFrequency(symbol);
                int n = this.Model.LogDenominator;
                int cdf = this.Model.GetCumulativeFrequency(symbol);

                state = (uint) (freq * (state >> n) + (state & mask) - cdf);
                // Renormalize
                if (state == _L)
                    break;
                bool end = false;
                while (state < _L)
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        // stream ended
                        if (state == _L) end = true;
                        else throw new Exception("Incorrect decode");
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

        private async Task<Dictionary<TSymbol, int>> ReadHeader(IAsyncEnumerator<byte> enumerator)
        {
            var dict = new Dictionary<TSymbol, int>();
            var size = await GetInt();
            Console.WriteLine($"Decoder got size: {size}");
            for (int i = 0; i < size; i++)
            {
                var arr = new byte[this.Converter.BytesPerSymbol];
                for (int j = 0; j < arr.Length; j++)
                {
                    await enumerator.MoveNextAsync();
                    arr[j] = enumerator.Current;
                }
                TSymbol key = this.Converter.Convert(arr);
                int value = await GetInt();
                dict.Add(key, value);
                Console.WriteLine($"Decomp dict: {key} -- {value}");
            }

            return dict;

            async Task<int> GetInt()
            {
                var int32Arr = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    await enumerator.MoveNextAsync();
                    int32Arr[i] = enumerator.Current;
                }
                return BitConverter.ToInt32(int32Arr);
            }
        }
    }
}