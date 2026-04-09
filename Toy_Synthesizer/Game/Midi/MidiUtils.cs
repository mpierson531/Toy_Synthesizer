using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

namespace Toy_Synthesizer.Game.Midi
{
    public static class MidiUtils
    {
        public const int A4_CENTER_NOTE = 69;
        public const double A4_CENTER_FREQUENCY = 440.0;

        public static readonly ImmutableArray<MidiNote> AllMidiNotes;
        public static readonly ImmutableArray<string> AllMidiNoteNames;

        static MidiUtils()
        {
            AllMidiNotes = new ImmutableArray<MidiNote>(Enum.GetValues<MidiNote>());

            string[] allMidiNoteNames = new string[AllMidiNotes.Count];

            for (int index = 0; index < AllMidiNotes.Count; index++)
            {
                allMidiNoteNames[index] = AllMidiNotes[index].ToString();
            }

            AllMidiNoteNames = new ImmutableArray<string>(allMidiNoteNames);
        }

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

            double rounded = GeoMath.RoundAwayFromZero(frequency, precision);

            return rounded;
        }

        public static bool TryMatchFrequency(double frequency, out MidiNote note,
                                             int precsion = 2)
        {
            frequency = GeoMath.RoundAwayFromZero(frequency, precsion);

            for (int index = 0; index < AllMidiNotes.Count; index++)
            {
                MidiNote currentNote = AllMidiNotes[index];

                double noteFrequency = GetFrequency(currentNote, precsion);

                if (GeoMath.Equals_Precise(frequency, noteFrequency))
                {
                    note = currentNote;

                    return true;
                }
            }

            note = default;

            return false;
        }

        // Default string comparison tyhpe is OrdinalIgnoreCase.
        public static bool TryMatchNoteName(string name, out MidiNote note,
                                            bool trim = true,
                                            StringComparison stringComparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (trim)
            {
                name = name.Trim();
            }

            for (int index = 0; index < AllMidiNotes.Count; index++)
            {
                string currentNote = AllMidiNoteNames[index];

                if (name.Equals(currentNote, stringComparisonType))
                {
                    note = AllMidiNotes[index];

                    return true;
                }
            }

            note = default;

            return false;
        }
    }
}
