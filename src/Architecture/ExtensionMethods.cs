using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Architecture
{
    public static class ExtensionMethods
    {
        public static IAsyncEnumerable<ToSymbol> ToCoder<ToSymbol>(this ISource source, ICoder<byte, ToSymbol> coder) =>
            coder.Encode(source.Fetch());

        public static IAsyncEnumerable<ToSymbol> ToCoder<FromSymbol, ToSymbol>(this IAsyncEnumerable<FromSymbol> pipeline, ICoder<FromSymbol, ToSymbol> coder) =>
            coder.Encode(pipeline);
    
        public static async Task Serialize(this IAsyncEnumerable<byte> pipeline, ISerializer serializer) =>
            await serializer.Complete(pipeline);
    }
}