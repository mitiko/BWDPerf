using System.Collections.Generic;

namespace BWDPerf.Interfaces
{
    public interface IDecoder<FromSymbol, ToSymbol>
    {
        IAsyncEnumerable<ToSymbol> Decode(IAsyncEnumerable<FromSymbol> input);
    }
}