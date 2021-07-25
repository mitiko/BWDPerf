using System;
using System.Collections.Generic;

namespace BWDPerf.Interfaces
{
    public interface IBlockConverter<FromSymbol, ToSymbol>
    {
        public void Load(ReadOnlyMemory<FromSymbol> block);
        public int GetConvertedLength();
        public IEnumerable<ToSymbol> Convert();
    }
}