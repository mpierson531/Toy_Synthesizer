using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    public static class WaveProcessing
    {

        public static double Process(WaveformType waveformType, double phase)
        {
            return waveformType switch
            {
                WaveformType.Sine => SineWave(phase),

                WaveformType.Square => SquareWave(phase),

                WaveformType.Sawtooth => SawtoothWave(phase),

                WaveformType.Triangle => TriangleWave(phase),

                WaveformType.Pulse => PulseWave(phase),

                WaveformType.InversePulse => PulseWave_Ceiling(phase),

                _ => throw new InvalidOperationException("Invalid WaveformType: " + waveformType)
            };
        }

        public static double SineWave(double phase)
        {
            return Math.Sin(phase);
        }

        public static double SquareWave(double phase)
        {
            return Math.Sin(phase) >= 0 ? 1.0 : -1.0;
        }

        public static double SawtoothWave(double phase)
        {
            return (phase / Math.PI) - 1.0;
        }

        public static double TriangleWave(double phase)
        {
            return 2.0 * Math.Abs(2.0 * (phase / (2.0 * Math.PI) - Math.Floor(phase / (2.0 * Math.PI) + 0.5))) - 1.0;
        }

        // dutyCycle is between 0.0 and 1.0 (0.5 for 50%).
        public static double PulseWave(double phase, float dutyCycle = 0.5f)
        {
            double fractional = phase - Math.Floor(phase); // Fractional of phase.

            return (fractional < dutyCycle) ? 1.0 : -1.0;
        }

        // dutyCycle is between 0.0 and 1.0 (0.5 for 50%).
        public static double PulseWave_Ceiling(double phase, float dutyCycle = 0.5f)
        {
            return PulseWave(phase, dutyCycle) == 1.0 ? -1.0 : 1.0;
        }
    }
}
