using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.DigitalSignalProcessing;

namespace Toy_Synthesizer.Game.Midi
{
    public static class MidiUtils
    {
        public const int A4_CENTER_NOTE = 69;
        public const double A4_CENTER_FREQUENCY = 440; // TODO: Subject to change; may make static or something.
        private const double STANDARD_A4_CENTER_FREQUENCY = 440;

        public const int DEFAULT_ROUNDING_PRECISION = 2;

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

        public static MidiNote ShiftOctaveBy(MidiNote note, int octaveAmount)
        {
            double frequency = GetFrequency(note);

            double shiftedFrequency = DSPUtils.ShiftOctaveBy(frequency, octaveAmount);

            return MatchFrequency_Raw(shiftedFrequency);
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

        public static double GetFrequency(MidiNote note, int precision = DEFAULT_ROUNDING_PRECISION)
        {
            double frequency = A4_CENTER_FREQUENCY * Math.Pow(2.0, ((int)note - A4_CENTER_NOTE) / 12.0);

            double rounded = GeoMath.RoundAwayFromZero(frequency, precision);

            return rounded;
        }

        public static bool TryMatchFrequency(double frequency, out MidiNote note,
                                             int precsion = DEFAULT_ROUNDING_PRECISION)
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

        /// <summary>
        /// Maps <paramref name="frequency"/> to a <see cref="MidiNote"/>.
        /// 
        /// <br></br>
        /// <br></br>
        /// 
        /// This rounds to the nearest MIDI note, so if <paramref name="frequency"/> does not exactly match a note, this may produce unexpected results.
        /// 
        /// <br></br>
        /// <br></br>
        /// 
        /// See also: <see cref="TryMatchFrequency(double, out MidiNote, int)"/>
        /// 
        /// </summary>
        /// 
        public static MidiNote MatchFrequency_Raw(double frequency, int precision = DEFAULT_ROUNDING_PRECISION)
        {
            double midiNote = GeoMath.RoundAwayFromZero(12.0 * Math.Log2(frequency / A4_CENTER_FREQUENCY) + 69, precision);

            return (MidiNote)midiNote;
        }

        /// <summary>
        /// Scales <paramref name="frequency"/> by <code>(<see cref="A4_CENTER_FREQUENCY"/> / <see cref="STANDARD_A4_CENTER_FREQUENCY"/></code>
        /// </summary>
        public static double ScaleFrequencyByA4(double frequency)
        {
            return frequency * (A4_CENTER_FREQUENCY / STANDARD_A4_CENTER_FREQUENCY);
        }
    }
}
