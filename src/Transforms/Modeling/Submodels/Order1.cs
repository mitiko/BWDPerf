using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class Order1 : IModel
    {
        private int[][] Counts { get; }
        private int State { get; set; }

        public Order1(int symbolCount)
        {
            this.Counts = new int[symbolCount][];
            for (int i = 0; i < this.Counts.Length; i++)
                this.Counts[i] = new int[symbolCount];
        }

        public Prediction Predict()
        {
            var prediction = new Prediction(this.Counts.Length);
            for (int i = 0; i < this.Counts.Length; i++)
                prediction[i] = this.Counts[this.State][i];
            return prediction;
        }

        public void Update(int symbolIndex)
        {
            this.Counts[this.State][symbolIndex]++;
            this.State = symbolIndex;
        }
    }
}