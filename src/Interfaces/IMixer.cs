namespace BWDPerf.Interfaces
{
    public interface IMixer : IModel
    {
        public IModel[] Models { get; }
    }
}