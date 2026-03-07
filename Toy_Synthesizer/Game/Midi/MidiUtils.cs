using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toy_Synthesizer.Game.Midi
{
    public static class MidiUtils
    {
        public const int A4_CENTER_NOTE = 69;
        public const double A4_CENTER_FREQUENCY = 440.0;

        public static MidiNote ShiftOctave(MidiNote note, int targetOctave)
        {
            int semitone = GetSemitone(note);

            int shiftedNote = (targetOctave + 1) * 12 + semitone;

            return (MidiNote)shiftedNote;
        }

        public static int GetSemitone(MidiNote note)
        {
            return GetSemitone((int)note);
        }

        public static int GetSemitone(int midiNote)
        {
            return midiNote % 12;
        }

        public static int GetOctave(MidiNote note)
        {
            return GetOctave((int)note);
        }

        public static int GetOctave(int midiNote)
        {
            return midiNote / 12 - 1;
        }

        public static double GetFrequency(MidiNote note, int precision = 2)
        {
            double frequency = A4_CENTER_FREQUENCY * Math.Pow(2.0, ((int)note - A4_CENTER_NOTE) / 12.0);

            double rounded = Math.Round(frequency, precision, MidpointRounding.AwayFromZero);

            return rounded;
        }
    }
}
