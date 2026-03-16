using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoMaths;

using Toy_Synthesizer.Game.Synthesizer.Backend;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.BuiltinAudioEffects.Delays
{
    public abstract class BaseDelay : IAudioEffect, IAudioEffectCommandReceiver
    {
        public const float DEFAULT_DELAY_SECONDS = 0.5f;
        public const float DEFAULT_FEEDBACK_LEVEL = 0.5f;
        public const float DEFAULT_LEVEL = 1f;

        public const float MIN_DELAY_SECONDS = 0f;
        public const float MAX_DELAY_SECONDS = 30f;

        public const float MIN_FEEDBACK = 0f;
        public const float MAX_FEEDBACK = 1f;

        public const float MIN_LEVEL = 0f;
        public const float MAX_LEVEL = 1f;

        public static readonly NumberRange<float> DelayRange;
        public static readonly NumberRange<float> FeedbackLevelRange;
        public static readonly NumberRange<float> LevelRange;

        static BaseDelay()
        {
            DelayRange = NumberRange<float>.From(MIN_DELAY_SECONDS, MAX_DELAY_SECONDS);

            FeedbackLevelRange = NumberRange<float>.From(MIN_FEEDBACK, MAX_FEEDBACK);

            LevelRange = NumberRange<float>.From(MIN_LEVEL, MAX_LEVEL);
        }

        private readonly DSP dsp;

        private float[] delayBuffer;

        private int delayWriteIndex = 0;

        private float delaySeconds;
        private float feedbackLevel;
        private float level;

        public float DelaySeconds
        {
            get => delaySeconds;
        }

        public float FeedbackLevel
        {
            get => feedbackLevel;
        }

        public float Level
        {
            get => level;
        }

        public int DelaySampleCount
        {
            get => (int)(DelaySeconds * dsp.SampleRate);
        }

        public BaseDelay(DSP dsp)
        {
            this.dsp = dsp;

            delaySeconds = DEFAULT_DELAY_SECONDS;

            feedbackLevel = DEFAULT_FEEDBACK_LEVEL;

            level = DEFAULT_LEVEL;

            ResizeDelayBuffer(MAX_DELAY_SECONDS);

            delayWriteIndex = 0;
        }

        public void Apply(Span<float> buffer)
        {
            int delaySamples = DelaySampleCount;

            for (int index = 0; index < buffer.Length; index++)
            {
                float input = buffer[index];

                float delayedSample = GetSample(input, delayBuffer, delayWriteIndex, delaySamples);

                float feedbackMix = input + (delayedSample * FeedbackLevel);

                delayBuffer[delayWriteIndex] = feedbackMix;

                buffer[index] = input + (delayedSample * Level);

                delayWriteIndex = (delayWriteIndex + 1) % delayBuffer.Length;
            }
        }

        protected abstract float GetSample(float sample, float[] delayBuffer, int writeIndex, int delaySamples);

        public void SendCommands(ReadOnlySpan<AudioEffectCommand> commands)
        {
            for (int index = 0; index < commands.Length; index++)
            {
                SendCommand(in commands[index]);
            }
        }

        public void SendCommand(ref readonly AudioEffectCommand command)
        {
            if (command.CommandID == (int)CommonDelayCommandType.SetDelaySeconds)
            {
                this.delaySeconds = DelayRange.Clamp(command.ValueStorage.Read<float>());
            }
            else if (command.CommandID == (int)CommonDelayCommandType.SetFeedbackLevel)
            {
                this.feedbackLevel = FeedbackLevelRange.Clamp(command.ValueStorage.Read<float>());
            }
            else if (command.CommandID == (int)CommonDelayCommandType.SetLevel)
            {
                this.level = LevelRange.Clamp(command.ValueStorage.Read<float>());
            }
            else
            {
                SendCommandInternal(in command);
            }
        }

        protected virtual void SendCommandInternal(ref readonly AudioEffectCommand command)
        {

        }

        private void ResizeDelayBuffer(float delaySeconds)
        {
            int size = (int)(delaySeconds * dsp.SampleRate);

            if (delayBuffer is null)
            {
                delayBuffer = new float[size];
            }
            else
            {
                Array.Resize(ref delayBuffer, size);
            }
        }
    }
}
