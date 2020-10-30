using System.Collections.Generic;
using System.Threading.Tasks;

namespace BWDPerf.Interfaces
{
    public interface ISerializer
    {
        Task Complete(IAsyncEnumerable<byte> input);
    }
}