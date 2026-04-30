using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.AudioBackend
{
    public interface IFloatSampleProvider
    {
        int Read(float[] buffer, int offset, int count);
    }
}
