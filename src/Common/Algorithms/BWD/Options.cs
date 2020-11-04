namespace BWDPerf.Common.Algorithms.BWD
{
    public struct Options
    {
        public int MaxWordSize { get; set; }
        public int BPC { get; set; }
        public int IndexSize { get; set; }
        public bool AutoEnd { get; set; }

        public Options(int maxWordSize = 16, int indexSize = 7, int bpc = 8, bool autoEnd = false)
        {
            this.MaxWordSize = maxWordSize;
            this.BPC = bpc;
            this.IndexSize = indexSize;
            this.AutoEnd = autoEnd;
        }
    }
}