using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toy_Synthesizer.Game.Synthesizer.Backend;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.BuiltinAudioEffects.Delays
{
    public sealed class SimpleFeedbackDelay : BaseDelay
    {
        public SimpleFeedbackDelay(DSP dsp) : base(dsp) { }

        protected override float GetSample(float sample, float[] delayBuffer, int writeIndex, int delaySamples)
        {
            int readIndex = writeIndex - delaySamples;

            if (readIndex < 0)
            {
                readIndex += delayBuffer.Length;
            }

            return delayBuffer[readIndex];
        }
    }
}
