using System.Collections.Generic;

namespace BWDPerf.Interfaces
{
    public interface ISource
    {
        IAsyncEnumerable<byte> Fetch();
    }
}