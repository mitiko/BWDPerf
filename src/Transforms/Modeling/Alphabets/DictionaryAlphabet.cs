using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Alphabets
{
    public class DictionaryAlphabet : IAlphabet<ushort>
    {
        private ushort[] Symbols { get; }

        public ushort this[int index] => this.Symbols[index];
        public int this[ushort symbol] => symbol;
        public int Length => this.Symbols.Length;

        public DictionaryAlphabet(ushort[] symbols) => this.Symbols = symbols;
    }
}