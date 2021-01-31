using System;
using System.Collections.Generic;
using System.Linq;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Models.RANS
{
    public class StaticOrder0<TSymbol> : IRANSModel<TSymbol>
        where TSymbol : struct
    {
        public Dictionary<TSymbol, int> FreqTable { get; set; } = new();

        public void Initialize(ref Dictionary<TSymbol, int> initial)
        {
            var den = initial.Values.Sum();
            Console.WriteLine($"Denominator: {den}");
            // Quantize values
            foreach (var kv in initial)
            {
                // p_s = f_s / denominator
                // q_s = p_s * M
                // q_s >= 1
                double M = 1 << LogDenominator;
                // Make sure q is always at least 1
                int q = 1 + (int) ((double) initial[kv.Key] / den * (M - initial.Count));
                this.FreqTable.Add(kv.Key, q);

                Console.WriteLine($"Adding to dict: {kv.Key} -- {q}");
            }
        }

        public void AddSymbol(TSymbol s) => throw new NotImplementedException("Can't add symbol for a static model");

        public int GetFrequency(TSymbol s) => this.FreqTable[s];

        public int GetDenominator() => this.FreqTable.Values.Sum();
        public int LogDenominator => 23; // same as _L

        public int GetCumulativeFrequency(TSymbol s)
        {
            int cum = 0;
            var enumerator = this.FreqTable.GetEnumerator();
            for (int i = 0; i < this.FreqTable.Count; i++)
            {
                enumerator.MoveNext();
                if (enumerator.Current.Key.Equals(s))
                    break;
                cum += enumerator.Current.Value;
            }
            return cum;
        }

        public TSymbol GetSymbol(int y)
        {
            int cum = 0;
            var enumerator = this.FreqTable.GetEnumerator();
            for (int i = 0; i < this.FreqTable.Count; i++)
            {
                enumerator.MoveNext();
                cum += enumerator.Current.Value;
                if (y < cum)
                    return this.FreqTable.ElementAt(i).Key;
            }
            // Assume the decoder keeps track of state being normalized nad output the last symbol;
            return enumerator.Current.Key;
        }
    }
}