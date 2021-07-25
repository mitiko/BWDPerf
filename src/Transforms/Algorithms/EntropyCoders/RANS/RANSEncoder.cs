using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.EntropyCoders.RANS.Entities;
using BWDPerf.Transforms.Modeling;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.RANS
{
    public class RANSEncoder<InputSymbol, PredictionSymbol> : ICoder<ReadOnlyMemory<InputSymbol>, byte>
    {
        public IAlphabet<PredictionSymbol> Alphabet { get; }
        public IModel Model { get; }
        public IQuantizer Quantizer { get; }
        public IBlockConverter<InputSymbol, PredictionSymbol> Converter { get; }

        public RANSEncoder(
            IAlphabet<PredictionSymbol> alphabet,
            IModel model,
            IQuantizer quantizer,
            IBlockConverter<InputSymbol,PredictionSymbol> converter)
        {
            this.Alphabet = alphabet;
            this.Model = model;
            this.Quantizer = quantizer;
            this.Converter = converter;
        }

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<ReadOnlyMemory<InputSymbol>> input)
        {
            await foreach (var inputBlock in input)
            {
                this.Converter.Load(inputBlock);
                var len = this.Converter.GetConvertedLength();
                var predictionBlock = this.Converter.Convert();

                var ransSymbols = new RANSSymbol[len];
                int tenPercent = len / 10, i = 0;
                int logM = this.Quantizer.Accuracy;

                // Predict stream forwards
                foreach (var predictionSymbol in predictionBlock)
                {
                    var pred = this.Model.Predict(); pred.Normalize();
                    var symbolIndex = this.Alphabet[predictionSymbol];
                    ransSymbols[i] = this.Quantizer.Encode(symbolIndex, pred);
                    this.Model.Update(symbolIndex);

                    if (++i % tenPercent == 0) Console.WriteLine($"[RANS] Predicted {10 * i / tenPercent}%");
                }

                // Assume a compression ratio of at least 0.5 to not resize the stack often
                var stream = new Stack<byte>(capacity: inputBlock.Length / 2);
                uint state = RANS._L;

                // Encode the stream backwards
                Console.WriteLine("[RANS] Started coding");
                for (i = len - 1; i >= 0 ; i--)
                {
                    Debug.Assert(RANS._L <= state, "State was under bound [L, bL)");
                    Debug.Assert(state < (RANS._L << RANS._logB), "State was over bound [L, bl)");

                    var ransSymbol = ransSymbols[i];
                    RANS.RenormalizeEncode(ref state, ransSymbol, logM, stream);
                    RANS.Encode(ref state, ransSymbol, logM);

                    if (i % tenPercent == 0) Console.WriteLine($"[RANS] Encoded {100 - 10 * i / tenPercent}%");
                }
                Console.WriteLine($"[RANS] Ended coding");

                // Output the encoder's ending state as it is the beginning state of the decoder
                // There are optimizations for using log(state) bits, but for now 32 bits is ok
                foreach (var @byte in BitConverter.GetBytes(state))
                    yield return @byte;

                // Using a stack reverses the direction. This way the decoder gets bytes in the forward direction
                while (stream.Count > 0)
                    yield return stream.Pop();
            }
        }
    }
}