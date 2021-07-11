using System;
using System.Collections.Generic;
using System.Diagnostics;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Modeling.Submodels
{
    public class UShortOrder3 : IModel
    {
        public Dictionary<ulong, OccurenceDictionary<ushort>> Counts { get; set; } = new();
        public ulong State { get; set; }
        public int N { get; }
        private readonly ulong _mask = (1 << 48) - 1;

        public UShortOrder3(int symbolCount)
        {
            this.N = symbolCount;
            if (symbolCount > 65536) throw new NotImplementedException("UShortOrder4 model supports only ushort alphabets [0-65535]");
        }

        public Prediction Predict()
        {
            if (this.Counts.ContainsKey(this.State))
            {
                var p = new Prediction(this.N);
                var oc = this.Counts[this.State];
                foreach (var count in oc)
                    p[count.Key] = count.Value;
                return p;
            }
            else return new Prediction(this.N);
        }

        public void Update(int symbolIndex)
        {
            Debug.Assert(symbolIndex <= ushort.MaxValue, "Alphabet has more symbols than reported");

            var s = (ushort) symbolIndex;
            if (!this.Counts.ContainsKey(this.State)) this.Counts[this.State] = new();
            this.Counts[this.State].Add(s);
            this.State <<= 16;
            this.State |= s;
            this.State &= _mask;
        }
    }
}