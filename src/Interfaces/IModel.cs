namespace BWDPerf.Interfaces
{
    public interface IModel<TSymbol>
    {
        // These two methods represent a probability p_s for symbol s:
        // p_s = f_s / M, where M is the denominator
        public int GetFrequency(TSymbol s);
        public int GetDenominator();

        public int GetCumulativeFrequency(TSymbol s);

        public void AddSymbol(TSymbol s);
    }
}