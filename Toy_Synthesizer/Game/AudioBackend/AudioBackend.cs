using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoAudio;
using GeoLib.GeoAudio.DataProviders;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Microsoft.Xna.Framework;

using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Toy_Synthesizer.Game.AudioBackend
{
    public class AudioBackend : Disposable
    {
        public const int SAMPLE_RATE = 44100;

        private readonly Game game;

        private readonly MMDevice outputDevice;
        private readonly WasapiOut output;

        private readonly WaveFormat naudioWaveFormat;

        private readonly NAudioSampleProviderWrapper naudioSampleProviderWrapper;

        private readonly IFloatSampleProvider floatSampleProvider;

        public AudioBackend(Game game, IFloatSampleProvider floatSampleProvider)
        {
            this.game = game;

            this.floatSampleProvider = floatSampleProvider;

            outputDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            output = new WasapiOut(outputDevice, AudioClientShareMode.Shared, useEventSync: false, latency: 50);

            naudioWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: SAMPLE_RATE, channels: 2);

            naudioSampleProviderWrapper = new NAudioSampleProviderWrapper(naudioWaveFormat, floatSampleProvider);

            output.Init(naudioSampleProviderWrapper);
            output.Play();
        }

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            output.Stop();
            output.Dispose();

            outputDevice.Dispose();

            GC.SuppressFinalize(this);
        }

        private sealed class NAudioSampleProviderWrapper : ISampleProvider
        {
            private readonly WaveFormat naudioWaveFormat;
            private readonly IFloatSampleProvider floatSampleProvider;

            WaveFormat ISampleProvider.WaveFormat
            {
                get => naudioWaveFormat;
            }

            internal NAudioSampleProviderWrapper(WaveFormat naudioWaveFormat, IFloatSampleProvider floatSampleProvider)
            {
                this.naudioWaveFormat = naudioWaveFormat;

                this.floatSampleProvider = floatSampleProvider;
            }

            int ISampleProvider.Read(float[] buffer, int offset, int count)
            {
                return floatSampleProvider.Read(buffer, offset, count);
            }
        }
    }
}
