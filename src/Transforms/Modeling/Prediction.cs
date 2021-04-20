using System;

namespace BWDPerf.Transforms.Modeling
{
    public class Prediction
    {
        // Represents a probability distribution for a given alphabet
        // p[i] is the probability that the symbol alphabet[i] will occur
        // private double[] Probabilities { get; }
        private double[] Probabilities { get; }
        public double this[int index]
        {
            get => this.Probabilities[index];
            set => this.Probabilities[index] = value;
        }
        public int Length => this.Probabilities.Length;

        public Prediction(int symbolCount) => this.Probabilities = new double[symbolCount];

        public void Normalize()
        {
            double sum = this.Sum();
            for (int i = 0; i < this.Length; i++)
                this.Probabilities[i] /= sum;
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
            return p;
        }

        public static Prediction FromSymbol(int symbolIndex, int symbolCount)
        {
            var prediction = new Prediction(symbolCount);
            prediction[symbolIndex] = 1;
            return prediction;
        }

        public static Prediction operator* (Prediction p, double weight)
        {
            for (int i = 0; i < p.Length; i++)
                p[i] *= weight;
            return p;
        }

        public static Prediction operator+ (Prediction p1, Prediction p2)
        {
            if (p1.Length != p2.Length) throw new Exception("Can't mix predictions of different sizes");
            for (int i = 0; i < p1.Length; i++)
                p1[i] += p2[i];
            return p1;
        }

        public static Prediction operator- (Prediction p1, Prediction p2)
        {
            if (p1.Length != p2.Length) throw new Exception("Can't mix predictions of different sizes");
            for (int i = 0; i < p1.Length; i++)
                p1[i] -= p2[i];
            return p1;
        }
    }
}