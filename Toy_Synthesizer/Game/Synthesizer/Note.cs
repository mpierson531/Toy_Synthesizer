using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.Synthesizer
{
    public struct Note
    {
        public Key Key;
        public double BaseCenterFrequency;
        public int Octave;

        public readonly double GetFrequency()
        {
            return ChromaticScaleUtils.GetFrequency(BaseCenterFrequency, Octave);
        }

        public Note(Key key, double baseCenterFrequency, int octave)
        {
            Key = key;
            BaseCenterFrequency = baseCenterFrequency;
            Octave = octave;
        }

        public Note()
        {

        }
    }
}