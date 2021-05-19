using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class UShortOrder2 : IModel
    {
        public Dictionary<uint, OccurenceDictionary<ushort>> Counts { get; set; } = new();
        public uint State { get; set; }
        public int SymbolCount { get; }

        public UShortOrder2(int symbolCount)
        {
            this.SymbolCount = symbolCount;
            if (symbolCount > 65536) throw new NotImplementedException("UShortOrder2 model supports only ushort alphabets [0-65535]");
        }

        public Prediction Predict()
        {
            if (this.Counts.ContainsKey(this.State))
            {
                var p = new Prediction(this.SymbolCount);
                var oc = this.Counts[this.State];
                foreach (var count in oc)
                    p[count.Key] = count.Value;
                return p;
            }
            else return new Prediction(this.SymbolCount);
        }

        public void Update(int symbolIndex)
        {
            var s = (ushort) symbolIndex;
            if (!this.Counts.ContainsKey(this.State)) this.Counts[this.State] = new();
            this.Counts[this.State].Add(s);
            this.State <<= 16;
            this.State |= s;
        }
    }
}