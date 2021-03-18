using System;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Quantizers
{
    public class BasicQuantizer : IQuantizer
    {
        public int Accuracy => 23;
        public IModel Model { get; }

        public BasicQuantizer(IModel model) =>
            this.Model = model;

        public (int cdf, int f) GetPrediction(int symbolIndex)
        {
            var prediction = this.Model.Predict();
            int cdf = 0, f = 0, n = 1 << this.Accuracy;
            for (int i = 0; i < symbolIndex; i++)
                cdf += 1 + (int) (prediction[i] * (n - prediction.Symbols));
            f = 1 + (int) (prediction[symbolIndex] * (n - prediction.Symbols));
            return (cdf, f);
        }

        public int GetSymbolIndex(int cdf)
        {
            var prediction = this.Model.Predict();
            int n = 1 << this.Accuracy;
            int CDF = 0;
            for (int i = 0; i < prediction.Symbols; i++)
            {
                CDF += 1 + (int) (prediction[i] * (n - prediction.Symbols));
                if (cdf < CDF) return i;
            }
            throw new Exception("CDF out of range");
        }

        public void Update(int symbolIndex) =>
            this.Model.Update(symbolIndex);
    }
}