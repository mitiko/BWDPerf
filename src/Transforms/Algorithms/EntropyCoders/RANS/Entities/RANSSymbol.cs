namespace BWDPerf.Transforms.Algorithms.EntropyCoders.RANS.Entities
{
    public readonly struct RANSSymbol
    {
        // Rans symbols have a frequency and cumulative distribution frequency (both quantized to n bits)
        // Rans symbols are ultimately formatted predictions ready to be encoded into the rans state
        public readonly uint Cdf { get; }
        public readonly uint Freq { get; }

        public RANSSymbol(uint cdf, uint freq)
        {
            this.Cdf = cdf;
            this.Freq = freq;
        }

        public static implicit operator RANSSymbol((uint cdf, uint freq) tuple)
            => new RANSSymbol(tuple.cdf, tuple.freq);
    }
}