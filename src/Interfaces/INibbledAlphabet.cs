namespace BWDPerf.Interfaces
{
    public interface INibbleAlphabet<TSymbol>
    {
        public TSymbol this[(int s1, int s2) index] { get; }
        public (int s1, int s2) this[TSymbol symbol] { get; }
        public int Length { get; }
    }
}