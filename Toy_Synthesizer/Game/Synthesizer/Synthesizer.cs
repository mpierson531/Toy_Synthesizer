using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Input;

using NAudio.CoreAudioApi;
using NAudio.Wave;

using GeoLib;
using GeoLib.GeoInput;

using Toy_Synthesizer.Game.Synthesizer.Frontend;

namespace Toy_Synthesizer.Game.Synthesizer
{
    public class Synthesizer : Disposable
    {
        private readonly Game game;

        private readonly Backend.Backend backend;
        private readonly Frontend.Frontend frontend;

        public Backend.Backend Backend
        {
            get => backend;
        }

        public Frontend.Frontend Frontend
        {
            get => frontend;
        }

        public Synthesizer(Game game)
        {
            this.game = game;

            this.backend = new Backend.Backend(game);

            this.frontend = new Frontend.Frontend(game, backend);
        }

        public void Update(float delta)
        {

        }

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            backend.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
