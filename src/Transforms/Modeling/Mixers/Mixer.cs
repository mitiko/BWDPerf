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
        private double LearningRate { get; set; }
        private readonly double _epsilon = 0.00001;
        private readonly int N; // symbol count
        private readonly int M; // model count

        public Mixer(double lr = 0.1, int n = 256, params IModel[] models)
        {
            this.Models = models;
            this.Predictions = new Prediction[models.Length];
            this.Weights = new double[models.Length];
            this.ActivatedWeights = new double[models.Length];
            this.LearningRate = lr;
            this.N = n;
            this.M = models.Length;
        }

        public Prediction Predict()
        {
            this.Prediction = new Prediction(N);
            for (int i = 0; i < M; i++)
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
            var P = new double[M];
            for (int i = 0; i < M; i++)
            {
                this.Models[i].Update(symbolIndex);
                P[i] = this.Predictions[i][symbolIndex];
                s += this.ActivatedWeights[i];
            }

            var DLoss = -1 / (p * Math.Log(2) + _epsilon);
            for (int i = 0; i < M; i++)
            {
                var DpDP_i = (P[i] - p) / s;
                var DP_iDw = this.ActivatedWeights[i] * (1 - this.ActivatedWeights[i]);
                var dw = DLoss * DpDP_i * DP_iDw * this.LearningRate;
                if (double.IsNaN(dw)) throw new Exception($"dw[{i}] was NaN");
                if (double.IsNaN(this.ActivatedWeights[i])) throw new Exception($"sig(w)[{i}] was NaN");
                this.Weights[i] -= dw;
            }
        }

        private static double Sigmoid(double x) => 1 / (1 + Math.Exp(-x));
    }
}