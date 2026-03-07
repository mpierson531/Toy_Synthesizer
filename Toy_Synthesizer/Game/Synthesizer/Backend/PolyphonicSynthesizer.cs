using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using NAudio.Wave;

using SharpDX.MediaFoundation;

using Toy_Synthesizer.Game.CommonUtils;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    public class PolyphonicSynthesizer : ISampleProvider
    {
        public const int DEFAULT_SAMPLE_RATE = 44100;

        public const double DEFAULT_AMPLITUDE = 0.25;
        public const WaveformType DEFAULT_WAVEFORM_TYPE = WaveformType.Square;

        public const double DEFAULT_GLOBAL_GAIN = 0.5;
        public const double DEFAULT_MASTER_VOLUME = 1.0;
        public const double DEFAULT_GLOBAL_PAN = 0.0;

        public const double MIN_GLOBAL_GAIN = 0.0;
        public const double MAX_GLOBAL_GAIN = 5.0;

        public const double MIN_MASTER_VOLUME = 0.0;
        public const double MAX_MASTER_VOLUME = 1.0;

        public const double MIN_GLOBAL_PAN = -1.0;
        public const double MAX_GLOBAL_PAN = 1.0;

        public const double MIN_OSCILLATOR_AMPLITUDE = 0.0;
        public const double MAX_OSCILLATOR_AMPLITUDE = 1.0;

        public static readonly NumberRange<double> GlobalGainRange;
        public static readonly NumberRange<double> MasterVolumeRange;
        public static readonly NumberRange<double> GlobalPanRange;
        public static readonly NumberRange<double> OscillatorAmplitudeRange;
        public static readonly ImmutableArray<WaveformType> SupportedWaveformTypes;

        static PolyphonicSynthesizer()
        {
            GlobalGainRange = NumberRange<double>.From(MIN_GLOBAL_GAIN, MAX_GLOBAL_GAIN);

            MasterVolumeRange = NumberRange<double>.From(MIN_MASTER_VOLUME, MAX_MASTER_VOLUME);

            GlobalPanRange = NumberRange<double>.From(MIN_GLOBAL_PAN, MAX_GLOBAL_PAN);

            OscillatorAmplitudeRange = NumberRange<double>.From(MIN_OSCILLATOR_AMPLITUDE, MAX_OSCILLATOR_AMPLITUDE);

            SupportedWaveformTypes = new ImmutableArray<WaveformType>
            (
                WaveformType.Sine,
                WaveformType.Triangle,
                WaveformType.Square,
                WaveformType.Sawtooth,

                WaveformType.Pulse,
                WaveformType.InversePulse
            );
        }

        private readonly float[] tempAudioSourceMixBuffer = new float[int.MaxValue / 2];

        private readonly ViewableList<Voice> onVoices = new ViewableList<Voice>(500);

        private readonly ViewableList<Voice> offVoices = new ViewableList<Voice>(500);

        // This is for use with the RemoveVoice method.
        private readonly ViewableList<int> onVoicesIndicesToRemoveOnNextRead;

        private readonly ViewableList<IAudioSource> audioSources;

        private readonly ViewableList<IAudioEffect> effects;

        private ArrayRingBuffer<float> recordedAudio;

        private readonly WaveFormat waveFormat;
        private readonly int sampleRate;

        private readonly object lockObject = new object();

        private double globalVoicePitchShiftRatio;
        private double globalGain;
        private double masterVolume;
        private double globalPan;

        private bool isRecordingAudio;

        private double currentLeftMix;
        private double currentRightMix;

        public int SampleRate
        {
            get => sampleRate;
        }

        public WaveFormat WaveFormat
        {
            get => waveFormat;
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

        public double GlobalGain
        {
            get => Interlocked.CompareExchange(ref globalGain, 0.0, 0.0);

            set
            {
                lock (lockObject)
                {
                    double previous = globalGain;

                    globalGain = GlobalGainRange.Clamp(value);

                    OnGlobalGainChanged?.Invoke(previous, globalGain);
                }
            }
        }

        public double MasterVolume
        {
            get => Interlocked.CompareExchange(ref masterVolume, 0.0, 0.0);

            set
            {
                lock(lockObject)
                {
                    double previous = masterVolume;

                    masterVolume = MasterVolumeRange.Clamp(value);

                    OnMasterVolumeChanged?.Invoke(previous, masterVolume);
                }
            }
        }

        public double GlobalPan
        {
            get => Interlocked.CompareExchange(ref globalPan, 0.0, 0.0);

            set
            {
                lock (lockObject)
                {
                    double previous = globalPan;

                    globalPan = GlobalPanRange.Clamp(value);

                    OnGlobalPanChanged?.Invoke(previous, globalPan);
                }
            }
        }

        public bool IsRecordingAudio
        {
            get => isRecordingAudio;
        }

        public int RecordedAudioCount
        {
            get => recordedAudio.CurrentCount;
        }

        public double RecordedAudioDuration
        {
            get => recordedAudio.CurrentCount / (2.0 * SampleRate);
        }

        public double CurrentLeftMix
        {
            get => currentLeftMix;
        }

        public double CurrentRightMix
        {
            get => currentRightMix;
        }

        public event Action<PolyphonicSynthesizer, Voice> OnVoiceAdded;
        public event Action<PolyphonicSynthesizer, Voice> OnVoiceRemoved;

        public event Action<double, double> OnGlobalGainChanged;
        public event Action<double, double> OnMasterVolumeChanged;
        public event Action<double, double> OnGlobalPanChanged;

        public PolyphonicSynthesizer(int sampleRate = DEFAULT_SAMPLE_RATE)
        {
            this.sampleRate = sampleRate;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);

            onVoicesIndicesToRemoveOnNextRead = new ViewableList<int>();

            audioSources = new ViewableList<IAudioSource>(100);

            effects = new ViewableList<IAudioEffect>(100);

            int recordingMaxSampleCount = 2 * sampleRate * 300;

            recordedAudio = new ArrayRingBuffer<float>(recordingMaxSampleCount);

            GlobalVoicePitchShiftSemitones = 0.0;

            GlobalGain = DEFAULT_GLOBAL_GAIN;

            MasterVolume = DEFAULT_MASTER_VOLUME;

            GlobalPan = DEFAULT_GLOBAL_PAN;
        }

        public void AddAudioSource(IAudioSource audioSource)
        {
            lock(lockObject)
            {
                audioSources.Add(audioSource);
            }
        }

        public bool RemoveAudioSource(IAudioSource audioSource)
        {
            lock(lockObject)
            {
                return audioSources.Remove(audioSource);
            }
        }

        public void AddAudioEffect(IAudioEffect effect)
        {
            lock(lockObject)
            {
                effects.Add(effect);
            }
        }

        public bool RemoveAudioEffect(IAudioEffect effect)
        {
            lock (lockObject)
            {
                return effects.Remove(effect);
            }
        }

        public void ForEachVoice(Action<Voice> action)
        {
            lock (lockObject)
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
        }

        public void AddVoices(Voice[] voices,
                              bool on = false)
        {
            for (int index = 0; index < voices.Length; index++)
            { 
                AddVoice(voices[index], on);
            }
        }

        public void AddVoice(Voice voice,
                             bool on = false)
        {
            if (ContainsVoice(voice))
            {
                throw new InvalidOperationException("voice already exists.");
            }

            if (voice.AdsrEnvelope is null)
            {
                voice.AdsrEnvelope = new AdsrEnvelope(sampleRate);
            }
            else
            {
                voice.AdsrEnvelope.SampleRate = sampleRate;
            }

            if (voice.FilterAdsrEnvelope is null)
            {
                /*voice.FilterAdsrEnvelope = new AdsrEnvelope(sampleRate)
                {
                    AttackSeconds = 0.005,
                    DecaySeconds = 0.15,
                    SustainLevel = 0.0,
                    ReleaseSeconds = 0.1
                };*/

                voice.FilterAdsrEnvelope = new AdsrEnvelope(sampleRate)
                {
                    AttackSeconds = 0.0,
                    DecaySeconds = 0.1,
                    SustainLevel = 1.0,
                    ReleaseSeconds = 0.0
                };
            }
            else
            {
                voice.FilterAdsrEnvelope.SampleRate = sampleRate;
            }

            if (voice.Oscillators is null || voice.Oscillators.IsEmpty)
            {
                if (voice.Oscillators is null)
                {
                    voice.Oscillators = new ViewableList<Oscillator>(10);
                }

                Oscillator defaultOscillator0 = new Oscillator
                {
                    CenterFrequency = voice.CenterFrequency,
                    Phase = 0,
                    Amplitude = DEFAULT_AMPLITUDE * 0.25,
                    WaveformType = DEFAULT_WAVEFORM_TYPE,
                    DetuneCents = 0
                };

                Oscillator defaultOscillator1 = new Oscillator
                {
                    CenterFrequency = voice.CenterFrequency,
                    Phase = 0,
                    Amplitude = DEFAULT_AMPLITUDE,
                    WaveformType = DEFAULT_WAVEFORM_TYPE,
                    DetuneCents = -2
                };

                Oscillator defaultOscillator2 = new Oscillator
                {
                    CenterFrequency = voice.CenterFrequency,
                    Phase = 0,
                    Amplitude = DEFAULT_AMPLITUDE,
                    WaveformType = DEFAULT_WAVEFORM_TYPE,
                    DetuneCents = 2
                };

                voice.Oscillators.Add(defaultOscillator0);
                voice.Oscillators.Add(defaultOscillator1);
                voice.Oscillators.Add(defaultOscillator2);
            }

            if (on)
            {
                VoiceOn(voice);
            }
            else
            {
                AddOffVoice(voice, removeFromOn: false);
            }

            if (OnVoiceAdded is not null)
            {
                lock (lockObject)
                {
                    OnVoiceAdded(this, voice);
                }
            }
        }

        public bool RemoveVoice(Voice voice,
                                bool allowReleaseIfOn = true)
        {
            if (!ContainsVoice(voice))
            {
                return false;
            }

            lock (lockObject)
            {
                int offIndex = offVoices.IndexOf(voice);

                if (offIndex != -1)
                {
                    offVoices.RemoveAt(offIndex);

                    return true;
                }

                int onIndex = onVoices.IndexOf(voice);

                // onIndex should not be -1 by now.
                Utils.Assert(onIndex != -1);

                if (allowReleaseIfOn)
                {
                    VoiceOff(voice);

                    onVoicesIndicesToRemoveOnNextRead.Add(onIndex);
                }
                else
                {
                    ResetVoice(voice);

                    onVoices.RemoveAt(onIndex);
                }

                return true;
            }
        }

        // Begins voice.
        // voice should already be added before this.
        public void VoiceOn(Voice voice)
        {
            lock (lockObject)
            {
                AddOnVoice(voice, removeFromOff: true);

                voice.AdsrEnvelope.NoteOn();
                voice.FilterAdsrEnvelope.NoteOn();
            }
        }

        // Initiates the ending of voice.
        // voice should already be added before this.
        public void VoiceOff(Voice voice)
        {
            lock (lockObject)
            {
                voice.AdsrEnvelope.NoteOff();
                voice.FilterAdsrEnvelope.NoteOff();
            }
        }

        // Begins voice.
        // voices should already be added before this.
        public void VoicesOn(Voice[] voices)
        {
            for (int index = 0; index < voices.Length; index++)
            {
                VoiceOn(voices[index]);
            }
        }

        // Initiates the ending of voice.
        // voices should already be added before this.
        public void VoicesOff(Voice[] voices)
        {
            for (int index = 0; index < voices.Length; index++)
            {
                VoiceOff(voices[index]);
            }
        }

        // Initiates the ending of any voice with the same center frequency as frequency.
        public void VoicesOff(double frequency)
        {
            const double COMPARISON_EPSILON = 0.01;

            lock (lockObject)
            {
                for (int index = 0; index < onVoices.Count; index++)
                {
                    Voice voice = onVoices.GetUnchecked(index);

                    if (Math.Abs(voice.CenterFrequency - frequency) < COMPARISON_EPSILON)
                    {
                        voice.AdsrEnvelope.NoteOff();
                    }
                }
            }
        }

        private void AddOnVoice(Voice voice, bool removeFromOff)
        {
            lock (lockObject)
            {
                if (!onVoices.Contains(voice))
                {
                    onVoices.Add(voice);
                }

                voice.IsOff = false;

                if (removeFromOff)
                {
                    offVoices.Remove(voice);
                }
            }
        }

        private void AddOffVoice(Voice voice, bool removeFromOn)
        {
            lock(lockObject)
            {
                offVoices.Add(voice);

                if (removeFromOn)
                {
                    onVoices.Remove(voice);
                }
            }
        }

        public bool ContainsVoice(Voice voice)
        {
            return offVoices.Contains(voice) || onVoices.Contains(voice);
        }

        public Voice GetFirstVoice(double centerFrequency)
        {
            Voice voice = offVoices.Find(voice => voice.CenterFrequency == centerFrequency);

            if (voice is null)
            {
                voice = onVoices.Find(voice => voice.CenterFrequency == centerFrequency);
            }

            return voice;
        }

        // Returns true if any audio was given.
        // If this returns false, it means there was no audio to give, and realCount will be set to 0.
        // If there is audio to give, this will return true, and realCount will be set to the min of requestedCount and the amount of samples available to give.
        // Additionally, this will give stereo audio. So realCount will always be an even number.
        // Recorded audio will always be in stereo.
        /*public bool TryTakeRecordedAudio(Span<float> samples, int requestedCount, out int realCount)
        {
            lock (lockObject)
            {
                if (recordedAudio.IsEmpty)
                {
                    realCount = 0;

                    return false;
                }

                // Ensure the requestedCount and available samples are even for stereo alignment.

                int toRead = Math.Min(requestedCount, recordedAudio.CurrentCount);

                if (toRead % 2 != 0)
                {
                    toRead--;
                }

                if (toRead <= 0)
                {
                    realCount = 0;

                    return false;
                }

                Span<float> samplesSpan = samples.Slice(0, toRead);

                realCount = recordedAudio.Read(samplesSpan);

                // Should always return true at this point.

                return realCount > 0;
            }
        }*/

        public bool TryGetRecordedAudio(out ReadOnlySpan<float> recording)
        {
            lock (lockObject)
            {
                if (IsRecordingAudio)
                {
                    recording = ReadOnlySpan<float>.Empty;

                    return false;
                }

                recording = recordedAudio.GetReadonlySpan();

                return true;
            }
        }

        public bool TryTakeRecordedAudio(Span<float> samples, int requestedCount, out int realCount)
        {
            lock (lockObject)
            {
                if (recordedAudio.IsEmpty)
                {
                    realCount = 0;

                    return false;
                }

                // REMOVE the Math.Min clamp. The ring buffer will handle looping internally 
                // to fill the entire requestedCount.
                int toRead = requestedCount;

                if (toRead % 2 != 0)
                {
                    toRead--;
                }

                if (toRead <= 0)
                {
                    realCount = 0;

                    return false;
                }

                Span<float> samplesSpan = samples.Slice(0, toRead);
                realCount = recordedAudio.Read(samplesSpan);

                return realCount > 0;
            }
        }

        // Returns true if it was already recording.
        public bool BeginRecordingAudio()
        {
            if (isRecordingAudio)
            {
                return true;
            }

            lock (lockObject)
            {
                isRecordingAudio = true;

                recordedAudio.Clear();
            }

            return false;
        }

        // Returns false if it was not recording.
        public bool StopRecordingAudio()
        {
            if (!isRecordingAudio)
            {
                return false;
            }

            lock (lockObject)
            {
                isRecordingAudio = false;
            }

            return true;
        }

        // Returns true if there was any recorded audio that was cleared.
        public bool ClearRecordedAudio()
        {
            if (isRecordingAudio)
            {
                StopRecordingAudio();
            }

            lock (lockObject)
            {
                if (recordedAudio.IsEmpty)
                {
                    return false;
                }

                recordedAudio.Clear();

                return true;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count); // Not sure about this.

            double gain = GlobalGain;
            double masterVolume = MasterVolume;

            double volumeCoefficient = gain * masterVolume;

            double globalPan = GlobalPan;

            double normalizedPan = (globalPan + 1.0) / 2.0;

            double leftMix = Math.Cos(normalizedPan * (Math.PI / 2));
            double rightMix = Math.Sin(normalizedPan * (Math.PI / 2));

            leftMix *= volumeCoefficient;
            rightMix *= volumeCoefficient;

            double globalVoicePitchShiftRatio = this.GlobalVoicePitchShiftRatio;

            currentLeftMix = leftMix;
            currentRightMix = rightMix;

            SynthesizeVoices(buffer, offset, count, globalVoicePitchShiftRatio);

            MixAudioSources(buffer, offset, count);

            MixAudioEffects(buffer, offset, count);

            // TODO: Maybe change to include gain/pan/final mix in recording.
            if (isRecordingAudio)
            {
                recordedAudio.Write(buffer.AsSpan(offset, count));
            }

            FinalMix(buffer, offset, count, leftMix, rightMix);

            return count;
        }

        private void SynthesizeVoices(float[] buffer, int offset, int count, double globalVoicePitchShiftRatio)
        {
            if (!onVoicesIndicesToRemoveOnNextRead.IsEmpty)
            {
                for (int i = 0; i < onVoicesIndicesToRemoveOnNextRead.Count; i++)
                {
                    int index = onVoicesIndicesToRemoveOnNextRead.GetUnchecked(i);

                    Voice voice = onVoices.GetUnchecked(index);

                    ResetVoice(voice);

                    onVoices.RemoveAt(index);
                }

                onVoicesIndicesToRemoveOnNextRead.Clear();
            }

            for (int bufferIndex = 0; bufferIndex < count; bufferIndex += 2)
            {
                double sample = 0.0;

                for (int voiceIndex = onVoices.Count - 1; voiceIndex >= 0; voiceIndex--)
                {
                    Voice voice = onVoices.GetUnchecked(voiceIndex);

                    double ampAdsrResult = voice.AdsrEnvelope.NextSample();
                    double filterAdsrResult = voice.FilterAdsrEnvelope.NextSample();

                    if (voice.AdsrEnvelope.IsFinished)
                    {
                        ResetVoice(voice);

                        AddOffVoice(voice, removeFromOn: true);

                        continue;
                    }

                    double voiceSample = 0.0;

                    for (int oscillatorIndex = 0; oscillatorIndex < voice.Oscillators.Count; oscillatorIndex++)
                    {
                        voiceSample += voice.Oscillators.GetUnchecked(oscillatorIndex).NextSample(sampleRate, globalVoicePitchShiftRatio);
                    }

                    if (voice.LPF is not null)
                    {
                        double cutoff = voice.BaseCutoff + (filterAdsrResult * voice.FilterAdsrEnvelopeAmount);

                        voice.LPF.Set(cutoff, voice.LPF.Resonance, sampleRate);

                        voiceSample = voice.LPF.Process(voiceSample);
                    }

                    voiceSample *= voice.Mix;

                    sample += ampAdsrResult * voiceSample;
                }

                WriteMonoToStereo(buffer, offset, bufferIndex, sample);
            }
        }

        private void MixAudioSources(float[] buffer, int offset, int count)
        {
            if (audioSources.IsEmpty)
            {
                return;
            }

            if (count > tempAudioSourceMixBuffer.Length)
            {
                throw new InvalidOperationException("This should never be reached!");
            }

            int audioSourceCount = audioSources.Count;

            Span<float> mixBuffer = new Span<float>(tempAudioSourceMixBuffer, 0, count);

            for (int index = 0; index < audioSourceCount; index++)
            {
                mixBuffer.Clear();

                int read = audioSources[index].Read(mixBuffer);

                for (int i = 0; i < read; i++)
                {
                    buffer[offset + i] += mixBuffer[i];
                }
            }
        }

        private void MixAudioEffects(float[] buffer, int offset, int count)
        {
            Span<float> bufferSpan = new Span<float>(buffer, offset, count);

            for (int index = 0; index < effects.Count; index++)
            {
                effects[index].Apply(bufferSpan);
            }
        }

        private void FinalMix(float[] buffer, int offset, int count, double leftMix, double rightMix)
        {
            for (int index = 0; index < count; index += 2)
            {
                int bufferIndex = offset + index;

                double leftSample = buffer[bufferIndex] * leftMix;
                double rightSample = buffer[bufferIndex + 1] * rightMix;

                leftSample = ClampSample(leftSample);
                rightSample = ClampSample(rightSample);

                WriteStereoToStereo(buffer, offset, index, leftSample, rightSample);
            }
        }

        public static void WriteMonoToStereo(float[] buffer, int offset, int index, double sample)
        {
            buffer[offset + index] = (float)sample;
            buffer[offset + index + 1] = (float)sample;
        }

        public static void WriteStereoToStereo(float[] buffer, int offset, int index, double leftSample, double rightSample)
        {
            buffer[offset + index] = (float)leftSample;
            buffer[offset + index + 1] = (float)rightSample;
        }

        private static double ClampSample(double sample)
        {
            if (sample < -1.0)
            {
                return -1.0;
            }

            if (sample > 1.0)
            {
                return 1.0;
            }

            return sample;
        }

        private static void ResetVoice(Voice voice)
        {
            voice.IsOff = true;

            voice.AdsrEnvelope.Reset();

            ResetOscillators(voice);
        }

        private static void ResetOscillators(Voice voice)
        {
            for (int oscillatorIndex = 0; oscillatorIndex < voice.Oscillators.Count; oscillatorIndex++)
            {
                voice.Oscillators.GetUnchecked(oscillatorIndex).Reset();
            }
        }
    }
}
