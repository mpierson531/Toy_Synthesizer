using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing.BuiltinAudioEffects.Delays
{
    public static class CommonDelayCommands
    {
        public static AudioEffectCommand SetDelaySeconds(float delaySeconds)
        {
            return AudioEffectCommand.SendFloat((int)CommonDelayCommandType.SetDelaySeconds, delaySeconds);
        }

        public static AudioEffectCommand SetFeedbackLevel(float feedbackLevel)
        {
            return AudioEffectCommand.SendFloat((int)CommonDelayCommandType.SetFeedbackLevel, feedbackLevel);
        }

        public static AudioEffectCommand SetLevel(float level)
        {
            return AudioEffectCommand.SendFloat((int)CommonDelayCommandType.SetLevel, level);
        }
    }
}
