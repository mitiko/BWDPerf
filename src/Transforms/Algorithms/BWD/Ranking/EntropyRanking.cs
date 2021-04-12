using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Ranking
{
    public class EntropyRanking : IBWDRanking
    {
        private RankedWord BestWord { get; set; }
        private readonly RankedWord InitialWord = RankedWord.Empty;
        private List<int> Model { get; set; } = new();
        private List<int> BestWordModel { get; set; } = new();
        public BWDIndex BWDIndex { get; private set; }

        // State
        private double E = 0; // encoded size in bits (H * symbol count)
        private double D = 4096; // initial dictionary size in bits (256 symbols with 2 bytes per symbol)
        private double S = 256; // symbol count
        // New state (after dictionary update)
        private double Ew = 0; // encoded size with the change in dictionary (in bits)
        private double d = 0; // dictionary update in bits
        private double Sw = 0; // updated symbol count

        public void Initialize(BWDIndex BWDIndex)
        {
            // TODO: Start with a bias for initial dictionary overhead
            this.BWDIndex = BWDIndex;
            this.Model = new List<int>(capacity: 512);
            this.S = this.BWDIndex.Length;
            for (int i = 0; i < 256; i++)
                this.Model.Add(0);
            for (int i = 0; i < this.BWDIndex.Length; i++)
                this.Model[this.BWDIndex[i]]++;
            var entropy = GetEntropy(this.Model);
            Console.WriteLine($"-> Inititial entropy: {entropy}");
            this.BestWord = RankedWord.Empty;
            this.E = entropy * this.BWDIndex.Length;
        }

        public void Rank(Match match)
        {
            if (match.Length < 2) return; // Rank of single characters is 0
            var (count, loc) = this.BWDIndex.GetParsedCountAndLocation(match);
            if (count < 2) return; // Must locate match at at least 2 locations to get gains

            // Copy the model
            var model = new List<int>(this.Model);
            // Update the model
            model.Add(count);
            for (int s = 0; s < match.Length; s++)
                model[this.BWDIndex[loc+s]] -= count;
            // Console.WriteLine(GetEntropy());

            double dictOverhead = 8 * (match.Length + 1); // TODO: Try a more relaxing dict overhead - 5x?
            // double encodedSize = GetEntropy(model) * Sum(model);
            double encodedSize = GetSize(model, count, match.Length);
            double rank = this.E - encodedSize - dictOverhead;

            if (rank > this.BestWord.Rank)
            {
                this.BestWord = new RankedWord(new Word(loc, match.Length), rank);
                this.BestWordModel = new List<int>(model);
                this.Ew = encodedSize;
                this.Sw = GetNewSymbolCount(count, match.Length);
                this.d = dictOverhead;
            }
        }

        public List<RankedWord> GetTopRankedWords()
        {
            this.Model = new List<int>(this.BestWordModel); // Update the model
            this.D += this.d; // Update the dictionary size
            this.E = this.Ew;
            this.S = this.Sw;
            var word = this.BestWord;
            this.BestWord = RankedWord.Empty;
            if (word.Rank <= 0)
            {
                var entropy = GetEntropy(this.Model);
                Console.WriteLine($"[ESTIMATE] Final entropy: {entropy}");
                Console.WriteLine($"[ESTIMATE] Uncompressed dictionary size estimated: {D}");
                return new List<RankedWord>() { RankedWord.Empty };
            }
            Console.WriteLine($"E: {E}; d: {d}");
            return new List<RankedWord>() { word };
        }

        private double GetEntropy(List<int> model)
        {
            double n = this.S;
            double entropy = n * Math.Log2(n);

            for (int i = 0; i < model.Count; i++)
                entropy -= model[i] * Math.Log2(Math.Max(1, model[i]));

            entropy /= n;
            return entropy;
        }

        private double GetSize(List<int> model, int count, int length)
        {
            double n = GetNewSymbolCount(count, length);
            double size = n * Math.Log2(n);

            for (int i = 0; i < model.Count; i++)
                size -= model[i] * Math.Log2(Math.Max(1, model[i]));

            return size;
        }

        private double GetNewSymbolCount(int count, int length)
        {
            // Each of $l$ letters we replace with a single word at $c$ locations
            return this.S + (1 - length) * count;
        }
    }
}