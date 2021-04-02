namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public struct Match
    {
        // Index in SA
        public int Index { get; set; }
        // Count - how many matches before parsing
        public int Count { get; set; }
        // Length of word
        public int Length { get; }

        public Match(int index, int count, int length)
        {
            this.Index = index;
            this.Count = count;
            this.Length = length;
        }
    }
}