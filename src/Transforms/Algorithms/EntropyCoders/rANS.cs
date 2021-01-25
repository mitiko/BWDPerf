using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders
{
    public class rANS : ICoder<byte[], byte[]>, IDecoder<byte, byte>
    {
        public async IAsyncEnumerable<byte[]> Encode(IAsyncEnumerable<byte[]> input)
        {
            await foreach (var buffer in input)
            {
                var freq = new OccurenceDictionary<Character>();
                var readLiteral = new Character(0, false);
                freq.Add(readLiteral);
                BigInteger x = 0;
                var list = new List<byte>();
                int count = 0;

                foreach (var symbol in buffer)
                {
                    var s = new Character(symbol);
                    var isNewSymbol = freq.Add(s);

                    if (isNewSymbol)
                    {
                        EncodeSymbol(readLiteral);
                        freq.Add(readLiteral);
                        list.Add(symbol);
                        count++;
                    }

                    EncodeSymbol(s);
                    count++;
                }

                void EncodeSymbol(Character s)
                {
                    int n = Convert.ToInt32(Math.Log2(freq.Sum()));
                    x = ((x / freq[s]) << n) + (x % freq[s]) + CDF(s, ref freq);
                    Console.WriteLine($"x = {x}");
                }

                var int32Arr = new byte[4];
                yield return BitConverter.GetBytes(count); // how many characters to read
                yield return BitConverter.GetBytes(list.Count);
                yield return BitConverter.GetBytes(x.GetByteCount());
                yield return list.ToArray();
                yield return x.ToByteArray();
            }
        }

        public async IAsyncEnumerable<byte> Decode(IAsyncEnumerable<byte> input)
        {
            var enumerator = input.GetAsyncEnumerator();
            var freq = new OccurenceDictionary<Character>();
            var readLiteral = new Character(0, false);
            freq.Add(readLiteral);
            var count = await ReadInt(enumerator);
            var listCount = await ReadInt(enumerator);
            var byteCount = await ReadInt(enumerator);

            var arr = new byte[listCount];
            for (int i = 0; i < arr.Length; i++)
            { await enumerator.MoveNextAsync(); arr[i] = enumerator.Current; }

            var xArr = new byte[byteCount];
            for (int i = 0; i < xArr.Length; i++)
            { await enumerator.MoveNextAsync(); xArr[i] = enumerator.Current; }

            var x = new BigInteger(xArr);

            while (x != 0)
            {
                int n = Convert.ToInt32(Math.Log2(freq.Sum()));
                int mask = (1 << n) - 1;
                var s = Symbol(x & mask, ref freq);
                x = freq[s] * (x >> n) + (x & mask) - CDF(s, ref freq);
                freq.Add(s);
                if (s.IsLiteral)
                    yield return s.Representation;
            }
        }

        public async Task<int> ReadInt(IAsyncEnumerator<byte> enumerator)
        {
            var int32Arr = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                await enumerator.MoveNextAsync();
                int32Arr[i] = enumerator.Current;
            }
            return BitConverter.ToInt32(int32Arr);
        }

        public int CDF(Character s, ref OccurenceDictionary<Character> freq)
        {
            var dict = freq.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            var cumFreq = 0;
            var enumerator = dict.GetEnumerator();
            enumerator.MoveNext();
            while (enumerator.Current.Key != s)
            {
                cumFreq += enumerator.Current.Value;
                enumerator.MoveNext();
            }
            return cumFreq;
        }

        public Character Symbol(BigInteger y, ref OccurenceDictionary<Character> freq)
        {
            var dict = freq.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            BigInteger cumFreq = 0;
            var enumerator = dict.GetEnumerator();
            enumerator.MoveNext();
            while (cumFreq < y)
            {
                cumFreq += enumerator.Current.Value;
                enumerator.MoveNext();
            }
            return enumerator.Current.Key;
        }
    }

    public struct Character
    {
        public Character(byte representation, bool isLiteral)
        {
            this.Representation = representation;
            this.IsLiteral = isLiteral;
        }

        public Character(byte representation)
        {
            this.Representation = representation;
            this.IsLiteral = true;
        }

        public byte Representation { get; set; }
        public bool IsLiteral { get; set; }

        public static bool operator==(Character a, Character b) =>
            a.Representation == b.Representation &&
            a.IsLiteral == b.IsLiteral;

        public static bool operator!=(Character a, Character b) => !(a == b);

        public override bool Equals(object obj)
        {
            return obj is Character character &&
                   Representation == character.Representation &&
                   IsLiteral == character.IsLiteral;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Representation, IsLiteral);
        }
    }
}