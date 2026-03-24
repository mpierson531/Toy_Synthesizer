using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    // TODO: Continue implementing.
    public enum SynthesizerCommandType : int
    {
        None,

        AddVoice,
        RemoveVoice,
        VoiceOn,
        VoiceOff,

        ForEachVoiceAction,

        SetVoice_CenterFrequency,

        SetVoice_Name,

        SetVoice_Mix,

        SetVoice_Attack,
        SetVoice_Decay,
        SetVoice_Sustain,
        SetVoice_Release,

        SetVoice_LPFBaseCutoff,
        SetVoice_LPF_Resonance,

        SetVoice_LPF_Attack,
        SetVoice_LPF_Decay,
        SetVoice_LPF_Sustain,
        SetVoice_LPF_Release,

        SetVoice_LPF_ADSR_Amount,

        Voice_AddOscillator,
        Voice_RemoveOscillator,

        SetVoice_Oscillator_Amplitude,
        SetVoice_Oscillator_WaveformType,
        SetVoice_Oscillator_DetuneCents,

        EndType
    }
}
