using System;

using Toy_Synthesizer.Game.CommonUtils.RawValueStorage;
using Toy_Synthesizer.Game.Synthesizer.Backend;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing
{
    public static class SynthesizerCommands
    {
        // Voice management commands

        public static AudioSourceCommand AddVoice(Voice voice)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.AddVoice, objectValue: voice);
        }

        public static AudioSourceCommand RemoveVoice(Voice voice)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.RemoveVoice, objectValue: voice);
        }

        public static AudioSourceCommand VoiceOn(Voice voice)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.VoiceOn, objectValue: voice);
        }

        public static AudioSourceCommand VoiceOff(Voice voice)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.VoiceOff, objectValue: voice);
        }

        public static AudioSourceCommand ForEachVoiceAction(Action<Voice> action)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.ForEachVoiceAction, objectValue: action);
        }

        // Center frequency

        public static AudioSourceCommand SetVoiceCenterFrequency(Voice voice, double frequency)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_CenterFrequency, valueStorage: RawValueStorage_64B.From(frequency), objectValue: voice);
        }

        // Name 

        public static AudioSourceCommand SetVoiceName(Voice voice, string name)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_Name, objectValue: voice, objectValue2: name);
        }

        // Mix

        public static AudioSourceCommand SetVoiceMix(Voice voice, double mix)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_Mix, valueStorage: RawValueStorage_64B.From(mix), objectValue: voice);
        }

        // Voice ADSR commands

        public static AudioSourceCommand SetVoiceAttack(Voice voice, double attack)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_Attack, valueStorage: RawValueStorage_64B.From(attack), objectValue: voice);
        }

        public static AudioSourceCommand SetVoiceDecay(Voice voice, double decay)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_Decay, valueStorage: RawValueStorage_64B.From(decay), objectValue: voice);
        }

        public static AudioSourceCommand SetVoiceSustain(Voice voice, double sustain)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_Sustain, valueStorage: RawValueStorage_64B.From(sustain), objectValue: voice);
        }

        public static AudioSourceCommand SetVoiceRelease(Voice voice, double release)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_Release, valueStorage: RawValueStorage_64B.From(release), objectValue: voice);
        }

        // Voice LPF commands

        public static AudioSourceCommand SetVoiceLPFBaseCutoff(Voice voice, double cutoff)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_LPFBaseCutoff, valueStorage: RawValueStorage_64B.From(cutoff), objectValue: voice);
        }

        public static AudioSourceCommand SetVoiceLPFResonance(Voice voice, double resonance)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_LPF_Resonance, valueStorage: RawValueStorage_64B.From(resonance), objectValue: voice);
        }

        public static AudioSourceCommand SetVoiceLPFAttack(Voice voice, double attack)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_LPF_Attack, valueStorage: RawValueStorage_64B.From(attack), objectValue: voice);
        }

        public static AudioSourceCommand SetVoiceLPFDecay(Voice voice, double decay)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_LPF_Decay, valueStorage: RawValueStorage_64B.From(decay), objectValue: voice);
        }

        public static AudioSourceCommand SetVoiceLPFSustain(Voice voice, double sustain)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_LPF_Sustain, valueStorage: RawValueStorage_64B.From(sustain), objectValue: voice);
        }

        public static AudioSourceCommand SetVoiceLPFRelease(Voice voice, double release)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_LPF_Release, valueStorage: RawValueStorage_64B.From(release), objectValue: voice);
        }

        public static AudioSourceCommand SetVoiceLPFAdsrAmount(Voice voice, double amount)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_LPF_ADSR_Amount, valueStorage: RawValueStorage_64B.From(amount), objectValue: voice);
        }

        // Voice oscillator management commands

        public static AudioSourceCommand AddVoiceOscillator(Voice voice, Oscillator oscillator)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.Voice_AddOscillator, objectValue: voice, objectValue2: oscillator);
        }

        public static AudioSourceCommand RemoveVoiceOscillator(Voice voice, Oscillator oscillator)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.Voice_RemoveOscillator, objectValue: voice, objectValue2: oscillator);
        }

        public static AudioSourceCommand ForEachVoiceOscillatorAction(Voice voice, Action<Oscillator> oscillator)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.Voice_ForEachOscillator, objectValue: voice, objectValue2: oscillator);
        }

        // Oscillator value commands

        public static AudioSourceCommand SetVoiceOscillatorAmplitude(Oscillator oscillator, double amplitude)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_Oscillator_Amplitude, valueStorage: RawValueStorage_64B.From(amplitude), objectValue: oscillator);
        }

        public static AudioSourceCommand SetVoiceOscillatorWaveformType(Oscillator oscillator, WaveformType waveformType)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_Oscillator_WaveformType, valueStorage: RawValueStorage_64B.From(waveformType), objectValue: oscillator);
        }

        public static AudioSourceCommand SetVoiceOscillatorDetuneCents(Oscillator oscillator, double detuneCents)
        {
            return AudioSourceCommand.Create((int)SynthesizerCommandType.SetVoice_Oscillator_DetuneCents, valueStorage: RawValueStorage_64B.From(detuneCents), objectValue: oscillator);
        }
    }
}