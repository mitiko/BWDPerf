using System;

namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public struct Match : IComparable<Match>
    {
        // Index in buffer
        public int Index { get; set; }
        // How many matches before parsing (located together in the SA)
        public int SACount { get; set; }
        // How many matches after parsing
        public int Count { get; set; }
        // Length of word
        public int Length { get; }

        public Match(int index, int saCount, int count, int length)
        {
            if (index < 0 || saCount < 0 || count < 0 || length <= 0)
                throw new ArgumentException("Invalid initialization of a match struct");
            this.Index = index;
            this.SACount = saCount;
            this.Count = count;
            this.Length = length;
        }

        public int CompareTo(Match other)
        {
            // Sort matches in ascending order of end index and descending order of start index
            // This way we can detect when the predicate of being inside a range is over
            
            // As an optimization we don't compute -1 on the end index
            var thisEnd = this.Index + this.Length;
            var otherEnd = other.Index + other.Length;
            if (thisEnd > otherEnd) return 1;
            else if (thisEnd < otherEnd) return -1;
            else
            {
                if (this.Index > other.Index) return -1;
                else if (this.Index < other.Index) return 1;
                else return 0;
            }
        }
    }
}