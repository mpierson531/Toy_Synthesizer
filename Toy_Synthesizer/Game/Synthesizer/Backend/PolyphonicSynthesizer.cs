using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.DigitalSignalProcessing;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Console;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    // TODO: Look into Voice/StateVariableLPF default cutoffs more.
    public class PolyphonicSynthesizer : IAudioSource, IAudioSourceCommandReceiver
    {
        public const double DEFAULT_AMPLITUDE = 0.5;
        public const WaveformType DEFAULT_WAVEFORM_TYPE = WaveformType.Square;

        public const double MIN_CENTER_FREQUENCY = 0.0;
        public const double MAX_CENTER_FREQUENCY = 32000.0;

        public const double DEFAULT_LPFBASE_CUTOFF = 800.0;
        public const double DEFAULT_LPF_ADSR_AMOUNT = 3000.0;
        public const double DEFAULT_MIX = 1.0;

        public const double MIN_ATTACK = 0.0;
        public const double MAX_ATTACK = 3600.0;

        public const double MIN_DECAY = 0.0;
        public const double MAX_DECAY = 3600.0;

        public const double MIN_SUSTAIN = 0.0;
        public const double MAX_SUSTAIN = 1.0;

        public const double MIN_RELEASE = 0.0;
        public const double MAX_RELEASE = 3600.0;

        public const double MIN_LPF_BASE_CUTOFF = 0.0;
        public const double MAX_LPF_BASE_CUTOFF = 32000.0;

        public const double MIN_LPF_RESONANCE = 0.0;
        public const double MAX_LPF_RESONANCE = 1.0;

        public const double MIN_OSCILLATOR_AMPLITUDE = 0.0;
        public const double MAX_OSCILLATOR_AMPLITUDE = 1.0;

        public const double MIN_OSCILLATOR_DETUNE_CENTS = -50.0;
        public const double MAX_OSCILLATOR_DETUNE_CENTS = 50.0;

        public const double MIN_MIX = 0.0;
        public const double MAX_MIX = 1.0;

        public static readonly NumberRange<double> CenterFrequencyRange;

        public static readonly NumberRange<double> AttackRange;
        public static readonly NumberRange<double> DecayRange;
        public static readonly NumberRange<double> SustainRange;
        public static readonly NumberRange<double> ReleaseRange;

        public static readonly NumberRange<double> LPF_BaseCutoffRange;
        public static readonly NumberRange<double> LPF_ResonanceRange;

        public static readonly NumberRange<double> OscillatorAmplitudeRange;
        public static readonly ImmutableArray<WaveformType> SupportedOscillatorWaveformTypes;
        public static readonly NumberRange<double> OscillatorDetuneCentsRange;

        public static readonly NumberRange<double> MixRange;

        static PolyphonicSynthesizer()
        {
            CenterFrequencyRange = NumberRange<double>.From(MIN_CENTER_FREQUENCY, MAX_CENTER_FREQUENCY);

            AttackRange = NumberRange<double>.From(MIN_ATTACK, MAX_ATTACK);
            DecayRange = NumberRange<double>.From(MIN_DECAY, MAX_DECAY);
            SustainRange = NumberRange<double>.From(MIN_SUSTAIN, MAX_SUSTAIN);
            ReleaseRange = NumberRange<double>.From(MIN_RELEASE, MAX_RELEASE);

            LPF_BaseCutoffRange = NumberRange<double>.From(MIN_LPF_BASE_CUTOFF, MAX_LPF_BASE_CUTOFF);
            LPF_ResonanceRange = NumberRange<double>.From(MIN_LPF_RESONANCE, MAX_LPF_RESONANCE);

            OscillatorAmplitudeRange = NumberRange<double>.From(MIN_OSCILLATOR_AMPLITUDE, MAX_OSCILLATOR_AMPLITUDE);

            SupportedOscillatorWaveformTypes = new ImmutableArray<WaveformType>
            (
                WaveformType.Sine,
                WaveformType.Triangle,
                WaveformType.Square,
                WaveformType.Sawtooth,

                WaveformType.Pulse,
                WaveformType.InversePulse
            );

            OscillatorDetuneCentsRange = NumberRange<double>.From(MIN_OSCILLATOR_DETUNE_CENTS, MAX_OSCILLATOR_DETUNE_CENTS);

            MixRange = NumberRange<double>.From(MIN_MIX, MAX_MIX);
        }

        private readonly ViewableList<Voice> voicesMasterList = new ViewableList<Voice>(500);
        private readonly ViewableList<Voice> onVoices = new ViewableList<Voice>(500);
        private readonly ViewableList<Voice> offVoices = new ViewableList<Voice>(500);
        private readonly ViewableList<ViewableList<ParameterizedOscillator>> globalVoiceOscillatorVoiceBindings = new ViewableList<ViewableList<ParameterizedOscillator>>(500);

        private readonly ViewableList<Oscillator> globalVoiceOscillators = new ViewableList<Oscillator>(500);

        // This is for use with the RemoveVoice method.
        private readonly ViewableList<int> onVoicesIndicesToRemoveOnNextRead;

        private readonly int sampleRate;

        //private readonly object lockObject = new object();

        private double globalVoicePitchShiftRatio;

        public int SampleRate
        {
            get => sampleRate;
        }

        public double GlobalVoicePitchShiftRatio
        {
            get => Interlocked.CompareExchange(ref globalVoicePitchShiftRatio, 0.0, 0.0);
            set => Interlocked.Exchange(ref globalVoicePitchShiftRatio, value);
        }

        public double GlobalVoicePitchShiftSemitones
        {
            get => ChromaticScaleUtils.PitchRatioToSemitones(GlobalVoicePitchShiftRatio);
            set => GlobalVoicePitchShiftRatio = ChromaticScaleUtils.SemitonesToPitchRatio(value);
        }

        public event Action<PolyphonicSynthesizer, Voice> OnVoiceAdded;
        public event Action<PolyphonicSynthesizer, Voice> OnVoiceRemoved;

        public PolyphonicSynthesizer(int sampleRate)
        {
            this.sampleRate = sampleRate;

            onVoicesIndicesToRemoveOnNextRead = new ViewableList<int>();

            GlobalVoicePitchShiftSemitones = 0.0;
        }

        private void ForEachVoice(Action<Voice> action)
        {
            for (int index = 0; index < offVoices.Count; index++)
            {
                action(offVoices.GetUnchecked(index));
            }

            for (int index = 0; index < onVoices.Count; index++)
            {
                action(onVoices.GetUnchecked(index));
            }
        }

        private void ForEachVoiceOscillator(Voice voice, Action<Oscillator> action)
        {
            for (int index = 0; index < voice.Oscillators.Count; index++)
            {
                action(voice.Oscillators.GetUnchecked(index));
            }
        }

        private void AddVoice(Voice voice,
                              bool on = false,
                              bool addDefaultOscillatorsIfEmpty = true)
        {
            if (ContainsVoice(voice))
            {
                throw new InvalidOperationException("voice already exists.");
            }

            voicesMasterList.Add(voice);

            ViewableList<ParameterizedOscillator> globalVoiceOscillatorBinding = new ViewableList<ParameterizedOscillator>(globalVoiceOscillators.Count);

            globalVoiceOscillatorVoiceBindings.Add(globalVoiceOscillatorBinding);

            for (int globalVoiceOscillatorIndex = 0; globalVoiceOscillatorIndex < globalVoiceOscillators.Count; globalVoiceOscillatorIndex++)
            {
                globalVoiceOscillatorBinding.Add(new ParameterizedOscillator());
            }

            if (voice.Adsr is null)
            {
                voice.Adsr = new AdsrEnvelope(sampleRate);
            }
            else
            {
                voice.Adsr.SampleRate = sampleRate;
            }

            if (voice.LPF_Adsr is null)
            {
                voice.LPF_Adsr = voice.Adsr.Copy(deepCopy: true);
            }
            else
            {
                voice.LPF_Adsr.SampleRate = sampleRate;
            }

            if ((voice.Oscillators is null || voice.Oscillators.IsEmpty) && addDefaultOscillatorsIfEmpty)
            {
                if (voice.Oscillators is null)
                {
                    voice.Oscillators = new ViewableList<Oscillator>(capacity: 10);
                }

                Oscillator defaultOscillator0 = CreateDefaultOscillator(voice.CenterFrequency);
                Oscillator defaultOscillator1 = CreateDefaultOscillator(voice.CenterFrequency);

                voice.Oscillators.Add(defaultOscillator0);
                voice.Oscillators.Add(defaultOscillator1);
            }

            if (on)
            {
                VoiceOn(voice, throwIfNonExistent: false);
            }
            else
            {
                AddOffVoice(voice, removeFromOn: false);
            }

            if (OnVoiceAdded is not null)
            {
                OnVoiceAdded(this, voice);
            }
        }

        private bool RemoveVoice(Voice voice,
                                 bool allowReleaseIfOn = true)
        {
            int voiceMasterIndex = voicesMasterList.IndexOf(voice);

            if (voiceMasterIndex == -1)
            {
                GeoDebug.Assert(!offVoices.Contains(voice) && !onVoices.Contains(voice));

                return false;
            }

            int offIndex = offVoices.IndexOf(voice);

            if (!allowReleaseIfOn || offIndex != -1)
            {
                GeoDebug.Assert(onVoices.Contains(voice) || offIndex != -1);

                if (offIndex != -1)
                {
                    GeoDebug.Assert(!onVoices.Contains(voice));

                    offVoices.RemoveAt(offIndex);
                }
                else
                {
                    int onIndex = onVoices.IndexOf(voice);

                    GeoDebug.Assert(onIndex != -1);

                    onVoices.RemoveAt(onIndex);
                }

                ResetVoice(voice);

                voicesMasterList.RemoveAt(voiceMasterIndex);
                globalVoiceOscillatorVoiceBindings.RemoveAt(voiceMasterIndex);
            }
            else
            {
                int onIndex = onVoices.IndexOf(voice);

                GeoDebug.Assert(onIndex != -1);

                VoiceOff(voice);

                onVoicesIndicesToRemoveOnNextRead.Add(onIndex);
            }

            return true;
        }

        private void AddGlobalVoiceOscillator(Oscillator oscillator)
        {
            GeoDebug.Assert(!globalVoiceOscillators.Contains(oscillator));

            globalVoiceOscillators.Add(oscillator);

            for (int index = 0; index < globalVoiceOscillatorVoiceBindings.Count; index++)
            {
                globalVoiceOscillatorVoiceBindings.GetUnchecked(index).Add(new ParameterizedOscillator());
            }
        }

        private void RemoveGlobalVoiceOscillator(Oscillator oscillator)
        {
            GeoDebug.Assert(globalVoiceOscillators.Contains(oscillator));

            int oscillatorIndex = globalVoiceOscillators.IndexOf(oscillator);

            globalVoiceOscillators.RemoveAt(oscillatorIndex);

            for (int index = 0; index < globalVoiceOscillatorVoiceBindings.Count; index++)
            {
                globalVoiceOscillatorVoiceBindings.GetUnchecked(index).RemoveAt(oscillatorIndex);
            }
        }

        // Begins voice.
        private void VoiceOn(Voice voice, bool throwIfNonExistent)
        {
            AddOnVoice(voice, removeFromOff: true, throwIfNonExistent: throwIfNonExistent);

            voice.Adsr.NoteOn();
            voice.LPF_Adsr.NoteOn();
        }

        // Initiates the ending of voice.
        private void VoiceOff(Voice voice)
        {
            voice.Adsr.NoteOff();
            voice.LPF_Adsr.NoteOff();
        }

        private void AddOnVoice(Voice voice, bool removeFromOff, bool throwIfNonExistent)
        {
            bool isAlreadyOn = onVoices.Contains(voice);

            if (throwIfNonExistent && !isAlreadyOn && !offVoices.Contains(voice))
            {
                throw new InvalidOperationException("voice was non-existent prior to turning it on.");
            }

            if (!isAlreadyOn)
            {
                onVoices.Add(voice);
            }

            voice.IsOff = false;

            if (removeFromOff)
            {
                offVoices.Remove(voice);
            }
        }

        private void AddOffVoice(Voice voice, bool removeFromOn)
        {
            GeoDebug.Assert(!offVoices.Contains(voice));

            offVoices.Add(voice);

            if (removeFromOn)
            {
                onVoices.Remove(voice);
            }
        }

        public bool ContainsVoice(Voice voice)
        {
#if DEBUG
            bool masterListContains = voicesMasterList.Contains(voice);

            if (masterListContains)
            {
                bool offVoicesContains = offVoices.Contains(voice);
                bool onVoicesContains = onVoices.Contains(voice);

                GeoDebug.Assert(offVoicesContains || onVoicesContains);

                if (offVoicesContains)
                {
                    GeoDebug.Assert(!onVoicesContains);
                }
                else if (onVoicesContains)
                {
                    GeoDebug.Assert(!offVoicesContains);
                }

                return true;
            }

            return false;

#else
            return voicesMasterList.Contains(voice);
#endif
        }

        int IAudioSource.Read(Span<float> buffer)
        {
            double globalVoicePitchShiftRatio = GlobalVoicePitchShiftRatio;

            Synthesize(buffer, globalVoicePitchShiftRatio);

            return buffer.Length;
        }

        private void Synthesize(Span<float> buffer, double globalVoicePitchShiftRatio)
        {
            if (!onVoicesIndicesToRemoveOnNextRead.IsEmpty)
            {
                for (int i = 0; i < onVoicesIndicesToRemoveOnNextRead.Count; i++)
                {
                    int index = onVoicesIndicesToRemoveOnNextRead.GetUnchecked(i);

                    Voice voice = onVoices.GetUnchecked(index);

                    GeoDebug.Assert(!offVoices.Contains(voice));

                    ResetVoice(voice);

                    onVoices.RemoveAt(index);

                    int voiceMasterIndex = voicesMasterList.IndexOf(voice);

                    voicesMasterList.RemoveAt(voiceMasterIndex);

                    globalVoiceOscillatorVoiceBindings.RemoveAt(voiceMasterIndex);
                }

                onVoicesIndicesToRemoveOnNextRead.Clear();
            }

            for (int bufferIndex = 0; bufferIndex < buffer.Length; bufferIndex += 2)
            {
                double sample = 0.0;

                for (int voiceIndex = onVoices.Count - 1; voiceIndex >= 0; voiceIndex--)
                {
                    Voice voice = onVoices.GetUnchecked(voiceIndex);

                    double ampAdsrResult = voice.Adsr.NextSample();
                    double filterAdsrResult = voice.LPF_Adsr.NextSample();

                    if (voice.Adsr.IsFinished)
                    {
                        ResetVoice(voice);

                        AddOffVoice(voice, removeFromOn: true);

                        continue;
                    }

                    double voiceSample = 0.0;

                    if (voice.Oscillators is not null && !voice.Oscillators.IsEmpty)
                    {
                        for (int oscillatorIndex = 0; oscillatorIndex < voice.Oscillators.Count; oscillatorIndex++)
                        {
                            voiceSample += voice.Oscillators.GetUnchecked(oscillatorIndex).NextSample(sampleRate, globalVoicePitchShiftRatio);
                        }
                    }

                    if (!globalVoiceOscillators.IsEmpty)
                    {
                        int voiceMasterIndex = voicesMasterList.IndexOf(voice);

                        ViewableList<ParameterizedOscillator> globalVoiceOscillatorsBinding = globalVoiceOscillatorVoiceBindings[voiceMasterIndex];

                        GeoDebug.Assert(!globalVoiceOscillatorsBinding.IsEmpty);

                        GeoDebug.Assert(globalVoiceOscillatorsBinding.Count == globalVoiceOscillators.Count);

                        for (int globalVoiceOscillatorIndex = 0; globalVoiceOscillatorIndex < globalVoiceOscillators.Count; globalVoiceOscillatorIndex++)
                        {
                            Oscillator template = globalVoiceOscillators[globalVoiceOscillatorIndex];
                            ParameterizedOscillator oscillatorPhaseState = globalVoiceOscillatorsBinding[globalVoiceOscillatorIndex];

                            voiceSample += oscillatorPhaseState.NextSample
                            (
                                sampleRate: sampleRate,
                                pitchShiftRatio: globalVoicePitchShiftRatio,
                                amplitude: template.Amplitude,
                                waveformType: template.WaveformType,
                                detuneCents: template.DetuneCents,
                                centerFrequency: voice.CenterFrequency
                            );
                        }
                    }

                    if (voice.LPF is not null)
                    {
                        double cutoff = voice.LPF_BaseCutoff + filterAdsrResult * voice.LPF_AdsrAmount;

                        voice.LPF.Set(cutoff, voice.LPF.Resonance, sampleRate);

                        voiceSample = voice.LPF.Process(voiceSample);
                    }

                    voiceSample *= voice.Mix;

                    sample += ampAdsrResult * voiceSample;
                }

                DSPUtils.WriteMonoToStereo(buffer, 0, bufferIndex, sample);
            }
        }

        public void SendCommands(ReadOnlySpan<AudioSourceCommand> commands)
        {
            for (int index = 0; index < commands.Length; index++)
            {
                SendCommand(in commands[index]);
            }
        }

        public void SendCommand(ref readonly AudioSourceCommand command)
        {
            if (command.CommandID < 0 || command.CommandID >= (int)SynthesizerCommandType.EndType)
            {
                throw InvalidCommandIDException(command.CommandID);
            }

            switch ((SynthesizerCommandType)command.CommandID)
            {
                case SynthesizerCommandType.None:
                case SynthesizerCommandType.EndType:
                    break;

                case SynthesizerCommandType.AddVoice:
                    AddVoice((Voice)command.ObjectValue, addDefaultOscillatorsIfEmpty: command.ValueStorage.Read<bool>());
                    break;

                case SynthesizerCommandType.RemoveVoice:
                    RemoveVoice((Voice)command.ObjectValue);
                    break;

                case SynthesizerCommandType.AddGlobalVoiceOscillator:
                    AddGlobalVoiceOscillator((Oscillator)command.ObjectValue);
                    break;

                case SynthesizerCommandType.RemoveGlobalVoiceOscillator:
                    RemoveGlobalVoiceOscillator((Oscillator)command.ObjectValue);
                    break;

                case SynthesizerCommandType.VoiceOn:
                    VoiceOn((Voice)command.ObjectValue, throwIfNonExistent: true);
                    break;

                case SynthesizerCommandType.VoiceOff:
                    VoiceOff((Voice)command.ObjectValue);
                    break;

                case SynthesizerCommandType.ForEachVoiceAction:
                    ForEachVoice((Action<Voice>)command.ObjectValue);
                    break;

                case SynthesizerCommandType.SetVoice_CenterFrequency:
                    SetVoiceCenterFrequency((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Name:
                    SetVoiceName((Voice)command.ObjectValue, (string)command.ObjectValue2);
                    break;

                case SynthesizerCommandType.SetVoice_Mix:
                    SetVoiceMix((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Attack:
                    SetVoiceAttack((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;
                case SynthesizerCommandType.SetVoice_Decay:
                    SetVoiceDecay((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;
                case SynthesizerCommandType.SetVoice_Sustain:
                    SetVoiceSustain((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;
                case SynthesizerCommandType.SetVoice_Release:
                    SetVoiceRelease((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_LPFBaseCutoff:
                    SetVoiceLPFBaseCutoff((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_LPF_Resonance:
                    SetVoiceLPFResonance((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_LPF_Attack:
                    SetVoiceLPFAttack((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;
                case SynthesizerCommandType.SetVoice_LPF_Decay:
                    SetVoiceLPFDecay((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;
                case SynthesizerCommandType.SetVoice_LPF_Sustain:
                    SetVoiceLPFSustain((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;
                case SynthesizerCommandType.SetVoice_LPF_Release:
                    SetVoiceLPFRelease((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_LPF_ADSR_Amount:
                    SetVoiceLPFAdsrAmount((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.Voice_AddOscillator:
                    AddVoiceOscillator((Voice)command.ObjectValue, (Oscillator)command.ObjectValue2);
                    break;

                case SynthesizerCommandType.Voice_RemoveOscillator:
                    RemoveVoiceOscillator((Voice)command.ObjectValue, (Oscillator)command.ObjectValue2);
                    break;

                case SynthesizerCommandType.Voice_ForEachOscillator:
                    ForEachVoiceOscillator((Voice)command.ObjectValue, (Action<Oscillator>)command.ObjectValue2);
                    break;

                case SynthesizerCommandType.SetVoice_Oscillator_Amplitude:
                    SetVoiceOscillatorAmplitude((Oscillator)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Oscillator_WaveformType:
                    SetVoiceOscillatorWaveformType((Oscillator)command.ObjectValue, command.ValueStorage.Read<WaveformType>());
                    break;

                case SynthesizerCommandType.SetVoice_Oscillator_DetuneCents:
                    SetVoiceOscillatorDetuneCents((Oscillator)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                default:
                    throw InvalidCommandIDException(command.CommandID);
            }
        }

        private static void SetVoiceCenterFrequency(Voice voice,  double frequency)
        {
            voice.CenterFrequency = CenterFrequencyRange.Clamp(frequency);
        }

        private static void SetVoiceName(Voice voice, string name)
        {
            voice.Name = name;
        }

        private static void SetVoiceMix(Voice voice, double mix)
        {
            voice.Mix = MixRange.Clamp(mix);
        }

        private static void SetVoiceAttack(Voice voice, double attack)
        {
            voice.Adsr.AttackSeconds = AttackRange.Clamp(attack);
        }

        private static void SetVoiceDecay(Voice voice, double decay)
        {
            voice.Adsr.DecaySeconds = DecayRange.Clamp(decay);
        }

        private static void SetVoiceSustain(Voice voice, double sustain)
        {
            voice.Adsr.SustainLevel = SustainRange.Clamp(sustain);
        }

        private static void SetVoiceRelease(Voice voice, double release)
        {
            voice.Adsr.ReleaseSeconds = ReleaseRange.Clamp(release);
        }

        private static void SetVoiceLPFBaseCutoff(Voice voice, double baseCutoff)
        {
            voice.LPF_BaseCutoff = LPF_BaseCutoffRange.Clamp(baseCutoff);
        }

        private static void SetVoiceLPFResonance(Voice voice, double resonance)
        {
            voice.LPF.Resonance = LPF_ResonanceRange.Clamp(resonance);
        }

        private static void SetVoiceLPFAttack(Voice voice, double attack)
        {
            voice.LPF_Adsr.AttackSeconds = AttackRange.Clamp(attack);
        }

        private static void SetVoiceLPFDecay(Voice voice, double decay)
        {
            voice.LPF_Adsr.DecaySeconds = DecayRange.Clamp(decay);
        }

        private static void SetVoiceLPFSustain(Voice voice, double sustain)
        {
            voice.LPF_Adsr.SustainLevel = SustainRange.Clamp(sustain);
        }

        private static void SetVoiceLPFRelease(Voice voice, double release)
        {
            voice.LPF_Adsr.ReleaseSeconds = ReleaseRange.Clamp(release);
        }

        // TODO: Implement range.
        private static void SetVoiceLPFAdsrAmount(Voice voice, double amount)
        {
            voice.LPF_AdsrAmount = amount;
        }

        private static void AddVoiceOscillator(Voice voice, Oscillator oscillator)
        {
            if (voice.Oscillators is null)
            {
                voice.Oscillators = new ViewableList<Oscillator>(capacity: 10);
            }

            voice.Oscillators.Add(oscillator);
        }

        private static void RemoveVoiceOscillator(Voice voice, Oscillator oscillator)
        {
            if (voice.Oscillators.Remove(oscillator))
            {
                oscillator.Reset();
            }
        }

        private static void SetVoiceOscillatorAmplitude(Oscillator oscillator, double amplitude)
        {
            oscillator.Amplitude = OscillatorAmplitudeRange.Clamp(amplitude);
        }

        private static void SetVoiceOscillatorWaveformType(Oscillator oscillator, WaveformType waveformType)
        {
            if (!SupportedOscillatorWaveformTypes.Contains(waveformType))
            {
                // Not sure if it should return or throw an exception.

                return;

                //throw new InvalidOperationException($"WaveformType for oscillators \"{waveformType}\" not supported.");
            }

            oscillator.WaveformType = waveformType;
        }

        private static void SetVoiceOscillatorDetuneCents(Oscillator oscillator, double cents)
        {
            oscillator.DetuneCents = OscillatorDetuneCentsRange.Clamp(cents);
        }

        private static InvalidOperationException InvalidCommandIDException(int commandID)
        {
            return new InvalidOperationException($"Invalid command ID: \"{commandID}\".");
        }

        private void ResetVoice(Voice voice)
        {
            voice.IsOff = true;

            voice.Adsr.Reset();

            ResetOscillators(voice);
        }

        private void ResetOscillators(Voice voice)
        {
            if (voice is not null && voice.Oscillators is not null && !voice.Oscillators.IsEmpty)
            {
                for (int oscillatorIndex = 0; oscillatorIndex < voice.Oscillators.Count; oscillatorIndex++)
                {
                    voice.Oscillators.GetUnchecked(oscillatorIndex).Reset();
                }
            }

            if (voice is not null && !globalVoiceOscillators.IsEmpty)
            {
                ViewableList<ParameterizedOscillator> globalVoiceOscillatorsBinding = globalVoiceOscillatorVoiceBindings[voicesMasterList.IndexOf(voice)];

                GeoDebug.Assert(!globalVoiceOscillatorsBinding.IsEmpty);

                for (int globalVoiceOscillatorBindingIndex = 0; globalVoiceOscillatorBindingIndex < globalVoiceOscillatorsBinding.Count; globalVoiceOscillatorBindingIndex++)
                {
                    globalVoiceOscillatorsBinding[globalVoiceOscillatorBindingIndex].Reset();
                }
            }
        }

        public static Oscillator CreateDefaultOscillator(double centerFrequency)
        {
            return new Oscillator
            {
                CenterFrequency = centerFrequency,
                Amplitude = DEFAULT_AMPLITUDE,
                WaveformType = DEFAULT_WAVEFORM_TYPE,
                DetuneCents = 0
            };
        }
    }
}
