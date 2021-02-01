namespace BWDPerf.Interfaces
{
    public interface IRANSModel<TSymbol> : IModel<TSymbol>
    {
        // For rANS M = 2^n, and we use n to do bitshifts to go faster, so we make the model return n
        // rANS models are also assumed to return frequencies quantized to p_s = f_s / 2^n
        // 31 is chosen as default bc it's the biggest we can shift 1 by and not overflow uint
        public int LogDenominator { get; }
    }
}