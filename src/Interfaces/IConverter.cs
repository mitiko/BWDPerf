using System.Collections.Generic;

namespace BWDPerf.Interfaces
{
    public interface IConverter<PredictionSymbol, OutputSymbol>
    {
        public void Buffer(PredictionSymbol predictionSymbol);
        public IEnumerable<OutputSymbol> Convert();
        public bool Flush();
    }
}