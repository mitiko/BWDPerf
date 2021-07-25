using System;

namespace BWDPerf.Transforms.Modeling
{
    // Represents a probability distribution for a given alphabet
    // p[i] is the probability that the symbol alphabet[i] will occur
    public class Prediction
    {
        private double[] Probabilities { get; }
        public double this[int index]
        {
            get => this.Probabilities[index];
            set => this.Probabilities[index] = value;
        }
        public int Length => this.Probabilities.Length;
        public bool IsNormalized { get; private set; } = false;

        public Prediction(int symbolCount) => this.Probabilities = new double[symbolCount];

        public void Normalize()
        {
            if (this.IsNormalized == true) return;
            this.IsNormalized = true;
            double sum = this.Sum();
            if (sum == 0) { this.Probabilities.AsSpan().Fill(1d / this.Length); return; }
            for (int i = 0; i < this.Length; i++)
                this.Probabilities[i] /= sum;
        }

        public void Print()
        {
            for (int i = 0; i < this.Length; i++)
            {
                Console.Write($"{this[i]}, ");
            }
            Console.WriteLine($". len = {this.Length}");
        }

        public double Sum()
        {
            double sum = 0;
            for (int i = 0; i < this.Length; i++)
                sum += this.Probabilities[i];
            return sum;
        }

        public static Prediction Uniform(int length)
        {
            var p = new Prediction(length);
            p.Probabilities.AsSpan().Fill(1d / length);
            p.IsNormalized = true;
            Console.WriteLine("[Predictions toolset] Returning a uniform distribution");
            return p;
        }

        public static Prediction operator* (double weight, Prediction p)
        {
            var pW = new Prediction(p.Length);
            for (int i = 0; i < p.Length; i++)
                pW[i] = p[i] * weight;
            return pW;
        }

        public static Prediction operator+ (Prediction p1, Prediction p2)
        {
            if (p1.Length != p2.Length) throw new Exception("Can't mix predictions of different sizes");
            var p = new Prediction(p1.Length);
            for (int i = 0; i < p1.Length; i++)
                p[i] = p1[i] + p2[i];
            return p;
        }

        public static Prediction operator- (Prediction p1, Prediction p2)
        {
            if (p1.Length != p2.Length) throw new Exception("Can't mix predictions of different sizes");
            var p = new Prediction(p1.Length);
            for (int i = 0; i < p1.Length; i++)
                p[i] = p1[i] - p2[i];
            return p;
        }
    }
}