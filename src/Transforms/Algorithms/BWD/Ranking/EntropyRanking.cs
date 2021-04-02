// using System;
// using System.Collections.Generic;
// using BWDPerf.Interfaces;
// using BWDPerf.Tools;
// using BWDPerf.Transforms.Algorithms.BWD.Entities;

// namespace BWDPerf.Transforms.Algorithms.BWD.Ranking
// {
//     public class EntropyRanking : IBWDRanking
//     {
//         private RankedWord BestWord { get; set; }
//         private readonly RankedWord InitialWord = RankedWord.Empty;
//         private Dictionary<byte, double> FreqTable { get; } = new();

//         private double C = 1; // symbols per byte
//         private double H = 8; // bits per symbol (entropy)
//         private double n = 0; // block size
//         private double dC = 0; // best word's change in C
//         private double dH = 0; // best word's change in H

//         public void Initialize(ReadOnlyMemory<byte> buffer)
//         {
//             // TODO: Start with a bias for initial dictionry overhead
//             var symbols = new OccurenceDictionary<byte>();
//             this.n = buffer.Length;
//             double entropy = 0;
//             for (int i = 0; i < n; i++)
//                 symbols.Add(buffer.Span[i]);
//             foreach (var kvp in symbols)
//                 this.FreqTable.Add(kvp.Key, kvp.Value / n);
//             foreach (var kvp in this.FreqTable)
//             {
//                 Console.WriteLine($"'{(char) kvp.Key}' --> {kvp.Value}");
//                 entropy -= kvp.Value * Math.Log2(kvp.Value);
//             }
//             Console.WriteLine($"-> Inititial entropy: {entropy}");

//             this.H = entropy;
//             this.BestWord = RankedWord.Empty;
//         }

//         public void Rank(Word word, int count, ReadOnlyMemory<byte> buffer)
//         {
//             double pw = (double) count / n;
//             double deltaC = pw * (word.Length - 1);
//             // TODO: Store deltaC in a hash table of [length][count]
//             double deltaH = pw * Math.Log2(pw);
//             for (int s = 0; s < word.Length; s++)
//             {
//                 double px = this.FreqTable[buffer.Span[word.Location + s]];
//                 double pChange = px - pw;
//                 deltaH += pChange * Math.Log2(pChange) - px * Math.Log2(px);
//             }
//             double dictOverhead = (double) 8 * (word.Length + 1) / n;
//             double rank = C * deltaH + deltaC * H + deltaC * deltaH - dictOverhead;

//             if (rank > this.BestWord.Rank)
//             {
//                 this.BestWord = new RankedWord(word, rank);
//                 this.dC = deltaC;
//                 this.dH = deltaH;
//             }
//         }

//         public List<RankedWord> GetTopRankedWords()
//         {
//             this.C -= this.dC;
//             this.H -= this.dH;
//             var word = this.BestWord;
//             this.BestWord = RankedWord.Empty;
//             if (word.Rank <= 0)
//             {
//                 Console.WriteLine($"Final entropy estimated: {this.H}");
//                 return new List<RankedWord>() { RankedWord.Empty };
//             }
//             Console.WriteLine($"dH: {dH}");
//             Console.WriteLine($"H: {H}");
//             Console.WriteLine($"Rank: {word.Rank}");
//             return new List<RankedWord>() { word };
//         }
//     }
// }