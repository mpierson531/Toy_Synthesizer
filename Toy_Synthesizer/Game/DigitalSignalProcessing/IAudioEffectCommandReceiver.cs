using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing
{
    public interface IAudioEffectCommandReceiver
    {
        void SendCommands(ReadOnlySpan<AudioEffectCommand> commands);

        void SendCommand(ref readonly AudioEffectCommand command);
    }
}
