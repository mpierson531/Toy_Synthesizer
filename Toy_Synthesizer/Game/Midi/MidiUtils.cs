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

        public const StringComparison DEFAULT_STRING_COMPARISON_TYPE = StringComparison.OrdinalIgnoreCase;

        public static readonly ImmutableArray<MidiNote> AllMidiNotes;
        public static readonly ImmutableArray<string> AllMidiNoteNames;
        public static readonly ImmutableArray<string> AllMidiNoteNames_Shorthand;

        static MidiUtils()
        {
            AllMidiNotes = new ImmutableArray<MidiNote>(Enum.GetValues<MidiNote>());

            string[] allMidiNoteNames_Array = new string[AllMidiNotes.Count];
            string[] allMidiNoteNames_Shorthand_Array = new string[AllMidiNotes.Count];

            for (int index = 0; index < AllMidiNotes.Count; index++)
            {
                allMidiNoteNames_Array[index] = AllMidiNotes[index].ToString();
            }

            AllMidiNoteNames = new ImmutableArray<string>(allMidiNoteNames_Array);

            // TODO: Maybe implement flat accidentals rather than only sharps.

            for (int index = 0; index < AllMidiNotes.Count; index++)
            {
                string currentNoteName = AllMidiNoteNames[index];

                char letterName = currentNoteName[0];

                int noteNameOctaveBeginIndex;

                if (currentNoteName.Contains("sharp", StringComparison.OrdinalIgnoreCase))
                {
                    noteNameOctaveBeginIndex = currentNoteName.IndexOf("sharp", StringComparison.OrdinalIgnoreCase) + "sharp".Length;
                }
                else
                {
                    noteNameOctaveBeginIndex = 1;
                }

                string octaveName = currentNoteName.Substring(noteNameOctaveBeginIndex);

                bool isMinusOctave = currentNoteName.Contains("minus", StringComparison.OrdinalIgnoreCase);

                string shorthand;

                if (currentNoteName.Contains("sharp", StringComparison.OrdinalIgnoreCase))
                {
                    shorthand = $"{letterName}#";
                }
                else
                {
                    shorthand = $"{letterName}";
                }

                if (isMinusOctave)
                {
                    string minusOctave = octaveName.Substring("minus".Length);

                    shorthand += $"-{minusOctave}";
                }
                else
                {
                    shorthand += octaveName;
                }

                allMidiNoteNames_Shorthand_Array[index] = shorthand;
            }

            AllMidiNoteNames_Shorthand = new ImmutableArray<string>(allMidiNoteNames_Shorthand_Array);
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
                                            StringComparison stringComparisonType = DEFAULT_STRING_COMPARISON_TYPE)
        {
            if (trim)
            {
                name = name.Trim();
            }

            for (int nameIndex = 0; nameIndex < AllMidiNotes.Count; nameIndex++)
            {
                string currentNote = AllMidiNoteNames[nameIndex];

                if (name.Equals(currentNote, stringComparisonType))
                {
                    note = AllMidiNotes[nameIndex];

                    return true;
                }
            }

            for (int shorthandNameIndex = 0; shorthandNameIndex < AllMidiNotes.Count; shorthandNameIndex++)
            {
                string currentNote = AllMidiNoteNames_Shorthand[shorthandNameIndex];

                if (name.Equals(currentNote, stringComparisonType))
                {
                    note = AllMidiNotes[shorthandNameIndex];

                    return true;
                }
            }

            note = default;

            return false;
        }
    }
}
