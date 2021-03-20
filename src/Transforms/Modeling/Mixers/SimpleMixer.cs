using System;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Mixers
{
    public class SimpleMixer : IMixer
    {
        public IModel[] Models { get; }
        private double Weight { get; set; }
        private double LearningRate { get; } = 0.08;
        private Prediction Prediction { get; set; }
        private Prediction Prediction1 { get; set; }
        private Prediction Prediction2 { get; set; }

        public int TotalPredictions { get; set; } = 0;

        public SimpleMixer(IModel model1, IModel model2)
        {
            this.Models = new IModel[] { model1, model2 };
            // This goes through sigmoid, so the actual initial used weight will be 0.5
            this.Weight = 0;
        }

        public Prediction Predict()
        {
            var prediction1 = this.Models[0].Predict();
            var prediction2 = this.Models[1].Predict();
            var w = Sigmoid(this.Weight);
            var p = prediction1 * w + prediction2 * (1 - w);
            p.Normalize();
            this.Prediction = p;
            this.Prediction1 = prediction1;
            this.Prediction2 = prediction2;
            return p;
        }

        public void Update(int symbolIndex)
        {
            this.Models[0].Update(symbolIndex);
            this.Models[1].Update(symbolIndex);
            // This is assuming that ground truth is the current symbol e.g. (0, 0, 1, 0)
            // KL divergence converges slower here for some reason.
            // I'm using log2(1 - p) as the cost. This is its derivative
            var dc = this.Prediction[symbolIndex] / ((1 - this.Prediction[symbolIndex]) * Math.Log(2));
            var ds = Sigmoid(this.Weight);
            ds = ds * (1-ds);
            var df = this.Prediction1[symbolIndex] - this.Prediction2[symbolIndex];
            // Calculate the derivatives and update the weight
            var dw = ds * df * dc * this.LearningRate;
            this.Weight = this.Weight - dw;
        }

        double Sigmoid(double x) => 1 / (1 + Math.Exp(-x));
    }
}