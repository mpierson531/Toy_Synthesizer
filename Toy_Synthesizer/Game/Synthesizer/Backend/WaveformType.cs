using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    public enum WaveformType : int
    {
        Sine,
        Triangle,
        Square,
        Sawtooth,

        Pulse,
        InversePulse
    }
}
