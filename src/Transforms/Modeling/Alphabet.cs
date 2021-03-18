using System.Collections.Generic;

namespace BWDPerf.Transforms.Modeling
{
    public class Alphabet<TSymbol>
    {
        private TSymbol[] Symbols { get; }
        private Dictionary<TSymbol, int> Positions { get; } = new();

        public TSymbol this[int index] => this.Symbols[index];
        public int this[TSymbol symbol] => this.Positions[symbol];
        public int Length => this.Symbols.Length;

        public Alphabet(TSymbol[] symbols)
        {
            this.Symbols = symbols;
            for (int i = 0; i < symbols.Length; i++)
                this.Positions.Add(symbols[i], i);
        }

        public static Alphabet<byte> ForText()
        {
            var symbols = new byte[256];
            for (int i = 0; i < symbols.Length; i++)
                symbols[i] = (byte) i;
            return new Alphabet<byte>(symbols);
        }
    }
}