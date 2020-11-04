namespace BWDPerf.Common.Entities
{
    // Represents an index in a dictionary.
    // Used to encode a blocks of integers with variable bits per symbol.
    public struct DictionaryIndex
    {
        public int Index { get; set; }
        public int BitsToUse { get; }

        public DictionaryIndex(int index, int bitsToUse)
        {
            this.Index = index;
            this.BitsToUse = bitsToUse;
        }
    }
}