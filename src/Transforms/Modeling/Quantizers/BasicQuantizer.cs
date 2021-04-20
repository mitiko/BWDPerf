using System;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Quantizers
{
    public class BasicQuantizer : IQuantizer
    {
        public int Accuracy => 23;
        public IModel Model { get; }

        public BasicQuantizer(IModel model) => this.Model = model;

        public Prediction Predict() => this.Model.Predict();

        public (int cdf, int freq) Encode(int symbolIndex, Prediction prediction)
        {
            int cdf = 0, n = (1 << this.Accuracy) - prediction.Length;
            int freq = 1 + (int) (prediction[symbolIndex] * n);
            for (int i = 0; i < symbolIndex; i++)
                cdf += 1 + (int) (prediction[i] * n);
            return (cdf, freq);
        }

        public int Decode(int cdf, Prediction prediction)
        {
            int n = (1 << this.Accuracy) - prediction.Length;
            int CDF = 0;
            for (int i = 0; i < prediction.Length; i++)
            {
                CDF += 1 + (int) (prediction[i] * n);
                if (cdf < CDF) return i;
            }
            throw new Exception("CDF out of range");
        }

        public void Update(int symbolIndex) => this.Model.Update(symbolIndex);
    }
}