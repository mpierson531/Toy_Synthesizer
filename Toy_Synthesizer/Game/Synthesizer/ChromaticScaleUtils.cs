using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toy_Synthesizer.Game.Midi;

namespace Toy_Synthesizer.Game.Synthesizer
{
    public static class ChromaticScaleUtils
    {
        private static readonly Note[] allNotes = GenerateNotesFromMidiNotes();

        public const double SEMITONE_RATIO = 1.0594630943592953;

        public static double SemitonesToPitchRatio(double semitones)
        {
            return Math.Pow(2.0, semitones / 12.0);
        }

        public static double PitchRatioToSemitones(double pitchRatio)
        {
            return 12.0 * Math.Log(pitchRatio, 2.0);
        }

        public static double SemitoneUp(double frequency)
        {
            return frequency * SEMITONE_RATIO;
        }

        public static double SemitoneDown(double frequency)
        {
            return frequency / SEMITONE_RATIO;
        }

        public static double GetFrequency(double baseCenterFrequency, int octave)
        {
            return baseCenterFrequency * Math.Pow(2.0, octave);
        }

        public static Note GetNoteFromMidi(MidiNote midiNote)
        {
            return allNotes[(int)midiNote - 12];
        }

        private static Note[] GenerateNotesFromMidiNotes()
        {
            List<Note> notes = new List<Note>();

            const MidiNote startMidi = MidiNote.C0;
            const MidiNote endMidi = MidiNote.G9;

            for (int midi = (int)startMidi; midi <= (int)endMidi; midi++)
            {
                int semitone = MidiUtils.GetSemitone(midi);
                int octave = MidiUtils.GetOctave(midi);

                Key key = (Key)semitone;
                double frequency = MidiUtils.GetFrequency((MidiNote)midi);

                notes.Add(new Note(key, frequency, octave));
            }

            return notes.ToArray();
        }
    }
}
