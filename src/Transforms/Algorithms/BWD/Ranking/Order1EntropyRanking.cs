using System;
using System.Collections.Generic;
using System.Linq;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Ranking
{
    public class Order1EntropyRanking : IBWDRanking
    {
        private RankedWord BestWord { get; set; }
        private readonly RankedWord InitialWord = RankedWord.Empty;
        private Dictionary<byte, double> FreqTable { get; } = new();
        private OccurenceDictionary<byte>[] QO { get; set; }
        private Dictionary<byte, double>[] QTable { get; set; }

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

            this.QTable = new Dictionary<byte, double>[256];
            this.QO = new OccurenceDictionary<byte>[256];
            for (int i = 0; i < 256; i++)
            {
                this.QTable[i] = new Dictionary<byte, double>();
                this.QO[i] = new OccurenceDictionary<byte>();
            }
            var context = buffer.Span[0];
            for (int i = 1; i < n; i++)
            {
                this.QO[context].Add(buffer.Span[i]);
                context = buffer.Span[i];
            }
            for (int i = 0; i < QO.Length; i++)
            {
                double sum = QO[i].Sum();
                if (sum == 0) continue;
                foreach (var count in QO[i])
                {
                    QTable[i][count.Key] = count.Value / sum;
                }
            }
            double sumP = 0;
            double sumQ = 0;
            double sumB = 0;
            double e1 = 0;
            foreach (var kvp in this.FreqTable)
            {
                sumP += kvp.Value;
                var p = kvp.Value;
                double q = 0;
                int qCount = 0;
                double bayes = 0;
                for (int i = 0; i < QTable.Length; i++)
                {
                    if (QTable[i].ContainsKey(kvp.Key))
                    {
                        q += QTable[i][kvp.Key];
                        // p(b) = p(b | a) * p(a)
                        var c = this.FreqTable[(byte) i];
                        bayes += QTable[i][kvp.Key] * c;
                        e1 -= c * QTable[i][kvp.Key] * Math.Log2(QTable[i][kvp.Key]);
                        // e1 -= p * Math.Log2(QTable[i][kvp.Key]);
                        qCount++;
                    }
                }
                q /= qCount;
                sumQ += q;
                sumB += bayes;
                Console.WriteLine($"'{(char) kvp.Key}' --> {(kvp.Value).ToString("C4")}; {bayes.ToString("C4")}");
                entropy -= kvp.Value * Math.Log2(kvp.Value);
            }
            Console.WriteLine($"Sum - p: {sumP}, q: {sumQ}, b: {sumB}");
            Console.WriteLine($"-> Inititial entropy: {entropy}, e1: {e1}");
            this.H = entropy;
            this.BestWord = RankedWord.Empty;
        }

        public void Rank(Word word, int count, ReadOnlyMemory<byte> buffer)
        {
            double pw = (double) count / n;
            double deltaC = pw * (word.Length - 1);
            // TODO: Store deltaC in a hash table of [length][count]
            double deltaH = pw * Math.Log2(pw);
            for (int s = 0; s < word.Length; s++)
            {
                double px = this.FreqTable[buffer.Span[word.Location + s]];
                double pChange = px - pw;
                deltaH += pChange * Math.Log2(pChange) - px * Math.Log2(px);
            }
            double dictOverhead = (double) 8 * (word.Length + 1) / n;
            double rank = C * deltaH + deltaC * H + deltaC * deltaH - dictOverhead;

            if (rank > this.BestWord.Rank)
            {
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
            this.BestWord = RankedWord.Empty;
            if (word.Rank <= 0)
            {
                Console.WriteLine($"Final entropy estimated: {this.H}");
                return new List<RankedWord>() { RankedWord.Empty };
            }
            Console.WriteLine($"dH: {dH}");
            Console.WriteLine($"H: {H}");
            Console.WriteLine($"Rank: {word.Rank}");
            return new List<RankedWord>() { word };
        }
    }
}