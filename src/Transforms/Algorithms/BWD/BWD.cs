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
            // Initialize the index
            this.BWDIndex = new BWDIndex(buffer);
            // Initialize the match finder and ranking
            this.MatchFinder.Initialize(this.BWDIndex);
            this.Ranking.Initialize(this.BWDIndex);

            var timer = System.Diagnostics.Stopwatch.StartNew();

            // Initialize the dictionary
            var dictionary = new BWDictionary();
            for (int i = 0; !this.BWDIndex.BitVector.IsEmpty(); i++)
            {
                // 0xFFFF is reserved. 0x0000-0x0100 is for single characters.
                // This implies max dict size is 65278 words.
                // Note no backwards compatibility is ensured!
                if (i == ushort.MaxValue - 256) break;
                foreach (var match in this.MatchFinder.GetMatches())
                    this.Ranking.Rank(match);

                var rankedWord = this.Ranking.GetTopRankedWords()[0];
                var word = rankedWord.Word;
                if (word.Equals(Word.Empty))
                    break;
                dictionary[i] = buffer.Slice(word.Location, word.Length).ToArray();
                PrintWord(rankedWord);

                this.BWDIndex.MarkWordAsUnavailable(word);
            }

            var elapsedTime = timer.Elapsed;
            Console.WriteLine($"Dict size is {dictionary.Count}");
            Console.WriteLine($"Time to compute dictionary: {elapsedTime}");
            if (dictionary.Count != 0) Console.WriteLine($"Avg time spent per word: {elapsedTime / dictionary.Count}");

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

            Console.WriteLine($"word -- '{str}'; ({loc}, {len}, {rw.Count}) -- {rw.Rank}");
        }
    }
}