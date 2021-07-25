namespace BWDPerf.Interfaces
{
    public interface IAlphabet<PredictionSymbol>
    {
        public PredictionSymbol this[int index] { get; }
        public int this[PredictionSymbol symbol] { get; }
        public int Length { get; }
    }
}