using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoMaths;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.Filters
{
    // TODO: implement commands.
    public class SVF : ICopyable
    {
        private int sampleRate;

        private double low;
        private double band;

        private double cutoffCoefficient;
        private double resonanceDamping;

        private double cutoff;

        public double Resonance;

        // Gain only affects the output returned by Process. It does not affect state at all.

        public double Cutoff
        {
            get => cutoff;

            set
            {
                if (cutoff == value)
                {
                    return;
                }

                cutoff = value;

                SetCutoffCoefficient();
            }
        }

        public double Gain;

        public int SampleRate
        {
            get => sampleRate;

            set
            {
                if (sampleRate == value)
                {
                    return;
                }

                sampleRate = value;

                SetCutoffCoefficient();
            }
        }

        public SVF(double cutoff, double resonance, double gain, int sampleRate)
        {
            Set(cutoff, resonance, gain, sampleRate);
        }

        public SVF()
        {

        }

        public void Set(double cutoff, double resonance, double gain, int sampleRate)
        {
            this.sampleRate = sampleRate;

            this.cutoff = cutoff;

            this.Resonance = resonance;

            this.Gain = gain;

            SetCutoffCoefficient();

            resonanceDamping = Math.Max(0.05, 1.0 - Resonance);
        }

        public SVFOutput Process(double input)
        {
            low += cutoffCoefficient * band;

            double high = input - low - resonanceDamping * band;

            band += cutoffCoefficient * high;

            GeoDebug.BreakIf(GeoMath.IsNaNOrInfinity(low));
            GeoDebug.BreakIf(GeoMath.IsNaNOrInfinity(high));
            GeoDebug.BreakIf(GeoMath.IsNaNOrInfinity(band));

            return new SVFOutput
            {
                Low = low * Gain,
                High = high * Gain,
                Band = band * Gain
            };
        }

        private void SetCutoffCoefficient()
        {
            cutoffCoefficient = 2.0 * Math.Sin(Math.PI * Cutoff / sampleRate);

            GeoDebug.BreakIf(GeoMath.IsNaNOrInfinity(cutoffCoefficient));
        }

        public void Reset()
        {
            low = 0;
            band = 0;
        }

        public SVF Copy(bool deepCopy = false)
        {
            return new SVF(Cutoff, Resonance, Gain, sampleRate);
        }

        object ICopyable.Copy(bool deepCopy)
        {
            return Copy(deepCopy);
        }
    }

}
