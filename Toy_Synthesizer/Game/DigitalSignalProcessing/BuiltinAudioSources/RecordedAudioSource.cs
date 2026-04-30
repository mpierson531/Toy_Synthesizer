using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.DigitalSignalProcessing;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.BuiltinAudioSources
{
    // TODO: If PolyphonicSynthesizer changes to include gain/pan/final mix in recording, compensate.

    public class RecordedAudioSource : IAudioSource
    {
        private DSP dsp;

        private int framesPlayed;

        // This is in seconds.
        public double Duration
        {
            get => dsp.RecordedAudioDuration;
        }

        // This is in seconds.
        public double PlaybackPositionSeconds
        {
            get
            {
                long totalFrames = framesPlayed;

                long clipFrames = dsp.RecordedAudioCount / 2;

                if (clipFrames == 0)
                {
                    return 0;
                }

                long frameInClip = totalFrames % clipFrames;

                return frameInClip / (double)dsp.SampleRate;
            }
        }

        public RecordedAudioSource(DSP dsp)
        {
            this.dsp = dsp;
        }

        public int Read(Span<float> buffer)
        {
            dsp.TryTakeRecordedAudio(buffer, requestedCount: buffer.Length, out int realCount);

            if (realCount == 0)
            {
                return 0;
            }

            framesPlayed += realCount / 2;

            return realCount;
        }
    }
}
