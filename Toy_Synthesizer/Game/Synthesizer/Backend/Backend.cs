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

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    public class Backend : Disposable
    {
        private readonly Game game;

        private readonly PolyphonicSynthesizer polyphonic;
        private readonly MMDevice outputDevice;
        private readonly WasapiOut output;

        private ParallelOptions parallelOptions;

        public ParallelOptions ParallelOptions
        {
            get => parallelOptions;
            set => parallelOptions = value;
        }

        public PolyphonicSynthesizer PolyphonicSynthesizer
        {
            get => polyphonic;
        }

        public Backend(Game game)
        {
            this.game = game;

            outputDevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            output = new WasapiOut(outputDevice, AudioClientShareMode.Shared, useEventSync: false, latency: 50);

            polyphonic = new PolyphonicSynthesizer();

            output.Init(polyphonic);
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
    }
}
