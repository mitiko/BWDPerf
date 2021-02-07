using System;
using System.Collections.Generic;

namespace BWDPerf.Interfaces
{
    public interface ISource<Symbol>
    {
        IAsyncEnumerable<Symbol> Fetch();
    }
}