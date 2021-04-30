using System;
using System.Diagnostics;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Quantizers
{
    public class BasicQuantizer : IQuantizer
    {
        public int Accuracy => 23;
        public IModel Model { get; }

        public BasicQuantizer(IModel model) => this.Model = model;

        public Prediction Predict()
        {
            // Console.WriteLine("[Quantizer] Pre-prediction");
            var p = this.Model.Predict();
            // Console.WriteLine("[Quantizer] Post-prediction");
            if (!p.IsNormalized)
            {
                Console.WriteLine("Quantizer is normalizing");
                p.Normalize();
            }
            return p;
        }

        public (uint cdf, uint freq) Encode(int symbolIndex, Prediction prediction)
        {
            uint cdf = 0, n = (uint) ((1 << this.Accuracy) - prediction.Length);
            uint freq = 1 + (uint) (prediction[symbolIndex] * n);
            for (int i = 0; i < symbolIndex; i++)
                cdf += 1 + (uint) (prediction[i] * n);
            return (cdf, freq);
        }

        public int Decode(uint cdf, Prediction prediction)
        {
            uint n = (uint) ((1 << this.Accuracy) - prediction.Length);
            uint CDF = 0;
            for (int i = 0; i < prediction.Length; i++)
            {
                CDF += 1 + (uint) (prediction[i] * n);
                if (cdf < CDF) return i;
            }
            throw new Exception("CDF out of range");
        }

        public void Update(int symbolIndex) => this.Model.Update(symbolIndex);
    }
}