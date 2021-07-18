using System;
using System.Collections.Generic;
using System.Diagnostics;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Matching
{
    public class LCPTableMatchProvider : IBWDMatchProvider
    {
        public BWDIndex BWDIndex { get; private set; }
        private SkipList<Match> Matches { get; set; } = new();

        public void Initialize(BWDIndex BWDIndex)
        {
            var timer = Stopwatch.StartNew();
            this.BWDIndex = BWDIndex;

            foreach (var match in GenerateMatches())
                this.Matches.Insert(match);
            Console.WriteLine($"Skip list took: {timer.Elapsed}");
        }

        public IEnumerable<Match> GetMatches() => this.Matches.Enumerate();

        public void RecountWord(Word chosenWord, int[] locations)
        {
            // TODO: Find all locations
            // FOR NOW!!! : Fixed by passing the reference to the same locations array
            // var word = this.BWDIndex.Buffer.Slice(chosenWord.Location, chosenWord.Length);
            // var rawSortedLocations = this.BWDIndex.SA.Search(this.BWDIndex.Buffer, word);
            // var locations = this.BWDIndex.Parse(rawSortedLocations, word.Length);
            int len = chosenWord.Length;
            var toRemove = new List<Match>();

            for (int i = 0; i < locations.Length; i++)
            {
                var locStart = locations[i]; // Start inclusive
                var locEnd = locStart + len; // End exclusive
                // If the end exceeds the start index of the location we (might) have an intersection
                Predicate<Match> predicate = m => m.Index + m.Length > locStart;
                bool maybeBreak = false;
                // System.Console.WriteLine($"loc range: [{locStart}, {locEnd})");

                // Since the skip list is ordered by end index in ascending order
                // the binary search guarantees us that all following nodes have their
                // match's end index exceeding the location's start index
                // The only case when there's no intersection is when the start index of
                // the match exceeds the end index of the location.
                // Due to the nature of ranges, we cannot sort the matches in such a way
                // that the second condition is also continuous on the skip list
                // (if it were, we could perform a second binary search and find all intersections fast)
                // But with a second ordering by the start index in descending order
                // we can be certain there's no more nodes with intersecting matches after
                // two consecutive secondary condition failures
                for (
                    var node = this.Matches.BinarySearchFirstInRange(predicate);
                    node != null;
                    node = node.Next[0])
                {
                    var m = node.Value;
                    var areIntersecting = m.Index < locEnd;


                    if (!areIntersecting)
                    {
                        if (maybeBreak) break;
                        else maybeBreak = true;
                    }
                    else
                    {
                        // Recount
                        m.Count = this.BWDIndex.Count(m);
                        if (m.Count < 2 || m.Length < 2)
                            toRemove.Add(node.Value);
                        else
                            node.Value = m;
                        maybeBreak = false;
                    }
                    // var mb = maybeBreak ? 1 : 0;
                    // System.Console.WriteLine($"[{m.Index:000000}, {m.Index + m.Length:000000}) {areIntersecting}, {mb}");
                }
                // Environment.Exit(1);
            }
            foreach (var value in toRemove)
                this.Matches.Remove(value);
            Console.WriteLine($"Removed {toRemove.Count}");
            toRemove.Clear();
        }

        private IEnumerable<Match> GenerateMatches()
        {
            var matches = new Stack<Match>();

            for (int i = 0; i < this.BWDIndex.Length; i++)
            {
                var lcp = this.BWDIndex.LCP[i];
                if (matches.Count == 0)
                {
                    for (int l = 2; l <= lcp; l++)
                        matches.Push(new Match(i, 0, 0, l));
                }

                while (matches.TryPeek(out var match) && lcp < match.Length)
                {
                    var m = matches.Pop();
                    m.SACount = i - m.Index + 1;
                    m.Index = this.BWDIndex.SA[m.Index];
                    m.Count = this.BWDIndex.Count(m);
                    yield return m;
                }
                while (matches.TryPeek(out var match) && lcp > match.Length)
                {
                    matches.Push(new Match(i, 0, 0, match.Length + 1));
                }
            }

            Debug.Assert(matches.Count == 0, "There were matches left in the LCP stack.");
        }
    }
}