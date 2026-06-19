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
    public class ParameterizedOscillator
    {
        private double phase = 0.0;

        public double NextSample(int sampleRate, double pitchShiftRatio,
                                 double amplitude,
                                 WaveformType waveformType,
                                 double detuneCents,
                                 double centerFrequency)
        {
            double output = Oscillator.Sample(amplitude: amplitude,
                                              waveformType: waveformType,
                                              detuneCents: detuneCents,
                                              centerFrequency: centerFrequency,
                                              phase: ref phase,
                                              sampleRate: sampleRate,
                                              pitchShiftRatio: pitchShiftRatio);

            return output;
        }

        public ParameterizedOscillator()
        {

        }

        public void Reset()
        {
            phase = 0.0;
        }
    }
}