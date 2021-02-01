using System.Collections.Generic;

namespace BWDPerf.Interfaces
{
    public interface IModel<TSymbol>
    {
        public void Initialize(ref Dictionary<TSymbol, int> initial);
        // These two methods represent a probability p_s for symbol s:
        // p_s = f_s / M, where M is the denominator
        public int GetFrequency(TSymbol s);
        public int GetDenominator();

        public int GetCumulativeFrequency(TSymbol s);
        public TSymbol GetSymbol(int y);

        public void AddSymbol(TSymbol s);
    }
}