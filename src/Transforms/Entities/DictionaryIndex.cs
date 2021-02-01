namespace BWDPerf.Transforms.Entities
{
    // Represents an index in a dictionary.
    // Used to encode a blocks of integers with variable bits per symbol.
    public struct DictionaryIndex
    {
        public int Index { get; }

        public DictionaryIndex(int index) =>
            this.Index = index;
    }
}