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
        public const double DEFAULT_BASE_CUTOFF = 800.0;
        public const double DEFAULT_FILTER_ADSR_ENVELOPE_AMOUNT = 3000.0;
        public const double DEFAULT_MIX = 1.0;

        public const double MIN_MIX = 0.0;
        public const double MAX_MIX = 1.0;

        public static readonly NumberRange<double> MixRange;

        static Voice()
        {
            MixRange = NumberRange<double>.From(MIN_MIX, MAX_MIX);
        }

        public static Voice FromMidi(MidiNote note, 
                                     double mix = DEFAULT_MIX,
                                     StateVariableLPF stateVariableLPF = null,
                                     AdsrEnvelope adsrEnvelope = null,
                                     AdsrEnvelope filterAdsrEnvelope = null,
                                     double baseCutoff = DEFAULT_BASE_CUTOFF,
                                     double filterAdsrEnvelopeAmount = DEFAULT_FILTER_ADSR_ENVELOPE_AMOUNT,
                                     ViewableList<Oscillator> oscillators = null)
        {
            return new Voice
            {
                Name = note.ToString(),
                Mix = mix,
                CenterFrequency = MidiUtils.GetFrequency(note),
                LPF = stateVariableLPF,
                AdsrEnvelope = adsrEnvelope,
                FilterAdsrEnvelope = filterAdsrEnvelope,
                BaseCutoff = baseCutoff,
                FilterAdsrEnvelopeAmount = filterAdsrEnvelopeAmount,
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
            set => mix = Math.Clamp(value, MIN_MIX, MAX_MIX);
        }

        public StateVariableLPF LPF;
        public AdsrEnvelope AdsrEnvelope;
        public AdsrEnvelope FilterAdsrEnvelope;
        public double BaseCutoff = DEFAULT_BASE_CUTOFF;
        public double FilterAdsrEnvelopeAmount = DEFAULT_FILTER_ADSR_ENVELOPE_AMOUNT;
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

                AdsrEnvelope = Copyables.Cast<AdsrEnvelope>(voice.AdsrEnvelope, deepCopy),
                FilterAdsrEnvelope = Copyables.Cast<AdsrEnvelope>(voice.FilterAdsrEnvelope, deepCopy),

                BaseCutoff = voice.BaseCutoff,
                FilterAdsrEnvelopeAmount = voice.FilterAdsrEnvelopeAmount,

                Oscillators = Copyables.Cast<ViewableList<Oscillator>>(voice.Oscillators, deepCopy),

                IsOff = true
            };
        }
    }
}