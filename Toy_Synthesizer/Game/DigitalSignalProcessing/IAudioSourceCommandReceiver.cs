using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing
{
    public interface IAudioSourceCommandReceiver
    {
        void SendCommands(ReadOnlySpan<AudioSourceCommand> commands);

        void SendCommand(ref readonly AudioSourceCommand command);
    }
}
