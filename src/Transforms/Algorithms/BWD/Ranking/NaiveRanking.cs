using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Ranking
{
    public class NaiveRanking : IBWDRanking
    {
        public int BPC { get; }
        private RankedWord BestWord { get; set; }
        private readonly RankedWord InitialWord = new RankedWord(new Word(-1, -1), double.MinValue);
        private Dictionary<int, Dictionary<int, double>> LearnedRanks { get; set; }

        public NaiveRanking(Options options)
        {
            this.BPC = options.BPC;
            this.BestWord = InitialWord;
            this.LearnedRanks = new Dictionary<int, Dictionary<int, double>>();
            for (int i = 1; i <= options.MaxWordSize; i++)
                this.LearnedRanks.Add(i, new Dictionary<int, double>());
        }

        public void Rank(Word word, int count)
        {
            if (!this.LearnedRanks[word.Length].TryGetValue(count, out var rank))
            {
                var calcRank = (word.Length - 1) * (count - 1);
                this.LearnedRanks[word.Length].Add(count, calcRank);
                rank = calcRank;
            }

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