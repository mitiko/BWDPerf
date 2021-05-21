using BWDPerf.Transforms.Modeling;

namespace BWDPerf.Interfaces
{
    public interface IQuantizer
    {
        // For example a 12-bit quantizer has accuracy 12
        // And quantizes the probabilities to the closest n/2^12 for an integer n
        // The idea is to have the sum be 2^12
        public int Accuracy { get; }

        // Return the prediction of the model
        public Prediction Predict();

        // Get cumulative distribution frequency and frequency
        public (uint cdf, uint freq) Encode(int symbolIndex, Prediction prediction);

        // Get symbol based on cdf range it falls in
        public int Decode(uint cdf, Prediction prediction);

        // Update the underlying model by passing what the symbol was
        public void Update(int symbolIndex);
    }
}