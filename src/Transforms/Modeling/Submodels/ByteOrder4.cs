using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class ByteOrder4 : IModel
    {
        public Dictionary<uint, OccurenceDictionary<byte>> Counts { get; set; } = new();
        public uint State { get; set; }

        public ByteOrder4(int symbolCount)
        {
            if (symbolCount > 256) throw new NotImplementedException("ByteOrder4 model supports only byte alphabets [0-255]");
        }

        public Prediction Predict()
        {
            if (this.Counts.ContainsKey(this.State))
            {
                var p = new Prediction(256);
                var oc = this.Counts[this.State];
                foreach (var count in oc)
                    p[count.Key] = count.Value;
                return p;
            }
            else return new Prediction(256);
        }

        public void Update(int symbolIndex)
        {
            var b = (byte) symbolIndex;
            if (!this.Counts.ContainsKey(this.State)) this.Counts[this.State] = new();
            this.Counts[this.State].Add(b);
            this.State <<= 8;
            this.State |= b;
        }
    }
}