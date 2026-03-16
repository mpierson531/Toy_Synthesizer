using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.DigitalSignalProcessing
{
    public enum DSPCommandType : int
    {
        None,

        AddAudioSource,
        RemoveAudioSource,

        AddAudioEffect,
        RemoveAudioEffect,

        BeginRecordingAudio,
        StopRecordingAudio,
        ClearRecordedAudio,

        SendAudioSourceCommand, 
        SendAudioEffectCommand
    }
}
