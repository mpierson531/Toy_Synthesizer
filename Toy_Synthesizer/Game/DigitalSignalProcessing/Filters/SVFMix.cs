using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.Filters
{
    public struct SVFMix
    {
        public const double DEFAULT_LOW_MIX = 1.0;
        public const double DEFAULT_HIGH_MIX = 1.0;
        public const double DEFAULT_BAND_MIX = 1.0;

        public static readonly SVFMix Default = new SVFMix(1.0, 1.0, 1.0);

        public static SVFMix LowPass()
        {
            return new SVFMix(low: 1.0, high: 0.0, band: 0.0);
        }

        public static SVFMix HighPass()
        {
            return new SVFMix(low: 0.0, high: 1.0, band: 0.0);
        }

        public static SVFMix Notch()
        {
            return new SVFMix(low: 1.0, high: 1.0, band: 0.0);
        }

        public static SVFMix BandPass()
        {
            return new SVFMix(low: 0.0, high: 0.0, band: 1.0);
        }

        public static SVFMix NoPass()
        {
            return new SVFMix();
        }

        public double Low;
        public double High;
        public double Band;
        
        public SVFMix(double low, double high, double band)
        {
            this.Low = low;
            this.High = high;
            this.Band = band;
        }
    }
}
