using System.Collections.Generic;
using System.Threading.Tasks;

namespace BWDPerf.Interfaces
{
    public interface IDualSerializer<SymbolA, SymbolB>
    {
        Task Complete(IAsyncEnumerable<(SymbolA first, SymbolB second)> input);
    }
}