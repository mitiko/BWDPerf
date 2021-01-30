namespace BWDPerf.Interfaces
{
    public interface IConverter<TSymbol>
    {
        public int BytesPerSymbol { get; set; }
        public byte[] Convert(TSymbol symbol);
        public TSymbol Convert(byte[] bytes);
    }
}