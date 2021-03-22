namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public struct RankedWord
    {
        public Word Word { get; set; }
        public double Rank { get; set; }
        public static RankedWord Empty => new RankedWord(Word.Empty, double.MinValue);

        public RankedWord(Word word, double rank)
        {
            this.Word = word;
            this.Rank = rank;
        }
    }
}