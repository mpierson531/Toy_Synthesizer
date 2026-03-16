using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.Midi;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    public class Voice : ICopyable
    {
        public static Voice FromMidi(MidiNote note, 
                                     double mix = PolyphonicSynthesizer.DEFAULT_MIX,
                                     StateVariableLPF stateVariableLPF = null,
                                     AdsrEnvelope adsr = null,
                                     AdsrEnvelope lpfAdsr = null,
                                     double lpfBaseCutoff = PolyphonicSynthesizer.DEFAULT_LPFBASE_CUTOFF,
                                     double lpfAdsrAmount = PolyphonicSynthesizer.DEFAULT_LPF_ADSR_AMOUNT,
                                     ViewableList<Oscillator> oscillators = null)
        {
            return new Voice
            {
                Name = note.ToString(),
                Mix = mix,
                CenterFrequency = MidiUtils.GetFrequency(note),
                LPF = stateVariableLPF,
                Adsr = adsr,
                LPF_Adsr = lpfAdsr,
                LPF_BaseCutoff = lpfBaseCutoff,
                LPF_AdsrAmount = lpfAdsrAmount,
                Oscillators = oscillators
            };
        }

        private double centerFrequency;

        public double CenterFrequency // All oscillators should have the same center frequency.
        {
            get => centerFrequency;

            set
            {
                centerFrequency = value;

                if (Oscillators is not null && !Oscillators.IsEmpty)
                {
                    for (int index = 0; index < Oscillators.Count; index++)
                    {
                        Oscillators[index].CenterFrequency = centerFrequency;
                    }
                }
            }
        }

        private double mix;

        public string Name;

        public double Mix
        {
            get => mix;
            set => mix = PolyphonicSynthesizer.MixRange.Clamp(value);
        }

        public AdsrEnvelope Adsr;
        public StateVariableLPF LPF;
        public double LPF_BaseCutoff = PolyphonicSynthesizer.DEFAULT_LPFBASE_CUTOFF;
        public AdsrEnvelope LPF_Adsr;
        public double LPF_AdsrAmount = PolyphonicSynthesizer.DEFAULT_LPF_ADSR_AMOUNT;
        public ViewableList<Oscillator> Oscillators;
        public bool IsOff;

        public Voice Copy(bool deepCopy)
        {
            return Copy(this, deepCopy);
        }

        object ICopyable.Copy(bool deepCopy)
        {
            return Copy(deepCopy);
        }

        public static Voice Copy(Voice voice, bool deepCopy = false)
        {
            return new Voice
            {
                CenterFrequency = voice.CenterFrequency,

                Name = voice.Name,

                Mix = voice.Mix,

                LPF = Copyables.Cast<StateVariableLPF>(voice.LPF, deepCopy),

                Adsr = Copyables.Cast<AdsrEnvelope>(voice.Adsr, deepCopy),
                LPF_Adsr = Copyables.Cast<AdsrEnvelope>(voice.LPF_Adsr, deepCopy),

                LPF_BaseCutoff = voice.LPF_BaseCutoff,
                LPF_AdsrAmount = voice.LPF_AdsrAmount,

                Oscillators = Copyables.Cast<ViewableList<Oscillator>>(voice.Oscillators, deepCopy),

                IsOff = true
            };
        }
    }
}