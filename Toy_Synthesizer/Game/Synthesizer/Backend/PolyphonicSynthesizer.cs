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
using Toy_Synthesizer.Game.DigitalSignalProcessing.Filters;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Console;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    public class PolyphonicSynthesizer : IAudioSource, IAudioSourceCommandReceiver
    {
        public const double DEFAULT_OSCILLATOR_AMPLITUDE = 0.75;
        public const WaveformType DEFAULT_OSCILLATOR_WAVEFORM_TYPE = WaveformType.Sine;

        public const double MIN_CENTER_FREQUENCY = 0.0;
        public const double MAX_CENTER_FREQUENCY = 32000.0;

        public const double DEFAULT_FILTER_BASE_CUTOFF = 5000;
        public const double DEFAULT_FILTER_ADSR_AMOUNT = 0.0;
        public const double DEFAULT_FILTER_RESONANCE = 0.25;
        public const double DEFAULT_FILTER_GAIN = 1.0;

        public const double DEFAULT_MIX = 1.0;

        public const double MIN_ATTACK = 0.0;
        public const double MAX_ATTACK = 3600.0;

        public const double MIN_DECAY = 0.0;
        public const double MAX_DECAY = 3600.0;

        public const double MIN_SUSTAIN = 0.0;
        public const double MAX_SUSTAIN = 1.0;

        public const double MIN_RELEASE = 0.0;
        public const double MAX_RELEASE = 3600.0;

        public const double MIN_FILTER_BASE_CUTOFF = 0.0;
        public const double MAX_FILTER_BASE_CUTOFF = 32000.0;

        public const double MIN_FILTER_ADSR_AMOUNT = 0.0;
        public const double MAX_FILTER_ADSR_AMOUNT = 20000.0;

        public const double MIN_FILTER_RESONANCE = 0.0;
        public const double MAX_FILTER_RESONANCE = 1.0;

        public const double MIN_FILTER_GAIN = 0.0;
        public const double MAX_FILTER_GAIN = 10.0;

        public const double MIN_FILTER_MIX = 0.0;
        public const double MAX_FILTER_MIX = 1.0;

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

        public static readonly NumberRange<double> Filter_BaseCutoffRange;
        public static readonly NumberRange<double> Filter_AdsrAmountRange;
        public static readonly NumberRange<double> Filter_ResonanceRange;
        public static readonly NumberRange<double> Filter_GainRange;
        public static readonly NumberRange<double> Filter_MixRange;

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

            Filter_BaseCutoffRange = NumberRange<double>.From(MIN_FILTER_BASE_CUTOFF, MAX_FILTER_BASE_CUTOFF);
            Filter_AdsrAmountRange = NumberRange<double>.From(MIN_FILTER_ADSR_AMOUNT, MAX_FILTER_ADSR_AMOUNT);
            Filter_ResonanceRange = NumberRange<double>.From(MIN_FILTER_RESONANCE, MAX_FILTER_RESONANCE);
            Filter_GainRange = NumberRange<double>.From(MIN_FILTER_GAIN, MAX_FILTER_GAIN);
            Filter_MixRange = NumberRange<double>.From(MIN_FILTER_MIX, MAX_FILTER_MIX);

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

            ValidateVoice(voice);

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

            if (voice.Filter_Adsr is null)
            {
                voice.Filter_Adsr = voice.Adsr.Copy(deepCopy: true);
            }
            else
            {
                voice.Filter_Adsr.SampleRate = sampleRate;
            }

            if ((voice.Oscillators is null || voice.Oscillators.IsEmpty) && addDefaultOscillatorsIfEmpty)
            {
                if (voice.Oscillators is null)
                {
                    voice.Oscillators = new ViewableList<Oscillator>(capacity: 10);
                }

                Oscillator defaultOscillator0 = CreateDefaultOscillator(voice.CenterFrequency, amplitude: DEFAULT_OSCILLATOR_AMPLITUDE, waveformType: WaveformType.Square);
                Oscillator defaultOscillator1 = CreateDefaultOscillator(voice.CenterFrequency, amplitude: DEFAULT_OSCILLATOR_AMPLITUDE, waveformType: WaveformType.Sine);

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

            ValidateOscillator(oscillator, oscillator.CenterFrequency);

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
            voice.Filter_Adsr.NoteOn();
        }

        // Initiates the ending of voice.
        private void VoiceOff(Voice voice)
        {
            voice.Adsr.NoteOff();
            voice.Filter_Adsr.NoteOff();
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
                    double filterAdsrResult = voice.Filter_Adsr.NextSample();

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

                    if (voice.Filter is not null)
                    {
                        double cutoff = voice.Filter_BaseCutoff + filterAdsrResult * voice.Filter_AdsrAmount;

                        cutoff = GeoMath.Clamp(cutoff, 0.0, 44100.0);

                        voice.Filter.Set(cutoff, voice.Filter.Resonance, voice.Filter.Gain, sampleRate);

                        SVFOutput filterOutput = voice.Filter.Process(voiceSample);

                        double filterMix = filterOutput.Mix(voice.FilterMix.GetValueOrDefault(SVFMix.Default));

                        voiceSample = filterMix;
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

                case SynthesizerCommandType.SetVoice_Filter:
                    SetVoiceFilter((Voice)command.ObjectValue, (SVF)command.ObjectValue2);
                    break;

                case SynthesizerCommandType.SetVoice_Filter_BaseCutoff:
                    SetVoiceFilterBaseCutoff((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Filter_Resonance:
                    SetVoiceFilterResonance((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Filter_Gain:
                    SetVoiceFilterGain((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Filter_Mix:
                    SetVoiceFilterMix((Voice)command.ObjectValue, command.ValueStorage.Read<UnmanagedNullable<SVFMix>>());
                    break;

                case SynthesizerCommandType.SetVoice_Filter_Mix_Low:
                    SetVoiceFilterMixLow((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Filter_Mix_High:
                    SetVoiceFilterMixHigh((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Filter_Mix_Band:
                    SetVoiceFilterMixBand((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Filter_Attack:
                    SetVoiceFilterAttack((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;
                case SynthesizerCommandType.SetVoice_Filter_Decay:
                    SetVoiceFilterDecay((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;
                case SynthesizerCommandType.SetVoice_Filter_Sustain:
                    SetVoiceFilterSustain((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;
                case SynthesizerCommandType.SetVoice_Filter_Release:
                    SetVoiceFilterRelease((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
                    break;

                case SynthesizerCommandType.SetVoice_Filter_ADSR_Amount:
                    SetVoiceFilterAdsrAmount((Voice)command.ObjectValue, command.ValueStorage.Read<double>());
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

        public SVF CreateDefaultLPF()
        {
            return CreateDefaultLPF(SampleRate);
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
            voice.Mix = ClampMix(mix);
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

        private static void SetVoiceFilter(Voice voice, SVF filter)
        {
            ValidateFilter(filter);

            voice.Filter = filter;
        }

        private static void SetVoiceFilterBaseCutoff(Voice voice, double baseCutoff)
        {
            voice.Filter_BaseCutoff = Filter_BaseCutoffRange.Clamp(baseCutoff);
        }

        private static void SetVoiceFilterResonance(Voice voice, double resonance)
        {
            if (voice.Filter is null)
            {
                return;
            }

            voice.Filter.Resonance = ClampFilterResonance(resonance);
        }

        private static void SetVoiceFilterGain(Voice voice, double gain)
        {
            if (voice.Filter is null)
            {
                return;
            }

            voice.Filter.Gain = ClampFilterGain(gain);
        }

        private static void SetVoiceFilterMix(Voice voice, UnmanagedNullable<SVFMix> mix)
        {
            if (mix.HasValue)
            {
                mix = ClampSVFMix(mix.Value);
            }

            voice.FilterMix = mix;
        }

        private static void SetVoiceFilterMixLow(Voice voice, double low)
        {
            if (voice.FilterMix.HasValue)
            {
                low = ClampFilterMix(low);

                voice.FilterMix.RefWritableUnsafe().Low = low;
            }
        }

        private static void SetVoiceFilterMixHigh(Voice voice, double high)
        {
            if (voice.FilterMix.HasValue)
            {
                high = ClampFilterMix(high);

                voice.FilterMix.RefWritableUnsafe().High = high;
            }
        }

        private static void SetVoiceFilterMixBand(Voice voice, double band)
        {
            if (voice.FilterMix.HasValue)
            {
                band = ClampFilterMix(band);

                voice.FilterMix.RefWritableUnsafe().Band = band;
            }
        }

        private static void SetVoiceFilterAttack(Voice voice, double attack)
        {
            if (voice.Filter_Adsr is null)
            {
                return;
            }

            voice.Filter_Adsr.AttackSeconds = AttackRange.Clamp(attack);
        }

        private static void SetVoiceFilterDecay(Voice voice, double decay)
        {
            if (voice.Filter_Adsr is null)
            {
                return;
            }

            voice.Filter_Adsr.DecaySeconds = DecayRange.Clamp(decay);
        }

        private static void SetVoiceFilterSustain(Voice voice, double sustain)
        {
            if (voice.Filter_Adsr is null)
            {
                return;
            }

            voice.Filter_Adsr.SustainLevel = SustainRange.Clamp(sustain);
        }

        private static void SetVoiceFilterRelease(Voice voice, double release)
        {
            if (voice.Filter_Adsr is null)
            {
                return;
            }

            voice.Filter_Adsr.ReleaseSeconds = ReleaseRange.Clamp(release);
        }

        // TODO: Implement range.
        private static void SetVoiceFilterAdsrAmount(Voice voice, double amount)
        {
            voice.Filter_AdsrAmount = Filter_AdsrAmountRange.Clamp(amount);
        }

        private static void AddVoiceOscillator(Voice voice, Oscillator oscillator)
        {
            if (voice.Oscillators is null)
            {
                voice.Oscillators = new ViewableList<Oscillator>(capacity: 10);
            }

            ValidateOscillator(oscillator, voice.CenterFrequency);

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
                waveformType = DEFAULT_OSCILLATOR_WAVEFORM_TYPE;

                // Not sure if it should throw an exception.

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
            voice.Filter_Adsr?.Reset();
            voice.Filter?.Reset();

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

        // This validates everything in a voice and ensures everything is in a valid range.
        // This will also validate oscillators and set the voice's oscillators' center frequency.
        public static void ValidateVoice(Voice voice)
        {
            if (voice is null)
            {
                return;
            }

            SetVoiceMix(voice, voice.Mix);

            SetVoiceCenterFrequency(voice, voice.CenterFrequency);

            ValidateAdsrEnvelope(voice.Adsr);

            ValidateVoiceFilter(voice);

            ValidateOscillators(voice.Oscillators, voice.CenterFrequency);
        }

        public static void ValidateAdsrEnvelope(AdsrEnvelope adsr)
        {
            if (adsr is null)
            {
                return;
            }

            ClampAdsrEnvelope(adsr);
        }

        public static void ClampAdsrEnvelope(AdsrEnvelope adsr)
        {
            adsr.AttackSeconds = AttackRange.Clamp(adsr.AttackSeconds);
            adsr.DecaySeconds = AttackRange.Clamp(adsr.DecaySeconds);
            adsr.SustainLevel = SustainRange.Clamp(adsr.SustainLevel);
            adsr.ReleaseSeconds = ReleaseRange.Clamp(adsr.ReleaseSeconds);
        }

        // Validates Voice.Filter_BaseCutoff, everything in Voice.Filter, Voice.Filter_Adsr, Voice.Filter_AdsrAmount, and Voice.FilterMix.
        public static void ValidateVoiceFilter(Voice voice)
        {
            if (voice is null)
            {
                return;
            }

            voice.Filter_BaseCutoff = Filter_BaseCutoffRange.Clamp(voice.Filter_BaseCutoff);

            // The filter's cutoff itself is set dynamically because of the filter's ADSR,
            // and the base cutoff is stored in Voice,
            // so I don't really need to clamp the filter's cutoff here.

            if (voice.Filter is not null)
            {
                ValidateFilter(voice.Filter);
            }

            ValidateVoiceFilterMix(voice);

            ValidateAdsrEnvelope(voice.Filter_Adsr);

            voice.Filter_AdsrAmount = Filter_AdsrAmountRange.Clamp(voice.Filter_AdsrAmount);
        }

        public static void ValidateFilter(SVF filter)
        {
            filter.Resonance = ClampFilterResonance(filter.Resonance);

            filter.Gain = ClampFilterGain(filter.Gain);
        }

        public static void ValidateVoiceFilterMix(Voice voice)
        {
            if (!voice.FilterMix.HasValue)
            {
                return;
            }

            voice.FilterMix = ClampSVFMix(voice.FilterMix.Value);
        }

        public static SVFMix ClampSVFMix(SVFMix mix)
        {
            ClampSVFMix(ref mix);

            return mix;
        }

        public static void ClampSVFMix(ref SVFMix mix)
        {
            mix.Low = ClampFilterMix(mix.Low);
            mix.High = ClampFilterMix(mix.High);
            mix.Band = ClampFilterMix(mix.Band);
        }

        public static double ClampMix(double mix)
        {
            return MixRange.Clamp(mix);
        }

        public static double ClampFilterResonance(double gain)
        {
            return Filter_ResonanceRange.Clamp(gain);
        }

        public static double ClampFilterGain(double gain)
        {
            return Filter_GainRange.Clamp(gain);
        }

        public static double ClampFilterMix(double mix)
        {
            return Filter_MixRange.Clamp(mix);
        }

        // Also sets the center frequency.
        public static void ValidateOscillators(ViewableList<Oscillator> oscillators, double centerFrequency)
        {
            if (oscillators is null)
            {
                return;
            }

            for (int index = 0; index < oscillators.Count; index++)
            {
                ValidateOscillator(oscillators[index], centerFrequency);
            }
        }

        // Also sets the center frequency.
        public static void ValidateOscillator(Oscillator oscillator, double centerFrequency)
        {
            oscillator.CenterFrequency = CenterFrequencyRange.Clamp(centerFrequency);

            oscillator.Amplitude = OscillatorAmplitudeRange.Clamp(oscillator.Amplitude);

            if (!SupportedOscillatorWaveformTypes.Contains(oscillator.WaveformType))
            {
                oscillator.WaveformType = DEFAULT_OSCILLATOR_WAVEFORM_TYPE;
            }

            oscillator.DetuneCents = OscillatorDetuneCentsRange.Clamp(oscillator.DetuneCents);
        }

        public static Oscillator CreateDefaultOscillator(double centerFrequency, 
                                                         double amplitude = DEFAULT_OSCILLATOR_AMPLITUDE,
                                                         WaveformType waveformType = DEFAULT_OSCILLATOR_WAVEFORM_TYPE)
        {
            return new Oscillator
            {
                CenterFrequency = centerFrequency,
                Amplitude = amplitude,
                WaveformType = waveformType,
                DetuneCents = 0
            };
        }

        public static SVF CreateDefaultLPF(int sampleRate)
        {
            return new SVF(cutoff: DEFAULT_FILTER_BASE_CUTOFF, 
                           resonance: DEFAULT_FILTER_RESONANCE, 
                           gain: DEFAULT_FILTER_GAIN,
                           sampleRate: sampleRate);
        }
    }
}
