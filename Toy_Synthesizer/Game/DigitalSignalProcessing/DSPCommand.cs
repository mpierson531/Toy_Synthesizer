using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing
{
    public readonly struct DSPCommand
    {
        public static DSPCommand AddAudioSource(IAudioSource audioSource)
        {
            return new DSPCommand(type: DSPCommandType.AddAudioSource, audioSource: audioSource);
        }

        public static DSPCommand RemoveAudioSource(IAudioSource audioSource)
        {
            return new DSPCommand(type: DSPCommandType.RemoveAudioSource, audioSource: audioSource);
        }

        public static DSPCommand AddAudioEffect(IAudioEffect audioEffect)
        {
            return new DSPCommand(type: DSPCommandType.AddAudioEffect, audioEffect: audioEffect);
        }

        public static DSPCommand RemoveAudioEffect(IAudioEffect audioEffect)
        {
            return new DSPCommand(type: DSPCommandType.RemoveAudioEffect, audioEffect: audioEffect);
        }

        public static DSPCommand BeginRecordingAudio()
        {
            return new DSPCommand(type: DSPCommandType.BeginRecordingAudio);
        }

        public static DSPCommand StopRecordingAudio()
        {
            return new DSPCommand(type: DSPCommandType.StopRecordingAudio);
        }

        public static DSPCommand ClearRecordedAudio()
        {
            return new DSPCommand(type: DSPCommandType.ClearRecordedAudio);
        }

        public static DSPCommand SendAudioSourceCommand(IAudioSource audioSource, AudioSourceCommand audioSourceCommand)
        {
            return new DSPCommand(type: DSPCommandType.SendAudioSourceCommand, audioSource: audioSource, audioSourceCommand: audioSourceCommand);
        }

        public static DSPCommand SendAudioEffectCommand(IAudioEffect audioEffect, AudioEffectCommand audioEffectCommand)
        {
            return new DSPCommand(type: DSPCommandType.SendAudioEffectCommand, audioEffect: audioEffect, audioEffectCommand: audioEffectCommand);
        }

        public readonly DSPCommandType Type;

        public readonly IAudioSource AudioSource;
        public readonly IAudioEffect AudioEffect;

        public readonly AudioSourceCommand AudioSourceCommand;
        public readonly AudioEffectCommand AudioEffectCommand;

        private DSPCommand(DSPCommandType type, 
                           IAudioSource audioSource = null,
                           IAudioEffect audioEffect = null,
                           AudioSourceCommand audioSourceCommand = default,
                           AudioEffectCommand audioEffectCommand = default)
        {
            this.Type = type;

            this.AudioSource = audioSource;
            this.AudioEffect = audioEffect;

            this.AudioSourceCommand = audioSourceCommand;
            this.AudioEffectCommand = audioEffectCommand;
        }
    }
}
