namespace BWDPerf.Common.Entities
{
    // Represents a word reference in a buffer, using first occurence location and length
    public struct Word
    {
        public int Location { get; }
        public int Length { get; }

        public Word(int location, int length)
        {
            this.Location = location;
            this.Length = length;
        }
    }
}