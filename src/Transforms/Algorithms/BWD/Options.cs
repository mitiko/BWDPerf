namespace BWDPerf.Transforms.Algorithms.BWD
{
    public struct Options
    {
        public int MaxWordSize { get; }
        public int BPC { get; }

        public Options(int maxWordSize = 16, int bpc = 8)
        {
            this.MaxWordSize = maxWordSize;
            this.BPC = bpc;
        }
    }
}