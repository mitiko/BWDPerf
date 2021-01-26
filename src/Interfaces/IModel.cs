namespace BWDPerf.Interfaces
{
    public interface IModel<TSymbol>
    {
        public double GetProbability(TSymbol s);

        // These two methods represent a probability p_s
        public int GetFrequency(TSymbol s);
        public int GetDenominator();

        public int GetCumulativeFrequency(TSymbol s);

        public void AddSymbol(TSymbol s);
    }
}