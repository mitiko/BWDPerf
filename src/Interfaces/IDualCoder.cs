using System.Collections.Generic;

namespace BWDPerf.Interfaces
{
    public interface IDualCoder<FromSymbol, ToSymbolA, ToSymbolB>
    {
        IAsyncEnumerable<(ToSymbolA, ToSymbolB)> Encode(IAsyncEnumerable<FromSymbol> input);
    }
}