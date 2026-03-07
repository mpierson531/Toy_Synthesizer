using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

namespace Toy_Synthesizer.Game.Synthesizer.Backend.BuiltinAudioSources
{
    // TODO: If PolyphonicSynthesizer changes to include gain/pan/final mix in recording, compensate.

    public class RecordedAudioSource : IAudioSource
    {
        private readonly PolyphonicSynthesizer synthesizer;

        private int framesPlayed;

        // This is in seconds.
        public double Duration
        {
            get => synthesizer.RecordedAudioDuration;
        }

        // This is in seconds.
        public double PlaybackPositionSeconds
        {
            get
            {
                long totalFrames = framesPlayed;
                long clipFrames = synthesizer.RecordedAudioCount / 2;

                if (clipFrames == 0)
                {
                    return 0;
                }

                long frameInClip = totalFrames % clipFrames;

                return frameInClip / (double)synthesizer.SampleRate;
            }
        }

        public RecordedAudioSource(PolyphonicSynthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }

        public int Read(Span<float> buffer)
        {
            synthesizer.TryTakeRecordedAudio(buffer, requestedCount: buffer.Length, out int realCount);

            if (realCount == 0)
            {
                return 0;
            }

            framesPlayed += realCount / 2;

            return realCount;
        }
    }
}
