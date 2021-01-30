using System.Collections.Generic;
using System.Linq;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Models.RANS
{
    public class Order0<TSymbol> : IRANSModel<TSymbol>
        where TSymbol : struct
    {
        public Dictionary<TSymbol, int> FreqTable { get; set; }

        public void Initialize(ref Dictionary<TSymbol, int> initial) => this.FreqTable = initial;

        public void AddSymbol(TSymbol s) => this.FreqTable[s]++;
        public int GetFrequency(TSymbol s)
        {
            // p_s = f_s / denominator
            // q_s = p_s * M
            // q_s >= 1
            double M = 1 << LogDenominator;
            // Make sure q is always at least 1
            int q = 1 + (int) ((double) this.FreqTable[s] / this.GetDenominator() * (M - this.FreqTable.Count));
            return q;
        }

        public int GetDenominator() => this.FreqTable.Values.Sum();
        public int LogDenominator => 31;

        public int GetCumulativeFrequency(TSymbol s) =>
            this.FreqTable.TakeWhile(kv => kv.Key.Equals(s)).Sum(kv => kv.Value);
    }
}