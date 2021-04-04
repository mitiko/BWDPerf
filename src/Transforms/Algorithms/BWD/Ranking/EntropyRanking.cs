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
        private Dictionary<byte, double> FreqTable { get; } = new();
        public BWDIndex BWDIndex { get; private set; }

        private double C = 1; // symbols per byte
        private double H = 8; // bits per symbol (entropy)
        private double n = 0; // block size
        private double dC = 0; // best word's change in C
        private double dH = 0; // best word's change in H

        public void Initialize(BWDIndex BWDIndex)
        {
            // TODO: Start with a bias for initial dictionary overhead
            this.BWDIndex = BWDIndex;
            this.n = this.BWDIndex.Buffer.Length;

            var symbols = new OccurenceDictionary<byte>();
            double entropy = 0;
            for (int i = 0; i < n; i++)
                symbols.Add(this.BWDIndex.Buffer.Span[i]);
            foreach (var kvp in symbols)
                this.FreqTable.Add(kvp.Key, kvp.Value / n);
            foreach (var kvp in this.FreqTable)
            {
                Console.WriteLine($"er: '{(char)kvp.Key}' --> {kvp.Value}");
                entropy -= kvp.Value * Math.Log2(kvp.Value);
            }
            Console.WriteLine($"-> Inititial entropy: {entropy}");

            this.H = entropy;
            this.BestWord = RankedWord.Empty;
        }

        public void Rank(Match match)
        {
            int count = this.BWDIndex.GetParsedCount(match);
            var location = this.BWDIndex.SA[match.Index];
            // TODO this n should reflect C
            double pw = (double) count / n;
            double deltaC = pw * (match.Length - 1); // TODO: Store deltaC in a hash table of [length][count]
            double deltaH = pw * Math.Log2(pw);
            for (int s = 0; s < match.Length; s++)
            {
                double px = this.FreqTable[this.BWDIndex.Buffer.Span[location + s]];
                double pChange = px - pw;
                deltaH += pChange * Math.Log2(pChange) - px * Math.Log2(px);
            }
            double dictOverhead = (double)8 * (match.Length + 1) / n;
            double rank = C * deltaH + deltaC * H + deltaC * deltaH - dictOverhead;

            if (rank > this.BestWord.Rank)
            {
                this.BestWord = new RankedWord(new Word(location, match.Length), rank);
                this.dC = deltaC;
                this.dH = deltaH;
            }
        }

        public List<RankedWord> GetTopRankedWords()
        {
            this.C -= this.dC;
            this.H -= this.dH;
            var word = this.BestWord;
            this.BestWord = RankedWord.Empty;
            if (word.Rank <= 0)
            {
                Console.WriteLine($"Final entropy estimated: {this.H}");
                return new List<RankedWord>() { RankedWord.Empty };
            }
            Console.WriteLine($"H: {H}; dH: {dH}");
            return new List<RankedWord>() { word };
        }
    }
}