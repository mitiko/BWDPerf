namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public struct RankedWord
    {
        public Word Word { get; set; }
        public double Rank { get; set; }

        public RankedWord(Word word, double rank)
        {
            this.Word = word;
            this.Rank = rank;
        }
    }
}