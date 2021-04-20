using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class Order1 : IModel
    {
        private int[][] Counts { get; }
        private int[] Sums { get; }
        private int State { get; set; }

        public Order1(int symbolCount)
        {
            this.Counts = new int[symbolCount][];
            for (int i = 0; i < this.Counts.Length; i++)
                this.Counts[i] = new int[symbolCount];
            this.Sums = new int[symbolCount];
        }

        public Prediction Predict()
        {
            double sum = this.Sums[this.State];
            if (sum == 0) return Prediction.Uniform(this.Counts.Length);
            var prediction = new Prediction(this.Counts.Length);
            for (int i = 0; i < this.Counts.Length; i++)
                prediction[i] = this.Counts[this.State][i] / sum;
            return prediction;
        }

        public void Update(int symbolIndex)
        {
            this.Counts[this.State][symbolIndex]++;
            this.Sums[this.State]++;
            this.State = symbolIndex;
        }
    }
}