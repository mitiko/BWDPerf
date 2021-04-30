using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class Order0 : IModel
    {
        private int[] Counts { get; }

        public Order0(int symbolCount) => this.Counts = new int[symbolCount];

        public Prediction Predict()
        {
            var prediction = new Prediction(this.Counts.Length);
            for (int i = 0; i < this.Counts.Length; i++)
                prediction[i] = this.Counts[i];
            return prediction;
        }

        public void Update(int symbolIndex) =>
            this.Counts[symbolIndex]++;
    }
}