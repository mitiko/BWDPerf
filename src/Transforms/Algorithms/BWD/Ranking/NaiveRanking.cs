using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Ranking
{
    public class NaiveRanking : IBWDRanking
    {
        public int BPC { get; }
        public int IndexSize { get; }
        private RankedWord BestWord { get; set; }
        private readonly RankedWord InitialWord = new RankedWord(new Word(-1, -1), double.MinValue);

        public NaiveRanking(int bpc, int indexSize)
        {
            this.BPC = bpc;
            this.IndexSize = indexSize;
            this.BestWord = InitialWord;
        }

        public void Rank(Word word, int count)
        {
            var rank = (word.Length * this.BPC - this.IndexSize) * (count - 1);
            if (rank > this.BestWord.Rank)
                this.BestWord = new RankedWord(word, rank);
        }

        public List<RankedWord> GetTopRankedWords()
        {
            var word = this.BestWord;
            this.BestWord = InitialWord;
            return new List<RankedWord>() { word };
        }
    }
}