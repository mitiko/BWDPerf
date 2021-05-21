namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public struct RankedWord
    {
        public Word Word { get; set; }
        public double Rank { get; set; }
        public int Count { get; set; }
        public static RankedWord Empty => new RankedWord(Word.Empty, double.MinValue, 0);

        public RankedWord(Word word, double rank, int count)
        {
            this.Word = word;
            this.Rank = rank;
            this.Count = count;
        }
    }
}