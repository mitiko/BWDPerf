using System;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Quantizers
{
    public class BasicQuantizer : IQuantizer
    {
        public int Accuracy => 23;

        public (uint cdf, uint freq) Encode(int symbolIndex, Prediction prediction)
        {
            uint cdf = 0, n = (uint) ((1 << this.Accuracy) - prediction.Length);
            uint freq = 1 + (uint) (prediction[symbolIndex] * n);
            for (int i = 0; i < symbolIndex; i++)
                cdf += 1 + (uint) (prediction[i] * n);
            return (cdf, freq);
        }

        public (int symbolIndex, uint cdf, uint freq) Decode(uint cdfRange, Prediction prediction)
        {
            uint n = (uint) ((1 << this.Accuracy) - prediction.Length);
            uint CDF = 0;
            for (int i = 0; i < prediction.Length; i++)
            {
                var freq = 1 + (uint) (prediction[i] * n);
                CDF += freq;
                if (cdfRange < CDF) return (i, CDF - freq, freq);
            }
            throw new Exception($"CDF out of range...");
        }
    }
}