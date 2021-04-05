using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Ranking
{
    public class EntropyRanking : IBWDRanking
    {
        private RankedWord BestWord { get; set; }
        private readonly RankedWord InitialWord = RankedWord.Empty;
        private Statistics Model { get; set; } = new();
        private Statistics BestWordModel { get; set; } = new();
        public BWDIndex BWDIndex { get; private set; }

        // State
        private double E = 0; // encoded size in bits (H * symbol count)
        private double D = 4096; // initial dictionary size in bits (256 symbols with 2 bytes per symbol)
        private ushort WordIndex = 256; // word index in the dictionary
        // New state (after dictionary update)
        private double Ew = 0; // encoded size with the change in dictionary (in bits)
        private double d = 0; // dictionary update in bits

        public void Initialize(BWDIndex BWDIndex)
        {
            // TODO: Start with a bias for initial dictionary overhead
            this.BWDIndex = BWDIndex;
            for (int i = 0; i < this.BWDIndex.Length; i++)
                this.Model.Order0.Add(this.BWDIndex[i]);
            var entropy = this.Model.GetEntropy();
            Console.WriteLine($"-> Inititial entropy: {entropy}");
            this.BestWord = RankedWord.Empty;
            this.E = entropy * this.BWDIndex.Length;
        }

        public void Rank(Match match)
        {
            if (match.Length < 2) return; // Rank of single characters is 0
            int count = this.BWDIndex.GetParsedCount(match);
            var loc = this.BWDIndex.SA[match.Index];
            if (count < 2) return; // Must locate match at at least 2 locations to get gains

            // Copy the model
            var model = new Statistics(this.Model);
            // Update the model
            model.Order0.Add(this.WordIndex, count);
            for (int s = 0; s < match.Length; s++)
                model.Order0.SubstractMany(this.BWDIndex[loc+s], count);

            double dictOverhead = 8 * (match.Length + 1);
            double encodedSize = model.GetEntropy() * model.Order0.Sum();
            double rank = this.E - encodedSize - dictOverhead;

            if (rank > this.BestWord.Rank)
            {
                this.BestWord = new RankedWord(new Word(loc, match.Length), rank);
                this.BestWordModel = model;
                this.Ew = encodedSize;
                this.d = dictOverhead;
            }
        }

        public List<RankedWord> GetTopRankedWords()
        {
            this.Model = this.BestWordModel; // Update the model
            this.D += this.d; // Update the dictionary size
            this.E = this.Ew;
            this.WordIndex += 1; // Update the word index
            var word = this.BestWord;
            this.BestWord = RankedWord.Empty;
            if (word.Rank <= 0)
            {
                var entropy = this.Model.GetEntropy();
                Console.WriteLine($"[ESTIMATE] Final entropy: {entropy}");
                Console.WriteLine($"[ESTIMATE] Uncompressed dictionary size estimated: {D}");
                return new List<RankedWord>() { RankedWord.Empty };
            }
            Console.WriteLine($"E: {E}; d: {d}");
            return new List<RankedWord>() { word };
        }

        private class Statistics
        {
            public Statistics() { }
            public Statistics(Statistics model) => this.Order0 = new OccurenceDictionary<ushort>(model.Order0);

            public OccurenceDictionary<ushort> Order0 { get; } = new();

            public double GetEntropy()
            {
                double entropy = 0;
                double n = this.Order0.Sum();
                foreach (var context in this.Order0.Keys)
                {
                    var px = this.Order0[context] / n;
                    entropy -= px * Math.Log2(px);
                }
                return entropy;
            }
        }
    }
}