using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.Synthesizer.Backend.BuiltinAudioEffects
{
    public sealed class SimpleRepeatDelay : IAudioEffect
    {
        private const float DELAY_SECONDS = 0.5f; // half-second delay
        private const int REPEATS = 8;
        private const float DECAY = 0.5f; // decay per repeat
        private const float LEVEL = 1f;

        private readonly PolyphonicSynthesizer synthesizer;
        private readonly float[] delayBuffer;
        private int writeIndex = 0;

        public SimpleRepeatDelay(PolyphonicSynthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
            int delayBufferSize = (int)(DELAY_SECONDS * synthesizer.SampleRate);
            delayBuffer = new float[delayBufferSize * REPEATS]; // buffer for multiple repeats
        }

        public void Apply(Span<float> buffer)
        {
            int sampleRate = synthesizer.SampleRate;
            int delaySamples = (int)(DELAY_SECONDS * sampleRate);

            for (int index = 0; index < buffer.Length; index++)
            {
                float delayedSample = 0f;

                for (int r = 1; r <= REPEATS; r++)
                {
                    int readIndex = writeIndex - r * delaySamples;

                    if (readIndex < 0)
                    {
                        readIndex += delayBuffer.Length;
                    }

                    delayedSample += delayBuffer[readIndex] * (float)Math.Pow(DECAY, r);
                }

                delayBuffer[writeIndex] = buffer[index];

                buffer[index] += delayedSample * LEVEL;

                writeIndex = (writeIndex + 1) % delayBuffer.Length;
            }
        }
    }
}
