using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.Filters
{
    // TODO: implement commands.
    public class StateVariableLPF : ICopyable
    {
        private int sampleRate;

        private double low;
        private double band;

        private double cutoffCoefficient;
        private double resonanceDamping;

        public double Cutoff;
        public double Resonance;

        public StateVariableLPF(double cutoff, double resonance, int sampleRate)
        {
            Set(cutoff, resonance, sampleRate);
        }

        public StateVariableLPF()
        {

        }

        public void Set(double cutoff, double resonance, int sampleRate)
        {
            this.sampleRate = sampleRate;

            cutoff = Math.Clamp(cutoff, 20.0, sampleRate * 0.25);

            Cutoff = cutoff;
            Resonance = resonance;

            cutoffCoefficient = 2.0 * Math.Sin(Math.PI * cutoff / sampleRate);

            resonanceDamping = Math.Max(0.05, 1.0 - Resonance);
        }

        public double Process(double input)
        {
            low += cutoffCoefficient * band;

            double high = input - low - resonanceDamping * band;

            band += cutoffCoefficient * high;

            GeoDebug.BreakIf(double.IsNaN(low) || double.IsInfinity(low) || double.IsNaN(band) || double.IsInfinity(band));

            return low;
        }

        public void Reset()
        {
            low = 0;
            band = 0;
        }

        public StateVariableLPF Copy(bool deepCopy = false)
        {
            return new StateVariableLPF(Cutoff, Resonance, sampleRate);
        }

        object ICopyable.Copy(bool deepCopy)
        {
            return Copy(deepCopy);
        }
    }

}
