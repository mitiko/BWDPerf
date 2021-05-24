using System;
using System.Collections.Generic;
using System.Diagnostics;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class ByteOrder2 : IModel
    {
        public Dictionary<ushort, OccurenceDictionary<byte>> Counts { get; set; } = new();
        public ushort State { get; set; }
        public int N { get; }

        public ByteOrder2(int symbolCount)
        {
            this.N = symbolCount;
            if (symbolCount > 256) throw new NotImplementedException("ByteOrder2 model supports only byte alphabets [0-255]");
        }

        public Prediction Predict()
        {
            if (this.Counts.ContainsKey(this.State))
            {
                var p = new Prediction(N);
                var oc = this.Counts[this.State];
                foreach (var count in oc)
                    p[count.Key] = count.Value;
                return p;
            }
            else return new Prediction(N);
        }

        public void Update(int symbolIndex)
        {
            Debug.Assert(symbolIndex <= byte.MaxValue, "Alphabet has more symbols than reported");

            var b = (byte) symbolIndex;
            if (!this.Counts.ContainsKey(this.State)) this.Counts[this.State] = new();
            this.Counts[this.State].Add(b);
            this.State <<= 8;
            this.State |= b;
        }
    }
}