using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class Order0 : IModel
    {
        public int[] Counts { get; }

        public Order0(int symbolCount)
        {
            this.Counts = new int[symbolCount];
            for (int i = 0; i < this.Counts.Length; i++)
                this.Counts[i] = 1;
        }

        public Prediction Predict()
        {
            var prediction = new Prediction(this.Counts.Length);
            double n = 0;
            for (int i = 0; i < this.Counts.Length; i++)
                n += this.Counts[i];
            for (int i = 0; i < this.Counts.Length; i++)
                prediction[i] = this.Counts[i] / n;
            return prediction;
        }

        public void Update(int symbolIndex) =>
            this.Counts[symbolIndex]++;
    }
}