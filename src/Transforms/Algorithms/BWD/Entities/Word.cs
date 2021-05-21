namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    // Represents a word reference in a buffer, using first occurence location and length
    public struct Word
    {
        public int Location { get; }
        public int Length { get; }
        public static Word Empty => new Word(-1, -1);

        public Word(int location, int length)
        {
            this.Location = location;
            this.Length = length;
        }
    }
}