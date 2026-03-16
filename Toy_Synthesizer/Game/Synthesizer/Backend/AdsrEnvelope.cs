using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;

namespace Toy_Synthesizer.Game.Synthesizer.Backend
{
    // TODO: implement commands.
    public class AdsrEnvelope : ICopyable
    {
        public int SampleRate;

        public double AttackSeconds = 0.05;
        public double DecaySeconds = 0.1;
        public double SustainLevel = 0.6;
        public double ReleaseSeconds = 0.1;

        public EnvelopeStage Stage { get; private set; } = EnvelopeStage.Off;
        public double Level { get; private set; } = 0.0;

        public bool IsFinished
        {
            get => Stage == EnvelopeStage.Off;
        }

        public AdsrEnvelope(int sampleRate)
        {
            SampleRate = sampleRate;
        }

        public void NoteOn()
        {
            Stage = EnvelopeStage.Attack;
        }

        public void NoteOff()
        {
            if (Stage != EnvelopeStage.Off)
            {
                Stage = EnvelopeStage.Release;
            }
        }

        public double NextSample()
        {
            switch (Stage)
            {
                case EnvelopeStage.Attack:

                    Level += 1.0 / (AttackSeconds * SampleRate);

                    if (Level >= 1.0)
                    {
                        Level = 1.0;
                        Stage = EnvelopeStage.Decay;
                    }

                    break;

                case EnvelopeStage.Decay:

                    Level -= (1.0 - SustainLevel) / (DecaySeconds * SampleRate);

                    if (Level <= SustainLevel)
                    {
                        Level = SustainLevel;
                        Stage = EnvelopeStage.Sustain;
                    }

                    break;

                case EnvelopeStage.Sustain:
                    break;

                case EnvelopeStage.Release:

                    Level -= SustainLevel / (ReleaseSeconds * SampleRate);

                    if (Level <= 0.0)
                    {
                        Level = 0.0;
                        Stage = EnvelopeStage.Off;
                    }

                    break;
            }

            return Level;
        }

        public void Reset()
        {
            Level = 0;
            Stage = EnvelopeStage.Off;
        }

        public AdsrEnvelope Copy(bool deepCopy = false)
        {
            return new AdsrEnvelope(SampleRate)
            {
                AttackSeconds = AttackSeconds,
                DecaySeconds = DecaySeconds,
                SustainLevel = SustainLevel,
                ReleaseSeconds = ReleaseSeconds
            };
        }

        object ICopyable.Copy(bool deepCopy)
        {
            return Copy(deepCopy);
        }
    }
}
