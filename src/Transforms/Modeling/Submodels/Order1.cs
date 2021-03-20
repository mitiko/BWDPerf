using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class Order1 : IModel
    {
        public int[][] Counts { get; }
        public int State { get; private set; }

        public Order1(int symbolCount)
        {
            this.Counts = new int[symbolCount][];
            for (int i = 0; i < this.Counts.Length; i++)
                this.Counts[i] = new int[symbolCount];
            for (int i = 0; i < this.Counts.Length; i++)
                for (int j = 0; j < this.Counts[i].Length; j++)
                    this.Counts[i][j] = 1;
        }

        public Prediction Predict()
        {
            var prediction = new Prediction(this.Counts.Length);
            double n = 0;
            for (int i = 0; i < this.Counts.Length; i++)
                n += this.Counts[this.State][i];
            for (int i = 0; i < this.Counts.Length; i++)
                prediction[i] = this.Counts[this.State][i] / n;
            return prediction;
        }

        public void Update(int symbolIndex)
        {
            this.Counts[this.State][symbolIndex]++;
            this.State = symbolIndex;
        }
    }
}