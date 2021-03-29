using System;
using System.Linq;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Counting;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    internal class BWD
    {
        internal IBWDRanking Ranking { get; }
        internal IBWDMatching MatchFinder { get; }
        internal BWDIndex BWDIndex { get; private set; }

        internal BWD(IBWDRanking ranking, IBWDMatching matchFinder)
        {
            this.Ranking = ranking;
            this.MatchFinder = matchFinder;
        }

        internal BWDictionary CalculateDictionary(ReadOnlyMemory<byte> buffer)
        {
            this.BWDIndex = new BWDIndex(buffer);

            var timer = System.Diagnostics.Stopwatch.StartNew();
            var rankingTime = new TimeSpan();
            var matchingTime = new TimeSpan();

            this.Ranking.Initialize(this.BWDIndex);
            rankingTime += timer.Elapsed; timer.Restart();
            this.MatchFinder.Initialize(this.BWDIndex);
            matchingTime += timer.Elapsed; timer.Restart();

            var dictionary = new BWDictionary();
            for (int i = 0; !this.BWDIndex.BitVector.IsEmpty(); i++)
            {
                foreach (var match in this.MatchFinder.GetMatches())
                    this.Ranking.Rank(match);
                rankingTime += timer.Elapsed; timer.Restart();

                var word = this.Ranking.GetTopRankedWords()[0].Word;
                if (word.Equals(Word.Empty))
                    break;
                dictionary[i] = buffer.Slice(word.Location, word.Length).ToArray();
                PrintWord(word);

                this.MatchFinder.UpdateState(word);
                matchingTime += timer.Elapsed; timer.Restart();
            }

            // If there are no more good words, add the remaining of the individual symbols to the dictionary
            for (int i = dictionary.Count; !this.BWDIndex.BitVector.IsEmpty(); i++)
            {
                var symbol = Word.Empty;
                for (int j = 0; j < buffer.Length; j++)
                {
                    if (this.BWDIndex.BitVector[j])
                    {
                        symbol = new Word(j, 1);
                        var locations = this.BWDIndex.SA.Search(buffer, buffer.Slice(j, 1));
                        for (int l = 0; l < locations.Length; l++) this.BWDIndex.BitVector[locations[l]] = false;
                        break;
                    }
                }
                dictionary[i] = buffer.Slice(symbol.Location, symbol.Length);
            }

            Console.WriteLine($"Dict size is {dictionary.Count}");
            Console.WriteLine($"Total ranking time: {rankingTime}; Avg: {rankingTime / dictionary.Count}");
            Console.WriteLine($"Total counting time: {matchingTime}; Avg: {matchingTime / dictionary.Count}");

            return dictionary;
        }

        private void PrintWord(Word word)
        {
            var str = "";
            var bytes = this.BWDIndex.Buffer
                .Slice(word.Location, word.Length)
                .ToArray()
                .AsSpan();
            foreach (var symbol in bytes)
                str += (char) symbol;
            Console.WriteLine($"word -- '{str}'; {word.Location} {word.Length}");
        }
    }
}