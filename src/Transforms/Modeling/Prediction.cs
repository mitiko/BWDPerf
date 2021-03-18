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

        // TODO: Add mixing methods
    }
}