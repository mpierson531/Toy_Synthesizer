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

        AddGlobalVoiceOscillator,
        RemoveGlobalVoiceOscillator,

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

        SetVoice_Filter,
        SetVoice_Filter_BaseCutoff,
        SetVoice_Filter_Resonance,
        SetVoice_Filter_Gain,
        SetVoice_Filter_Mix,
        SetVoice_Filter_Mix_Low,
        SetVoice_Filter_Mix_High,
        SetVoice_Filter_Mix_Band,

        SetVoice_Filter_Attack,
        SetVoice_Filter_Decay,
        SetVoice_Filter_Sustain,
        SetVoice_Filter_Release,

        SetVoice_Filter_ADSR_Amount,

        Voice_AddOscillator,
        Voice_RemoveOscillator,
        Voice_ForEachOscillator,

        SetVoice_Oscillator_Amplitude,
        SetVoice_Oscillator_WaveformType,
        SetVoice_Oscillator_DetuneCents,

        EndType
    }
}
