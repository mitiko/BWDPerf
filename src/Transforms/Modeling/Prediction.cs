using System;

namespace BWDPerf.Transforms.Modeling
{
    public struct Prediction
    {
        // Represents a probability distribution for a given alphabet
        // p[i] is the probability that the symbol alphabet[i] will occur
        private double[] Probabilities { get; }
        public double this[int index]
        {
            get => this.Probabilities[index];
            set => this.Probabilities[index] = value;
        }
        public int Symbols => this.Probabilities.Length;

        public Prediction(int symbolCount) =>
            this.Probabilities = new double[symbolCount];

        public void Normalize()
        {
            double total = 0;
            for (int i = 0; i < this.Symbols; i++)
                total += this.Probabilities[i];
            for (int i = 0; i < this.Symbols; i++)
                this.Probabilities[i] /= total;
        }

        public double Sum()
        {
            double sum = 0;
            for (int i = 0; i < this.Symbols; i++)
                sum += this.Probabilities[i];
            return sum;
        }

        public static Prediction FromSymbol(int symbolIndex, int symbolCount)
        {
            var prediction = new Prediction(symbolCount);
            prediction[symbolIndex] = 1;
            return prediction;
        }

        public static Prediction operator* (Prediction p, double weight)
        {
            for (int i = 0; i < p.Symbols; i++)
                p[i] *= weight;
            return p;
        }

        public static Prediction operator+ (Prediction p1, Prediction p2)
        {
            if (p1.Symbols != p2.Symbols) throw new Exception("Can't mix predictions of different sizes");
            for (int i = 0; i < p1.Symbols; i++)
                p1[i] += p2[i];
            return p1;
        }

        public static Prediction operator- (Prediction p1, Prediction p2)
        {
            if (p1.Symbols != p2.Symbols) throw new Exception("Can't mix predictions of different sizes");
            for (int i = 0; i < p1.Symbols; i++)
                p1[i] -= p2[i];
            return p1;
        }
    }
}