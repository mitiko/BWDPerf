using System;
using System.Collections.Generic;
using System.Diagnostics;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Matching
{
    // Provides matches faster by caching them in a linked list
    public class LCPStaticMatchFinder : IBWDMatchProvider
    {
        public SkipList<Match> MatchList { get; set; } = new();
        public BWDIndex BWDIndex { get; private set; }

        public void Initialize(BWDIndex BWDIndex)
        {
            var timer = Stopwatch.StartNew();
            this.BWDIndex = BWDIndex;

            foreach (var match in GenerateMatches())
                this.MatchList.Insert(match);
            
            Console.WriteLine($"Initializing the skip list took: {timer.Elapsed}");
            Console.WriteLine($"Skip list len: {this.MatchList.Count}");
        }
        
        public IEnumerable<Match> GetMatches() => this.MatchList.Enumerate();

        public void RemoveIfPossible(Match match) => this.MatchList.Remove(match);

        private IEnumerable<Match> GenerateMatches()
        {
            int n = this.BWDIndex.Length;
            var matches = new Stack<Match>();

            for (int i = 0; i < n; i++)
            {
                var lcp = this.BWDIndex.LCP[i];
                if (matches.Count == 0)
                {
                    for (int l = 2; l <= lcp; l++)
                        matches.Push(new Match(i, 0, l));
                }

                while (matches.TryPeek(out var match) && lcp < match.Length)
                {
                    var m = matches.Pop();
                    m.SACount = i - m.Index + 1;
                    yield return m;
                }
                while (matches.TryPeek(out var match) && lcp > match.Length)
                {
                    matches.Push(new Match(i, 0, match.Length + 1));
                }
            }
            Debug.Assert(matches.Count == 0, "There were matches left in the LCP stack.");
        }
    }
}