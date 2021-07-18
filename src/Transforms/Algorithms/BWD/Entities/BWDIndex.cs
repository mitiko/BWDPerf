using System;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public class BWDIndex
    {
        public int Length { get; }
        public SuffixArray SA { get; }
        public int[] SAinv { get; }
        public BitVector BitVector { get; }
        public LCPArray LCP { get; }
        public ReadOnlyMemory<byte> Buffer { get; }
        private Memory<ushort> Index { get; }
        private ushort WordIndex { get; set; } = 256;

        public ushort this[int index] => this.Index.Span[index];

        public BWDIndex(ReadOnlyMemory<byte> buffer)
        {
            this.Length = buffer.Length;
                var timer = System.Diagnostics.Stopwatch.StartNew();
            this.SA = new SuffixArray(buffer);
                Console.WriteLine($"Suffix array took: {timer.Elapsed}"); timer.Restart();
            this.LCP = new LCPArray(buffer, this.SA, out var SAinv);
            this.SAinv = SAinv;
                Console.WriteLine($"LCP array took: {timer.Elapsed}"); timer.Stop();
            this.BitVector = new BitVector(buffer.Length, bit: true);
            this.Buffer = buffer;
            this.Index = new ushort[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
                this.Index.Span[i] = buffer.Span[i];
        }

        public int Count(Match match)
        {
            var locations = this.SA[match.Index..(match.Index + match.SACount)];
            Array.Sort(locations);
            var count = 0;
            var lastMatch = - match.Length;
            for (int i = 0; i < locations.Length; i++)
            {
                var loc = locations[i];
                if (loc < lastMatch + match.Length) continue;

                bool available = true;
                for (int s = 0; s < match.Length; s++)
                    if (!this.BitVector[loc+s]) { available = false; break; }

                if (available)
                {
                    lastMatch = loc;
                    count += 1;
                }
            }
            return count;
        }

        public int[] Parse(Match match)
        {
            var locations = this.SA[match.Index..(match.Index + match.Count)];
            Array.Sort(locations);
            return this.Parse(locations, match.Length);
        }

        public int[] Parse(int[] rawSortedLocations, int wordLength)
        {
            var results = new int[rawSortedLocations.Length];
            var lastMatch = - wordLength;
            var index = 0;

            for (int i = 0; i < rawSortedLocations.Length; i++)
            {
                var loc = rawSortedLocations[i];
                if (loc < lastMatch + wordLength) continue;
                bool available = true;
                for (int s = 0; s < wordLength; s++)
                    if (!this.BitVector[loc+s]) { available = false; break; }
                if (available)
                {
                    lastMatch = loc;
                    results[index++] = loc;
                }
            }

            Array.Resize(ref results, index);
            return results;
        }

        public void MarkWordAsUnavailable(Word chosenWord, out int[] locations)
        {
            // This word has been added to the dictionary; mark it as unavailable
            var word = this.Buffer.Slice(chosenWord.Location, chosenWord.Length);
            var rawSortedLocations = this.SA.Search(this.Buffer, word);
            locations = this.Parse(rawSortedLocations, word.Length);

            for (int i = 0; i < locations.Length; i++)
            {
                for (int j = 0; j < word.Length; j++)
                {
                    this.BitVector[locations[i]+j] = false;
                    this.Index.Span[locations[i]+j] = this.WordIndex;
                }
            }
            this.WordIndex++;
        }
    }
}