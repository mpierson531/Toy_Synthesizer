using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.DigitalSignalProcessing.Filters;
using Toy_Synthesizer.Game.Midi;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    // NOTE: This class will not validate or clamp values
    public class Voice : ICopyable
    {
        public static Voice FromMidi(MidiNote note, 
                                     double mix = PolyphonicSynthesizer.DEFAULT_MIX,
                                     SVF filter = null,
                                     UnmanagedNullable<SVFMix> filterMix = default,
                                     AdsrEnvelope adsr = null,
                                     AdsrEnvelope lpfAdsr = null,
                                     double lpfBaseCutoff = PolyphonicSynthesizer.DEFAULT_FILTER_BASE_CUTOFF,
                                     double lpfAdsrAmount = PolyphonicSynthesizer.DEFAULT_FILTER_ADSR_AMOUNT,
                                     ViewableList<Oscillator> oscillators = null)
        {
            return new Voice
            {
                Name = note.ToString(),
                Mix = mix,
                CenterFrequency = MidiUtils.GetFrequency(note),
                Filter = filter,
                FilterMix = filterMix,
                Adsr = adsr,
                Filter_Adsr = lpfAdsr,
                Filter_BaseCutoff = lpfBaseCutoff,
                Filter_AdsrAmount = lpfAdsrAmount,
                Oscillators = oscillators
            };
        }

        public static Voice EmptyDefault(int sampleRate = AudioBackend.AudioBackend.SAMPLE_RATE)
        {
            return new Voice
            {
                Name = TextUtils.EmptyString,
                Mix = PolyphonicSynthesizer.DEFAULT_MIX,
                CenterFrequency = 0.0,
                Filter = null,
                FilterMix = default,
                Adsr = new AdsrEnvelope(sampleRate),
                Filter_Adsr = new AdsrEnvelope(sampleRate),
                Filter_BaseCutoff = PolyphonicSynthesizer.DEFAULT_FILTER_BASE_CUTOFF,
                Filter_AdsrAmount = PolyphonicSynthesizer.DEFAULT_FILTER_ADSR_AMOUNT,
                Oscillators = null
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

        public string Name = null;

        public double Mix = PolyphonicSynthesizer.DEFAULT_MIX;

        public AdsrEnvelope Adsr;
        public SVF Filter;
        public UnmanagedNullable<SVFMix> FilterMix;
        public double Filter_BaseCutoff = PolyphonicSynthesizer.DEFAULT_FILTER_BASE_CUTOFF;
        public AdsrEnvelope Filter_Adsr;
        public double Filter_AdsrAmount = PolyphonicSynthesizer.DEFAULT_FILTER_ADSR_AMOUNT;
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

                Filter = Copyables.Cast<SVF>(voice.Filter, deepCopy),
                FilterMix = voice.FilterMix,

                Adsr = Copyables.Cast<AdsrEnvelope>(voice.Adsr, deepCopy),
                Filter_Adsr = Copyables.Cast<AdsrEnvelope>(voice.Filter_Adsr, deepCopy),

                Filter_BaseCutoff = voice.Filter_BaseCutoff,
                Filter_AdsrAmount = voice.Filter_AdsrAmount,

                Oscillators = Copyables.Cast<ViewableList<Oscillator>>(voice.Oscillators, deepCopy),

                IsOff = true
            };
        }
    }
}