using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Ranking
{
    public class NaiveRanking : IBWDRankProvider
    {
        public int BPC { get; }
        private RankedWord BestWord { get; set; } = RankedWord.Empty;
        private readonly RankedWord InitialWord = RankedWord.Empty;
        private Dictionary<int, Dictionary<int, double>> LearnedRanks { get; set; }
        public BWDIndex BWDIndex { get; private set; }

        public NaiveRanking(int maxWordSize, int bpc = 8)
        {
            this.BPC = bpc;
            this.BestWord = InitialWord;
            this.LearnedRanks = new Dictionary<int, Dictionary<int, double>>();
            for (int i = 1; i <= maxWordSize; i++)
                this.LearnedRanks.Add(i, new Dictionary<int, double>());
        }

        public void Initialize(BWDIndex BWDIndex) => this.BWDIndex = BWDIndex;

        public void Rank(Match match)
        {
            var count = this.BWDIndex.Count(match);
            if (count < 2 || match.Length < 2) return;
            if (!this.LearnedRanks[match.Length].TryGetValue(count, out var rank))
            {
                var calcRank = (match.Length - 1) * (count - 1);
                this.LearnedRanks[match.Length].Add(count, calcRank);
                rank = calcRank;
            }

            if (rank > this.BestWord.Rank)
                this.BestWord = new RankedWord(new Word(this.BWDIndex.SA[match.Index], match.Length), rank, count);
        }

        public List<RankedWord> GetTopRankedWords()
        {
            var word = this.BestWord;
            this.BestWord = InitialWord;
            return new List<RankedWord>() { word };
        }
    }
}