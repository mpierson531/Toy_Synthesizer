using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.BuiltinAudioEffects.Delays
{
    public enum CommonDelayCommandType : int
    {
        None,

        SetDelaySeconds,
        SetFeedbackLevel,
        SetLevel
    }
}
