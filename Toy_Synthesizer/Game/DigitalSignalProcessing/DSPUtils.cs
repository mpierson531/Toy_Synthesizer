using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing
{
    public static class DSPUtils
    {
        public static double ShiftOctaveBy(double frequency, double octaveAmount)
        {
            return frequency * Math.Pow(2.0, octaveAmount);
        }

        public static void WriteMonoToStereo(float[] buffer, int offset, int index, double sample)
        {
            buffer[offset + index] = (float)sample;
            buffer[offset + index + 1] = (float)sample;
        }

        public static void WriteMonoToStereo(Span<float> buffer, int offset, int index, double sample)
        {
            buffer[offset + index] = (float)sample;
            buffer[offset + index + 1] = (float)sample;
        }

        public static void WriteStereoToStereo(float[] buffer, int offset, int index, double leftSample, double rightSample)
        {
            buffer[offset + index] = (float)leftSample;
            buffer[offset + index + 1] = (float)rightSample;
        }

        public static void WriteStereoToStereo(Span<float> buffer, int offset, int index, double leftSample, double rightSample)
        {
            buffer[offset + index] = (float)leftSample;
            buffer[offset + index + 1] = (float)rightSample;
        }
    }
}
