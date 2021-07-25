using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Ranking
{
    public class Order1EntropyRanking : IBWDRankProvider
    {
        private RankedWord BestWord { get; set; } = RankedWord.Empty;
        private Statistics Model { get; set; } = new();
        private Statistics BestWordModel { get; set; } = new();
        private IBWDMatchProvider MatchProvider { get; set; }
        public BWDIndex BWDIndex { get; private set; }

        // State
        private double E = 0; // encoded size in bits (H * symbol count)
        private double D = 4096; // initial dictionary size in bits (256 symbols with 2 bytes per symbol)
        private ushort WordIndex = 256; // word index in the dictionary
        // New state (after dictionary update)
        private double Ew = 0; // encoded size with the change in dictionary (in bits)
        private double d = 0; // dictionary update in bits

        public void Initialize(BWDIndex BWDIndex, IBWDMatchProvider matchProvider)
        {
            // TODO: Start with a bias for initial dictionary overhead
            // Initialize model
            this.BWDIndex = BWDIndex;
            this.MatchProvider = matchProvider;
            int n = this.BWDIndex.Length;
            for (int i = 0; i < n; i++)
                this.Model.Order0.Add(this.BWDIndex[i]);
            foreach (var context in this.Model.Order0.Keys)
                this.Model.Order1[context] = new OccurenceDictionary<ushort>();
            for (int i = 0; i < n-1; i++)
                this.Model.Order1[this.BWDIndex[i]].Add(this.BWDIndex[i+1]);

            double o0Entropy = this.Model.GetOrder0Entropy(), o1Entropy = this.Model.GetEntropy();
            Console.WriteLine($"-> Inititial entropy: o0: {o0Entropy}, o1: {o1Entropy}");
            this.E = o1Entropy * this.BWDIndex.Length;
        }

        public void Rank(Match match)
        {
            // Rank of single characters is 0
            if (match.Length < 2) { this.MatchProvider.RemoveIfPossible(match); return; }
            var locs = this.BWDIndex.Parse(match); // Get matching locations
            // Must locate match at at least 2 locations to get gains
            if (locs.Length < 2) { this.MatchProvider.RemoveIfPossible(match); return; }
            var loc = locs[0];

            // Copy the model
            var model = new Statistics(this.Model);
            // Update the model (done in 2 parts - update order0 counts and update order1 counts)
            // Update order0 symbol counts
            model.Order0.Add(this.WordIndex, locs.Length);
            for (int s = 0; s < match.Length; s++)
                model.Order0.SubstractMany(this.BWDIndex[loc+s], locs.Length); // For now words can only cover single characters, not other words
            // Update order1 symbol counts (done in 3 parts - symbols inside word, contexts of word, word is context)
            // 1) Update symbols inside word
            for (int s = 0; s < match.Length - 1; s++)
                model.Order1[this.BWDIndex[loc+s]].SubstractMany(this.BWDIndex[loc+s+1], locs.Length);
            // 2) Update contexts
            for (int i = ((locs[0] == 0) ? 1 : 0); i < locs.Length; i++)
            {
                model.Order1[this.BWDIndex[locs[i]-1]].Add(this.WordIndex);
                model.Order1[this.BWDIndex[locs[i]-1]].Substract(this.BWDIndex[loc]);
            }
            // 3) Update when the words is a context
            model.Order1.Add(this.WordIndex, new OccurenceDictionary<ushort>());
            for (int i = locs.Length - ((locs[^1] == this.BWDIndex.Length - match.Length) ? 2 : 1); i >= 0 ; i--)
            {
                model.Order1[this.WordIndex].Add(this.BWDIndex[locs[i]+match.Length]);
                model.Order1[this.BWDIndex[loc+match.Length-1]].Substract(this.BWDIndex[locs[i]+match.Length]);
            }
            // NOTE: 2) and 3) do a boundaries check just once, not at each location because the locations are sorted
            // If the locations are not sorted, we have to do the boundaries check for each location, not just once

            double dictOverhead = 8 * (match.Length + 1);
            double encodedSize = model.GetEntropy() * model.Order0.Sum();
            double rank = this.E - encodedSize - dictOverhead;

            if (rank > this.BestWord.Rank)
            {
                this.BestWord = new RankedWord(new Word(loc, match.Length), rank, locs.Length);
                this.BestWordModel = model;
                this.Ew = encodedSize;
                this.d = dictOverhead;
            }
        }

        public List<RankedWord> GetTopRankedWords()
        {
            var word = this.BestWord;
            if (word.Rank <= 0 || this.WordIndex == ushort.MaxValue)
            {
                var o1Entropy = this.Model.GetEntropy();
                var o0Entropy = this.Model.GetOrder0Entropy();
                Console.WriteLine($"[ESTIMATE] Final entropy: o0: {o0Entropy}; o1: {o1Entropy}");
                Console.WriteLine($"[ESTIMATE] Uncompressed dictionary size estimated: {D}");
                return new List<RankedWord>() { RankedWord.Empty };
            }
            this.Model = this.BestWordModel; // Update the model
            this.D += this.d; // Update the dictionary size
            this.E = this.Ew; // Update the state
            this.WordIndex += 1; // Update the word index
            this.BestWord = RankedWord.Empty;
            return new List<RankedWord>() { word };
        }

        private class Statistics
        {
            public Statistics() { }
            public Statistics(Statistics model)
            {
                this.Order0 = new OccurenceDictionary<ushort>(model.Order0);
                this.Order1 = new Dictionary<ushort, OccurenceDictionary<ushort>>();
                foreach (var kvp in model.Order1)
                    this.Order1[kvp.Key] = new OccurenceDictionary<ushort>(kvp.Value);
            }

            public OccurenceDictionary<ushort> Order0 { get; } = new();
            public Dictionary<ushort, OccurenceDictionary<ushort>> Order1 { get; } = new();

            public double GetEntropy()
            {
                double entropy = 0;
                double n = this.Order0.Sum();
                foreach (var context in this.Order0.Keys)
                {
                    var py = this.Order0[context] / n;
                    double cn = this.Order1[context].Sum();
                    foreach (var count in this.Order1[context].Values)
                    {
                        var pxy = count / cn;
                        entropy -= py * pxy * Math.Log2(pxy);
                    }
                }
                return entropy;
            }

            public double GetOrder0Entropy()
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