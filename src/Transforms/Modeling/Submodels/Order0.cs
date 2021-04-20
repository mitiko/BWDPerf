using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class Order0 : IModel
    {
        private int[] Counts { get; }
        private int Sum { get; set; }

        public Order0(int symbolCount) => this.Counts = new int[symbolCount];

        public Prediction Predict()
        {
            if (this.Sum == 0) return Prediction.Uniform(this.Counts.Length);
            // System.Console.WriteLine($"a count: {this.Counts[(byte) 'a']}; Sum: {this.Sum}");
            var prediction = new Prediction(this.Counts.Length);
            double sum = this.Sum;
            for (int i = 0; i < this.Counts.Length; i++)
                prediction[i] = this.Counts[i] / sum;
            return prediction;
        }

        public void Update(int symbolIndex)
        {
            // System.Console.WriteLine($"Updating order0 with '{(char) symbolIndex}'");
            this.Counts[symbolIndex]++;
            this.Sum++;
        }
    }
}