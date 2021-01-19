using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Architecture
{
    public static class ExtensionMethods
    {
        public static IAsyncEnumerable<ToSymbol> ToCoder<Symbol, ToSymbol>(this ISource<Symbol> source, ICoder<Symbol, ToSymbol> coder) =>
            coder.Encode(source.Fetch());

        public static IAsyncEnumerable<ToSymbol> ToCoder<FromSymbol, ToSymbol>(this IAsyncEnumerable<FromSymbol> pipeline, ICoder<FromSymbol, ToSymbol> coder) =>
            coder.Encode(pipeline);

        public static IAsyncEnumerable<ToSymbol> ToDecoder<Symbol, ToSymbol>(this ISource<Symbol> source, IDecoder<Symbol, ToSymbol> decoder) =>
            decoder.Decode(source.Fetch());

        public static IAsyncEnumerable<ToSymbol> ToDecoder<FromSymbol, ToSymbol>(this IAsyncEnumerable<FromSymbol> pipeline, IDecoder<FromSymbol, ToSymbol> decoder) =>
            decoder.Decode(pipeline);

        public static async Task Serialize<Symbol>(this IAsyncEnumerable<Symbol> pipeline, ISerializer<Symbol> serializer) =>
            await serializer.Complete(pipeline);
    }
}