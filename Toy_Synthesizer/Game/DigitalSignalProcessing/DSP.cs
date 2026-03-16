using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using NAudio.Wave;

using Toy_Synthesizer.Game.CommonUtils;
using Toy_Synthesizer.Game.Synthesizer.Backend;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing
{
    public class DSP : ISampleProvider
    {
        public const double DEFAULT_GLOBAL_GAIN = 0.5;
        public const double DEFAULT_MASTER_VOLUME = 1.0;
        public const double DEFAULT_GLOBAL_PAN = 0.0;

        public const double MIN_GLOBAL_GAIN = 0.0;
        public const double MAX_GLOBAL_GAIN = 5.0;

        public const double MIN_MASTER_VOLUME = 0.0;
        public const double MAX_MASTER_VOLUME = 1.0;

        public const double MIN_GLOBAL_PAN = -1.0;
        public const double MAX_GLOBAL_PAN = 1.0;

        public static readonly NumberRange<double> GlobalGainRange;
        public static readonly NumberRange<double> MasterVolumeRange;
        public static readonly NumberRange<double> GlobalPanRange;

        static DSP()
        {
            GlobalGainRange = NumberRange<double>.From(MIN_GLOBAL_GAIN, MAX_GLOBAL_GAIN);

            MasterVolumeRange = NumberRange<double>.From(MIN_MASTER_VOLUME, MAX_MASTER_VOLUME);

            GlobalPanRange = NumberRange<double>.From(MIN_GLOBAL_PAN, MAX_GLOBAL_PAN);
        }

        private readonly float[] tempAudioSourceMixBuffer = new float[int.MaxValue / 2];

        private ViewableList<DSPCommand> tempCommands;
        private ViewableList<DSPCommand> pendingCommands;

        private readonly Dictionary<IAudioSource, ViewableList<AudioSourceCommand>> audioSourceCommandBatches;
        private readonly Dictionary<IAudioEffect, ViewableList<AudioEffectCommand>> audioEffectCommandBatches;

        private readonly ViewableList<IAudioSource> audioSources;

        private readonly ViewableList<IAudioEffect> effects;

        private ArrayRingBuffer<float> recordedAudio;

        private readonly WaveFormat waveFormat;
        private readonly int sampleRate;

        private readonly object lockObject = new object();

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
                lock (lockObject)
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

        public DSP(int sampleRate)
        {
            this.sampleRate = sampleRate;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);

            this.tempCommands = new ViewableList<DSPCommand>(1000);
            this.pendingCommands = new ViewableList<DSPCommand>(1000);

            this.audioSourceCommandBatches = new Dictionary<IAudioSource, ViewableList<AudioSourceCommand>>(1000);

            this.audioEffectCommandBatches = new Dictionary<IAudioEffect, ViewableList<AudioEffectCommand>>(1000);

            audioSources = new ViewableList<IAudioSource>(1000);

            effects = new ViewableList<IAudioEffect>(1000);

            int recordingMaxSampleCount = 2 * sampleRate * 300;

            recordedAudio = new ArrayRingBuffer<float>(recordingMaxSampleCount);

            GlobalGain = DEFAULT_GLOBAL_GAIN;

            MasterVolume = DEFAULT_MASTER_VOLUME;

            GlobalPan = DEFAULT_GLOBAL_PAN;
        }

        // Not from audio thread, so locking should be ok for the most part.
        public void AddAudioSource(IAudioSource audioSource)
        {
            QueueCommand(DSPCommand.AddAudioSource(audioSource));
        }

        public void RemoveAudioSource(IAudioSource audioSource)
        {
            QueueCommand(DSPCommand.RemoveAudioSource(audioSource));
        }

        public void AddAudioEffect(IAudioEffect effect)
        {
            QueueCommand(DSPCommand.AddAudioEffect(effect));
        }

        public void RemoveAudioEffect(IAudioEffect effect)
        {
            QueueCommand(DSPCommand.RemoveAudioEffect(effect));
        }

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

        public void BeginRecordingAudio()
        {
            QueueCommand(DSPCommand.BeginRecordingAudio());
        }

        public void StopRecordingAudio()
        {
            QueueCommand(DSPCommand.StopRecordingAudio());
        }

        // Returns true if there was any recorded audio that was cleared.
        public void ClearRecordedAudio()
        {
            QueueCommand(DSPCommand.ClearRecordedAudio());
        }

        public void SendAudioSourceCommand(IAudioSource audioSource, AudioSourceCommand command)
        {
            QueueCommand(DSPCommand.SendAudioSourceCommand(audioSource, command));
        }

        public void SendAudioEffectCommand(IAudioEffect audioEffect, AudioEffectCommand command)
        {
            QueueCommand(DSPCommand.SendAudioEffectCommand(audioEffect, command));
        }

        public int Read(float[] buffer, int offset, int count)
        {
            ExecuteCommands();

            Array.Clear(buffer); // Only this seems to work.

            // Array.Clear(buffer, offset, count); // Doesn't seem to work. Strange sputtering when using this.

            double gain = GlobalGain;
            double masterVolume = MasterVolume;

            double volumeCoefficient = gain * masterVolume;

            double globalPan = GlobalPan;

            double normalizedPan = (globalPan + 1.0) / 2.0;

            double leftMix = Math.Cos(normalizedPan * (Math.PI / 2));
            double rightMix = Math.Sin(normalizedPan * (Math.PI / 2));

            leftMix *= volumeCoefficient;
            rightMix *= volumeCoefficient;

            currentLeftMix = leftMix;
            currentRightMix = rightMix;

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
                //mixBuffer.Clear();

                Array.Clear(tempAudioSourceMixBuffer, 0, count);

                int read = audioSources[index].Read(mixBuffer);

                for (int i = 0; i < read; i++)
                {
                    buffer[offset + i] += mixBuffer[i];
                }
            }
        }

        private void MixAudioEffects(float[] buffer, int offset, int count)
        {
            if (effects.IsEmpty)
            {
                return;
            }

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

                DSPUtils.WriteStereoToStereo(buffer, offset, index, leftSample, rightSample);
            }
        }

        private void ExecuteCommands()
        {
            ViewableList<DSPCommand> newTempCommands = pendingCommands;
            ViewableList<DSPCommand> commands = Interlocked.Exchange(ref pendingCommands, tempCommands);

            Interlocked.Exchange(ref tempCommands, newTempCommands);

            for (int index = 0; index < commands.Count; index++)
            {
                ref readonly DSPCommand command = ref commands.GetRefUnchecked(index);

                if (command.Type == DSPCommandType.SendAudioSourceCommand)
                {
                    QueueBatchedAudioSourceCommand(in command);
                }
                else if (command.Type == DSPCommandType.SendAudioEffectCommand)
                {
                    QueueBatchedAudioEffectCommand(in command);
                }
                else
                {
                    ExecuteCommand(in command);
                }
            }

            commands.Clear();

            FlushBatchedAudioSourceCommands();

            FlushBatchedAudioEffectCommands();
        }

        private void ExecuteCommand(ref readonly DSPCommand command)
        {
            // Handling SendAudioSourceCommand elsewhere.

            GeoDebug.Assert(command.Type != DSPCommandType.SendAudioSourceCommand);

            switch (command.Type)
            {
                case DSPCommandType.None:
                    return;

                case DSPCommandType.AddAudioSource:
                    ThrowIfCommandAudioSourceIsNull(in command);
                    AddAudioSource_Raw(command.AudioSource);
                    break;

                case DSPCommandType.RemoveAudioSource:
                    ThrowIfCommandAudioSourceIsNull(in command);
                    RemoveAudioSource_Raw(command.AudioSource);
                    break;

                case DSPCommandType.AddAudioEffect:
                    ThrowIfCommandAudioEffectIsNull(in command);
                    AddAudioEffect_Raw(command.AudioEffect);
                    break;

                case DSPCommandType.RemoveAudioEffect:
                    ThrowIfCommandAudioEffectIsNull(in command);
                    RemoveAudioEffect_Raw(command.AudioEffect);
                    break;

                case DSPCommandType.BeginRecordingAudio:
                    BeginRecordingAudio_Raw();
                    break;

                case DSPCommandType.StopRecordingAudio:
                    StopRecordingAudio_Raw();
                    break;

                case DSPCommandType.ClearRecordedAudio:
                    ClearRecordedAudio_Raw();
                    break;

                default: throw new InvalidOperationException($"Invalid DSPCommandType: \"{command.Type}\"");
            }
        }

        private void FlushBatchedAudioSourceCommands()
        {
            foreach (var kvp in audioSourceCommandBatches)
            {
                IAudioSource audioSource = kvp.Key;
                ViewableList<AudioSourceCommand> commands = kvp.Value;

                if (commands.Count > 0)
                {
                    GeoDebug.Assert(audioSource is IAudioSourceCommandReceiver receiver && audioSources.Contains(audioSource));

                    ReadOnlySpan<AudioSourceCommand> commandsSpan = commands.ToReadonlySpan();

                    ((IAudioSourceCommandReceiver)audioSource).SendCommands(commandsSpan);

                    commands.Clear();
                }
            }
        }

        private void FlushBatchedAudioEffectCommands()
        {
            foreach (var kvp in audioEffectCommandBatches)
            {
                IAudioEffect effect = kvp.Key;
                ViewableList<AudioEffectCommand> commands = kvp.Value;

                if (commands.Count > 0)
                {
                    GeoDebug.Assert(effect is IAudioEffectCommandReceiver receiver && effects.Contains(effect));

                    ReadOnlySpan<AudioEffectCommand> commandsSpan = commands.ToReadonlySpan();

                    ((IAudioEffectCommandReceiver)effect).SendCommands(commandsSpan);

                    commands.Clear();
                }
            }
        }

        private void QueueBatchedAudioSourceCommand(ref readonly DSPCommand command)
        {
            if (!audioSources.Contains(command.AudioSource))
            {
                return;
            }

            if (command.AudioSource is not IAudioSourceCommandReceiver)
            {
                return;
            }

            // Rather than adding initializing the batch in AddAudioSource_Raw, I'm just going to do it here cause I'm already checking here.

            if (!audioSourceCommandBatches.TryGetValue(command.AudioSource, out var batchList))
            {
                batchList = new ViewableList<AudioSourceCommand>();

                audioSourceCommandBatches[command.AudioSource] = batchList;
            }

            batchList.Add(command.AudioSourceCommand);
        }

        private void QueueBatchedAudioEffectCommand(ref readonly DSPCommand command)
        {
            if (!effects.Contains(command.AudioEffect))
            {
                return;
            }

            if (command.AudioEffect is not IAudioEffectCommandReceiver)
            {
                return;
            }

            // Rather than adding initializing the batch in AddAudioEffect_Raw, I'm just going to do it here cause I'm already checking here.

            if (!audioEffectCommandBatches.TryGetValue(command.AudioEffect, out var batchList))
            {
                batchList = new ViewableList<AudioEffectCommand>();

                audioEffectCommandBatches[command.AudioEffect] = batchList;
            }

            batchList.Add(command.AudioEffectCommand);
        }

        private void AddAudioSource_Raw(IAudioSource audioSource)
        {
            audioSources.Add(audioSource);
        }

        private void RemoveAudioSource_Raw(IAudioSource audioSource)
        {
            audioSources.Remove(audioSource);

            audioSourceCommandBatches.Remove(audioSource);
        }

        private void AddAudioEffect_Raw(IAudioEffect audioEffect)
        {
            effects.Add(audioEffect);
        }

        private void RemoveAudioEffect_Raw(IAudioEffect audioEffect)
        {
            effects.Remove(audioEffect);

            audioEffectCommandBatches.Remove(audioEffect);
        }

        private void BeginRecordingAudio_Raw()
        {
            if (isRecordingAudio)
            {
                return;
            }

            isRecordingAudio = true;
        }

        private void StopRecordingAudio_Raw()
        {
            if (!isRecordingAudio)
            {
                return;
            }

            isRecordingAudio = false;
        }

        private void ClearRecordedAudio_Raw()
        {
            if (isRecordingAudio)
            {
                StopRecordingAudio_Raw();
            }

            recordedAudio.Clear();
        }

        private void QueueCommand(DSPCommand command)
        {
            lock (lockObject)
            {
                tempCommands.Add(command);
            }
        }

        private static void ThrowIfCommandAudioSourceIsNull(ref readonly DSPCommand command)
        {
            if (command.AudioSource is null)
            {
                string message = $"Cannot {(command.Type == DSPCommandType.AddAudioSource ? "add" : "remove")} null audio source.";

                throw new InvalidOperationException(message);
            }
        }

        private static void ThrowIfCommandAudioEffectIsNull(ref readonly DSPCommand command)
        {
            if (command.AudioEffect is null)
            {
                string message = $"Cannot {(command.Type == DSPCommandType.AddAudioEffect ? "add" : "remove")} null audio effect.";

                throw new InvalidOperationException(message);
            }
        }

        public static double ClampSample(double sample)
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
    }
}