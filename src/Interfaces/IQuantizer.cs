using BWDPerf.Transforms.Modeling;

namespace BWDPerf.Interfaces
{
    // Converts probabilities to the closest x/2^n for an integer x
    // The idea is to have the denominator be a power of 2
    public interface IQuantizer
    {
        // The bit accuracy of a quantizer
        public int Accuracy { get; }

        // Quantize a prediction to a n-bit frequency and a cumulative distribution frequency
        public (uint cdf, uint freq) Encode(int symbolIndex, Prediction prediction);

        // Get symbol (as symbolIndex, cdf and freq) based on cdf range it falls in
        public (int symbolIndex, uint cdf, uint freq) Decode(uint cdfRange, Prediction prediction);
    }
}