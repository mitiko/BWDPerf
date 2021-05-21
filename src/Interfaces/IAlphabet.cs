namespace BWDPerf.Interfaces
{
    public interface IAlphabet<TSymbol>
    {
        public TSymbol this[int index] { get; }
        public int this[TSymbol symbol] { get; }
        public int Length { get; }
    }
}