using System.Collections.Generic;
using System.Threading.Tasks;

namespace BWDPerf.Interfaces
{
    public interface ISerializer<Symbol>
    {
        Task Complete(IAsyncEnumerable<Symbol> input);
    }
}