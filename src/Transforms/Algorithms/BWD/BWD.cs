using System;
using BWDPerf.Interfaces;
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

                var rankedWord = this.Ranking.GetTopRankedWords()[0];
                var word = rankedWord.Word;
                if (word.Equals(Word.Empty))
                    break;
                dictionary[i] = buffer.Slice(word.Location, word.Length).ToArray();
                PrintWord(rankedWord);

                this.BWDIndex.MarkWordAsUnavailable(word);
                matchingTime += timer.Elapsed; timer.Restart();
            }

            // If there are no more good words, add the remaining of the individual symbols to the dictionary
            // TODO: Extract this in BWDIndex.ExtractSingleCharacters or smth.
            for (int i = dictionary.Count; !this.BWDIndex.BitVector.IsEmpty(); i++)
            {
                var symbol = Word.Empty;
                for (int j = 0; j < buffer.Length; j++)
                {
                    if (this.BWDIndex.BitVector[j])
                    {
                        symbol = new Word(j, 1);
                        this.BWDIndex.MarkWordAsUnavailable(symbol);
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

        private void PrintWord(RankedWord rw)
        {
            var str = "";
            var loc = rw.Word.Location;
            var len = rw.Word.Length;
            var bytes = this.BWDIndex.Buffer
                .Slice(loc, len)
                .ToArray()
                .AsSpan();
            foreach (var symbol in bytes)
                str += (char) symbol;
            var count = rw.Rank / (len - 1) + 1;
            Console.WriteLine($"word -- '{str}'; ({loc}, {len}) -- {count}");
        }
    }
}