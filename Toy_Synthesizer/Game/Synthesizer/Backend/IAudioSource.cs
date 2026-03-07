using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    // Should be assumed to be the correct format, sample rate, etc.
    public interface IAudioSource
    {
        int Read(Span<float> buffer);
    }
}
