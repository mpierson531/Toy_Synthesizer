using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing
{
    public interface IAudioEffect
    {
        public void Apply(Span<float> buffer);
    }
}
