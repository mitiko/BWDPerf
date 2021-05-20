using System;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Mixers
{
    public class Mixer : IMixer
    {
        public IModel[] Models { get; set; }
        private Prediction[] Predictions { get; set; }
        private Prediction Prediction { get; set; }
        private double[] Weights { get; set; }
        private double[] ActivatedWeights { get; set; }
        // TODO: Write bias code
        private double[] Bias { get; set; }
        private double LearningRate { get; set; }
        private readonly double _epsilon = 0.00001;

        public Mixer(double lr = 0.1, int n = 256, params IModel[] models)
        {
            this.Models = models;
            this.Predictions = new Prediction[models.Length];
            this.Weights = new double[models.Length];
            this.ActivatedWeights = new double[models.Length];
            this.Bias = new double[n];
            this.LearningRate = lr;
        }

        public Prediction Predict()
        {
            this.Prediction = new Prediction(this.Bias.Length);
            for (int i = 0; i < Models.Length; i++)
            {
                this.Predictions[i] = this.Models[i].Predict(); // Predict
                this.Predictions[i].Normalize(); // Normalize
                this.ActivatedWeights[i] = Sigmoid(this.Weights[i]); // Activate
                this.Prediction += this.ActivatedWeights[i] * this.Predictions[i];
            }
            this.Prediction.Normalize();
            return this.Prediction;
        }

        public void Update(int symbolIndex)
        {
            var p = this.Prediction[symbolIndex];
            var s = 0.0d;
            var P = new double[this.Models.Length];
            for (int i = 0; i < Models.Length; i++)
            {
                this.Models[i].Update(symbolIndex);
                P[i] = this.Predictions[i][symbolIndex];
                s += this.ActivatedWeights[i];
            }

            var DLoss = -1 / (p * Math.Log(2) + _epsilon);
            for (int i = 0; i < Models.Length; i++)
            {
                var DpDP_i = (P[i] - p) / s;
                var DP_iDw = this.ActivatedWeights[i] * (1 - this.ActivatedWeights[i]);
                var dw = DLoss * DpDP_i * DP_iDw * this.LearningRate;
                if (double.IsNaN(dw)) throw new Exception($"dw[{i}] was NaN");
                if (double.IsNaN(this.ActivatedWeights[i])) throw new Exception($"sig(w)[{i}] was NaN");
                this.Weights[i] -= dw;
            }

            if (++C % 100_000 == 0)
            {
                // string _p = "";
                // for (int i = 0; i < Models.Length; i++)
                //     _p += $"{P[i]} ";
                // Console.WriteLine($"probs: {_p}");
                string _w = "";
                for (int i = 0; i < Models.Length; i++)
                    _w += $"{this.ActivatedWeights[i]/s} ";
                Console.WriteLine($"weights: {_w}");
            }
        }
        int C = 0;

        private double Sigmoid(double x) => 1 / (1 + Math.Exp(-x));
    }
}