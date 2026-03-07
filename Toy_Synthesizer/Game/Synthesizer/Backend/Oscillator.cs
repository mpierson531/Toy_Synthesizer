using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoMaths;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    public class Oscillator
    {
        public double CenterFrequency;
        public double Phase;
        public double Amplitude;
        public WaveformType WaveformType;
        public double DetuneCents;

        public double NextSample(int sampleRate, double pitchShiftRatio)
        {
            double freq = CenterFrequency * pitchShiftRatio * Math.Pow(2.0, DetuneCents / 1200.0);

            double value = Amplitude * WaveProcessing.Process(WaveformType, Phase);

            Phase += 2.0 * Math.PI * freq / sampleRate;

            if (Phase >= 2.0 * Math.PI)
            {
                Phase -= 2.0 * Math.PI;
            }

            return value;
        }

        public Oscillator(double centerFrequency, double amplitude, WaveformType waveformType, double detuneCents = 0.0)
        {
            CenterFrequency = centerFrequency;
            Phase = 0.0;
            Amplitude = amplitude;
            WaveformType = waveformType;
            DetuneCents = detuneCents;
        }

        public Oscillator()
        {

        }

        public void Reset()
        {
            Phase = 0.0;
        }
    }
}
