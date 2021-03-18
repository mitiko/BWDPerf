using BWDPerf.Transforms.Modeling;

namespace BWDPerf.Interfaces
{
    public interface IModel
    {
        // Get an array of probabilities for each symbol in the alphabet
        public Prediction Predict();

        // Update the model, knowing a specific symbol occured
        public void Update(int symbolIndex);
    }
}