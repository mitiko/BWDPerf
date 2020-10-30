using System.Collections.Generic;

namespace BWDPerf.Interfaces
{
    public interface ICoder<FromSymbol, ToSymbol>
    {
        IAsyncEnumerable<ToSymbol> Encode(IAsyncEnumerable<FromSymbol> input);
    }
}