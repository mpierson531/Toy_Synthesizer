using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoMaths;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.Filters
{
    public struct SVFOutput
    {
        public double Low;
        public double High;
        public double Band;

        public readonly double Notch
        {
            get => Low + High;
        }

        public readonly double Mix(SVFMix mixParams)
        {
            return (Low * mixParams.Low) + (High * mixParams.High) + (Band * mixParams.Band);
        }

        public readonly double Mix(ref readonly SVFMix mixParams)
        {
            return (Low * mixParams.Low) + (High * mixParams.High) + (Band * mixParams.Band);
        }
    }
}
