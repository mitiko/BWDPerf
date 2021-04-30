using System;
using System.Diagnostics;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Mixers
{
    public class SimpleMixer : IMixer
    {

        public IModel[] Models { get; }
        private double Weight { get; set; }
        private double LearningRate { get; } = 0.1;
        private Prediction Prediction { get; set; }
        private Prediction Prediction1 { get; set; }
        private Prediction Prediction2 { get; set; }
        private double _epsilon = 0.000001;

        public SimpleMixer(IModel model1, IModel model2)
        {
            this.Models = new IModel[] { model1, model2 };
            // This goes through sigmoid, so the actual initial used weight will be 0.5
            this.Weight = 0;
        }

        public Prediction Predict()
        {
            this.Prediction1 = this.Models[0].Predict();
            this.Prediction2 = this.Models[1].Predict();
            var w = Sigmoid(this.Weight);
            var p = this.Prediction1 * w + this.Prediction2;
            p.Normalize();
            this.Prediction = p;
            return p;
        }

        public void Update(int symbolIndex)
        {
            this.Models[0].Update(symbolIndex);
            this.Models[1].Update(symbolIndex);

            var p_sum = this.Prediction1.Sum();
            var q_sum = this.Prediction2.Sum();
            var p_i = this.Prediction1[symbolIndex];
            var q_i = this.Prediction2[symbolIndex];
            var z = this.Prediction[symbolIndex];
            var phi = Sigmoid(this.Weight);

            // var Dloss = 1 / (z * Math.Log(2) + _epsilon);
            // var loss = Math.Log2(z + _epsilon) * Math.Log2(z + _epsilon);
            var Dloss = Math.Log2(z + _epsilon) / (z * Math.Log(2) + _epsilon);
            var g = phi * p_sum + q_sum;
            var DzDphi = (p_i * q_sum - p_sum * q_i) / (g * g + _epsilon);
            var DphiDw = phi * (1 - phi);
            var dw = Dloss * DzDphi * DphiDw * this.LearningRate;
            if (double.IsNaN(phi)) throw new Exception("Phi was NaN");
            if (double.IsNaN(dw)) throw new Exception("Dw was NaN");
            this.Weight = this.Weight - dw;

            if (++C % 1000 == 0)
                Console.WriteLine($"{C} Prediction: {z}; Phi: {phi}; dPhi: {dw / DphiDw}");
            // Console.WriteLine($"p1 sum: {p_sum}, p2 sum: {q_sum}");
            // Console.WriteLine($"p1 pred: {p_i}, p2 pred: {q_i}");
        }
        int C = 0;

        double Sigmoid(double x) => 1 / (1 + Math.Exp(-x));
    }
}