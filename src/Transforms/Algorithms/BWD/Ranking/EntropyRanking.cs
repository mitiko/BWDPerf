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
        private readonly RankedWord InitialWord = new RankedWord(new Word(-1, -1), double.MinValue);
        private Dictionary<byte, double> FreqTable { get; } = new();

        private double C = 1; // symbols per byte
        private double H = 8; // bits per symbol (entropy)
        private double n = 0; // block size
        private double dC = 0; // best word's change in C
        private double dH = 0; // best word's change in H

        public void Initialize(ReadOnlyMemory<byte> buffer)
        {
            // TODO: Start with a bias for initial dictionry overhead
            var symbols = new OccurenceDictionary<byte>();
            this.n = buffer.Length;
            double entropy = 0;
            for (int i = 0; i < n; i++)
                symbols.Add(buffer.Span[i]);
            foreach (var kvp in symbols)
                this.FreqTable.Add(kvp.Key, kvp.Value / n);
            foreach (var kvp in this.FreqTable)
            {
                Console.WriteLine($"'{(char) kvp.Key}' --> {kvp.Value}");
                entropy -= kvp.Value * Math.Log2(kvp.Value);
            }

            this.H = entropy;
            this.BestWord = InitialWord;
        }

        public void Rank(Word word, int count, ReadOnlyMemory<byte> buffer)
        {
            double pw = (double) count / (n - word.Length + 1);
            double deltaC = pw * (1d / word.Length - word.Length);
            // TODO: store deltaC in a hash table. It currently doesn't change after choosing a word.
            double deltaH = - pw * Math.Log2(pw);
            for (int s = 0; s < word.Length; s++)
            {
                double px = this.FreqTable[buffer.Span[word.Location + s]];
                // Console.WriteLine($"px: {px}");
                double pChange = px - pw;
                // Console.WriteLine($"pChange: {pChange}");
                deltaH -= (pChange * Math.Log2(pChange) - px * Math.Log2(px));
            }
            double dictOverhead = (double) (word.Length + 1) / n;
            double rank = C * deltaH + deltaC * H - deltaC * deltaH - dictOverhead;

            if (rank > this.BestWord.Rank)
            {
                Console.WriteLine($"deltaH: {deltaH}");
                Console.WriteLine($"H: {H}");
                Console.WriteLine($"Rank: {rank}");
                this.BestWord = new RankedWord(word, rank);
                this.dC = deltaC;
                this.dH = deltaH;
            }
        }

        public List<RankedWord> GetTopRankedWords()
        {
            this.C -= this.dC;
            this.H -= this.dH;
            var word = this.BestWord;
            this.BestWord = InitialWord;
            return new List<RankedWord>() { word };
        }
    }
}