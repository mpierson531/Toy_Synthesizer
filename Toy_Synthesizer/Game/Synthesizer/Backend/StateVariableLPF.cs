using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    public class StateVariableLPF : ICopyable
    {
        private int sampleRate;

        private double low;
        private double band;

        private double f;
        private double q;

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

            cutoff = Math.Clamp(cutoff, 20.0, sampleRate * 0.45);

            Cutoff = cutoff;
            Resonance = Math.Clamp(resonance, 0.0, 1.0);

            f = 2.0 * Math.Sin(Math.PI * cutoff / sampleRate);

            q = 2.0 * (1.0 - Resonance);
        }

        public double Process(double input)
        {
            double high = input - low - q * band;

            band += f * high;
            low += f * band;

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
