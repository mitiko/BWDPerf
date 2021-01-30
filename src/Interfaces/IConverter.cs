namespace BWDPerf.Interfaces
{
    public interface IConverter<TSymbol>
    {
        public byte[] Convert(TSymbol symbol);
    }
}