using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Ranking
{
    public class EntropyRanking : IBWDRankProvider
    {
        private RankedWord BestWord { get; set; } = RankedWord.Empty;
        private OccurenceDictionary<ushort> Model { get; set; } = new();
        public BWDIndex BWDIndex { get; private set; }

        private int n = 0; // Symbol count
        public ushort wordIndex = 256; // Next word index

        public void Initialize(BWDIndex BWDIndex)
        {
            // TODO: Start with a bias for initial dictionary overhead
            this.BWDIndex = BWDIndex;
            this.n = BWDIndex.Length;
            for (int i = 0; i < n; i++)
                this.Model.Add(this.BWDIndex[i]);

            Console.WriteLine($"-> Inititial entropy: {GetEntropy()}");
            this.BestWord = RankedWord.Empty;
        }

        public void Rank(Match match)
        {
            var len = match.Length;
            var count = match.Count;
            var loc = match.Index;
            // Must locate match at at least 2 locations to get gains
            if (len < 2 || count < 2) return;

            var wordDictionary = new OccurenceDictionary<ushort>(len);
            for (int s = 0; s < len; s++)
                wordDictionary.Add(this.BWDIndex[loc+s]);

            int n1 = n - count * (len - 1);
            double rank = 0;
            foreach (var character in wordDictionary)
            {
                int cx = this.Model[character.Key];
                int cxw = cx - character.Value * count;
                rank += cxw * Math.Log2(cxw) - cx * Math.Log2(cx);
            }
            rank -= 8 * (len + 1); // Dictionary overhead
            rank += count * Math.Log2(count);
            rank -= n1 * Math.Log2(n1);
            // This is the same across all words at the current state, we can ignore it
            // rank += n * Math.Log2(n);

            if (rank > this.BestWord.Rank)
                this.BestWord = new RankedWord(new Word(loc, len), rank, count);
        }

        public List<RankedWord> GetTopRankedWords()
        {
            var word = this.BestWord;
            word.Rank += n * Math.Log2(n);
            if (word.Rank <= 0)
            {
                Console.WriteLine($"Final entropy estimate: {GetEntropy()}");
                return new List<RankedWord>() { RankedWord.Empty };
            }
            this.UpdateModel();
            this.BestWord = RankedWord.Empty;
            return new List<RankedWord>() { word };
        }

        private void UpdateModel()
        {
            var loc = this.BestWord.Word.Location;
            var len = this.BestWord.Word.Length;
            var c = this.BestWord.Count;
            this.n -= c * (len - 1);
            this.Model.Add(this.wordIndex++, c);
            // TODO: Check if anythings goes negative
            for (int s = 0; s < len; s++)
                this.Model.SubstractMany(this.BWDIndex[loc+s], c);
        }

        private double GetEntropy()
        {
            double entropy = 0;
            double sum = this.Model.Sum();
            foreach (var x in this.Model.Values)
            {
                double px = x / sum;
                entropy -= px * Math.Log2(px);
            }
            return entropy;
        }
    }
}