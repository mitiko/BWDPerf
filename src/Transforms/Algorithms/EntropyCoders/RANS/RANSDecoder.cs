using System;
using System.Collections.Generic;
using System.Diagnostics;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.EntropyCoders.RANS.Entities;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.RANS
{
    public class RANSDecoder<PredictionSymbol, OutputSymbol> : IDecoder<byte, OutputSymbol>
    {
        public IAlphabet<PredictionSymbol> Alphabet { get; }
        public IModel Model { get; }
        public IQuantizer Quantizer { get; }
        public IConverter<PredictionSymbol, OutputSymbol> Converter { get; set; }

        public RANSDecoder(
            IAlphabet<PredictionSymbol> alphabet,
            IModel model,
            IQuantizer quantizer,
            IConverter<PredictionSymbol, OutputSymbol> converter)
        {
            this.Alphabet = alphabet;
            this.Model = model;
            this.Quantizer = quantizer;
            this.Converter = converter;
        }

        public async IAsyncEnumerable<OutputSymbol> Decode(IAsyncEnumerable<byte> input)
        {
            var enumerator = input.GetAsyncEnumerator();
            var byteQueue = new Queue<byte>(capacity: 8);
            int logM = this.Quantizer.Accuracy;
            uint mask = (uint) (1 << logM) - 1;
            uint state = await ByteStreamHelper.GetUInt32Async(enumerator);

            while (true)
            {
                Debug.Assert(RANS._L <= state, "State was under bound [L, bL)");
                Debug.Assert(state < (RANS._L << RANS._logB), "State was over bound [L, bl)");

                // Predict and decode symbol
                var prediction = this.Model.Predict(); prediction.Normalize();
                var cdfRange = state & mask;
                var (symbolIndex, cdf, freq) = this.Quantizer.Decode(cdfRange, prediction);

                // Update state and update model
                RANS.Decode(ref state, new RANSSymbol(cdf, freq), logM, mask);
                this.Model.Update(symbolIndex);

                // Write symbol to the (output) stream
                var predictionSymbol = this.Alphabet[symbolIndex];
                this.Converter.Buffer(predictionSymbol);
                foreach (var outputSymbol in this.Converter.Convert())
                    yield return outputSymbol;

                // Check if EOF and read bytes from the (input) stream to write into the queue
                while (byteQueue.Count <= 8 && await enumerator.MoveNextAsync())
                    byteQueue.Enqueue(enumerator.Current);

                // Renormalize and stop decoding if there's nothing more to decode (the byte queue is empty and the state is L)
                if (RANS.RenormalizeDecode(ref state, byteQueue)) break;
            }

            // If there's prediction symbols left in the converter's buffer, the conversion was incomplete
            // Warn the user about it
            if (this.Converter.Flush())
                Console.WriteLine("[RANSDecoder] Warning: the conversion was incomplete");
            // TODO: Use yellow when we add a logging system
        }
    }
}