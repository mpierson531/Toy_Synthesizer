using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils.Collections;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    public class Oscillator
    {
        public double CenterFrequency;
        public double Amplitude;
        public WaveformType WaveformType;
        public double DetuneCents;

        private double phase = 0.0;

        public double NextSample(int sampleRate, double pitchShiftRatio)
        {
            double output = Sample(amplitude: Amplitude,
                                   waveformType: WaveformType,
                                   detuneCents: DetuneCents,
                                   centerFrequency: CenterFrequency,
                                   phase: ref phase,
                                   sampleRate: sampleRate,
                                   pitchShiftRatio: pitchShiftRatio);

            return output;
        }

        public Oscillator(double centerFrequency, double amplitude, WaveformType waveformType, double detuneCents = 0.0)
        {
            CenterFrequency = centerFrequency;
            Amplitude = amplitude;
            WaveformType = waveformType;
            DetuneCents = detuneCents;
            phase = 0.0;
        }

        public Oscillator()
        {

        }

        public void Reset()
        {
            phase = 0.0;
        }

        public static double Sample(double amplitude, 
                                    WaveformType waveformType, 
                                    double detuneCents, 
                                    double centerFrequency, 
                                    ref double phase, 
                                    int sampleRate, 
                                    double pitchShiftRatio)
        {
            double freq = centerFrequency * pitchShiftRatio * Math.Pow(2.0, detuneCents / 1200.0);

            double output = amplitude * WaveProcessing.Process(waveformType, phase);

            phase += 2.0 * Math.PI * freq / sampleRate;

            if (phase >= 2.0 * Math.PI)
            {
                phase -= 2.0 * Math.PI;
            }

            return output;
        }
    }
}
