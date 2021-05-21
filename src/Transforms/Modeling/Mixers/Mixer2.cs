using System;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Mixers
{
    // Returns a normalized mix of two models
    public class Mixer2 : IMixer
    {
        public IModel[] Models { get; }
        private double Weight { get; set; }
        private double LearningRate { get; } = 0.1;
        private Prediction Prediction { get; set; }
        private Prediction Prediction1 { get; set; }
        private Prediction Prediction2 { get; set; }
        private double _epsilon = 0.000001; // The double.Epsilon is too small and becomes infinity when dividing

        public Mixer2(IModel model1, IModel model2)
        {
            this.Models = new IModel[] { model1, model2 };
            this.Weight = 0; // This goes through sigmoid, so the actual initial used weight will be 0.5
        }

        public Prediction Predict()
        {
            this.Prediction1 = this.Models[0].Predict();
            this.Prediction2 = this.Models[1].Predict();
            this.Prediction1.Normalize();
            this.Prediction2.Normalize();
            var w = Sigmoid(this.Weight);
            // Interpolate between model1 and model2 as 0 <= w <= 1
            this.Prediction = w * this.Prediction1 + (1 - w) * this.Prediction2;
            return this.Prediction;
        }

        public void Update(int symbolIndex)
        {
            this.Models[0].Update(symbolIndex);
            this.Models[1].Update(symbolIndex);

            var p_i = this.Prediction1[symbolIndex];
            var q_i = this.Prediction2[symbolIndex];
            var z = this.Prediction[symbolIndex];
            var phi = Sigmoid(this.Weight);

            // z = phi * (p_i - q_i) + q_i
            // L = -log2(z)
            var DLoss = -1 / (z * Math.Log(2) + _epsilon);
            var DzDphi = p_i - q_i;
            var DphiDw = phi * (1 - phi);
            var dw = DLoss * DzDphi * DphiDw * this.LearningRate;
            if (double.IsNaN(dw)) throw new Exception("Dw was NaN");
            this.Weight = this.Weight - dw;
        }

        double Sigmoid(double x) => 1 / (1 + Math.Exp(-x));
    }
}