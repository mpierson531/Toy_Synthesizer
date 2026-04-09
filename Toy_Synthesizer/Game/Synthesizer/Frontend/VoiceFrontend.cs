using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Microsoft.Xna.Framework.Input;

using Toy_Synthesizer.Game.DigitalSignalProcessing;
using Toy_Synthesizer.Game.Midi;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Console;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend
{
    public class VoiceFrontend
    {
        public const double DEFAULT_SHIFT_SEMITONE_AMOUNT = 12.0;
        public const double DEFAULT_CONTROL_SEMITONE_AMOUNT = -12.0;
        public static readonly NumberRange<double> ShiftAndControlSemitoneRange;
        private static readonly Dictionary<Keys, ImmutableArray<Voice>> defaultKeyVoiceBindings;

        static VoiceFrontend()
        {
            ShiftAndControlSemitoneRange = NumberRange<double>.From(-24.0, 24.0);

            defaultKeyVoiceBindings = GetDefaultKeyVoiceBindings();
        }

        private readonly Game game;

        private readonly PolyphonicSynthesizer synthesizer;

        private readonly UIXmlParser uiXmlParser;

        private GroupWidget voicesGroup;

        private DropDownWidget voiceUtilitiesDropDown;

        private readonly object lockObject = new object();

        private double shiftSemitoneAmount;
        private double controlSemitoneAmount;

        private double? currentShiftShiftedSemitoneAmount;
        private double? currentControlShiftedSemitoneAmount;

        private readonly Dictionary<Keys, ViewableList<Voice>> keyVoiceBindings;

        private readonly string[] utilityActionNames;
        private readonly Action[] utilityActions;

        public double ShiftSemitoneAmount
        {
            get => shiftSemitoneAmount;

            set
            {
                value = ShiftAndControlSemitoneRange.Clamp(value);

                double delta = value - shiftSemitoneAmount;

                shiftSemitoneAmount = value;

                if (currentShiftShiftedSemitoneAmount.HasValue)
                {
                    game.Synthesizer.GlobalVoicePitchShiftSemitones += delta;

                    currentShiftShiftedSemitoneAmount += delta;
                }

                OnShiftSemitoneAmountChanged?.Invoke(shiftSemitoneAmount);
            }
        }

        public double ControlSemitoneAmount
        {
            get => controlSemitoneAmount;

            set
            {
                value = ShiftAndControlSemitoneRange.Clamp(value);

                double delta = value - controlSemitoneAmount;

                controlSemitoneAmount = value;

                if (currentControlShiftedSemitoneAmount.HasValue)
                {
                    game.Synthesizer.GlobalVoicePitchShiftSemitones += delta;

                    currentControlShiftedSemitoneAmount += delta;
                }

                OnControlSemitoneAmountChanged?.Invoke(controlSemitoneAmount);
            }
        }

        public Action<double> OnShiftSemitoneAmountChanged;
        public Action<double> OnControlSemitoneAmountChanged;

        public VoiceFrontend(Game game, UIXmlParser uiXmlParser)
        {
            this.game = game;

            this.synthesizer = game.Synthesizer;

            this.uiXmlParser = uiXmlParser;

            currentShiftShiftedSemitoneAmount = null;
            currentControlShiftedSemitoneAmount = null;

            ShiftSemitoneAmount = DEFAULT_SHIFT_SEMITONE_AMOUNT;
            ControlSemitoneAmount = DEFAULT_CONTROL_SEMITONE_AMOUNT;

            keyVoiceBindings = new Dictionary<Keys, ViewableList<Voice>>(defaultKeyVoiceBindings.Count);

            foreach (KeyValuePair<Keys, ImmutableArray<Voice>> binding in defaultKeyVoiceBindings)
            {
                Voice[] voicesCopy = binding.Value.ToArray(voice => voice.Copy(deepCopy: true));

                keyVoiceBindings.Add(binding.Key, new ViewableList<Voice>(voicesCopy));
            }

            foreach (ViewableList<Voice> voices in keyVoiceBindings.Values)
            {
                for (int index = 0; index < voices.Count; index++)
                {
                    voices[index].LPF = new StateVariableLPF(20000, 0.25, game.Synthesizer.SampleRate);
                }

                for (int index = 0; index < voices.Count; index++)
                {
                    AudioSourceCommand addVoiceCommand = SynthesizerCommands.AddVoice(voices[index]);

                    game.Synthesizer.SendCommand(ref addVoiceCommand);
                }
            }

            // Distribute the oscillator amplitudes equally, to ensure a decent volume.
            AudioSourceCommand forEachVoiceCommand = SynthesizerCommands.ForEachVoiceAction(delegate (Voice voice)
            {
                if (voice.Oscillators.Count > 1)
                {
                    voice.Oscillators.ForEach(oscillator => oscillator.Amplitude /= voice.Oscillators.Count);
                }
            });

            game.Synthesizer.SendCommand(ref forEachVoiceCommand);

            InitUtilityActions(out utilityActionNames, out utilityActions);
        }

        public bool KeyDown(Keys key, bool isRepeat, float holdTime)
        {
            if (!isRepeat && keyVoiceBindings.TryGetValue(key, out ViewableList<Voice> voices))
            {
                for (int index = 0; index < voices.Count; index++)
                {
                    AudioSourceCommand voiceOnCommand = SynthesizerCommands.VoiceOn(voices[index]);

                    game.DSP.SendAudioSourceCommand(game.Synthesizer, voiceOnCommand);
                }

                return true;
            }

            if (!isRepeat && (key == Keys.LeftShift || key == Keys.RightShift) && !currentShiftShiftedSemitoneAmount.HasValue)
            {
                currentShiftShiftedSemitoneAmount = ShiftSemitoneAmount;

                game.Synthesizer.GlobalVoicePitchShiftSemitones += ShiftSemitoneAmount;

                return true;
            }

            if (!isRepeat && (key == Keys.LeftControl || key == Keys.RightControl) && !currentControlShiftedSemitoneAmount.HasValue)
            {
                currentControlShiftedSemitoneAmount = ControlSemitoneAmount;

                game.Synthesizer.GlobalVoicePitchShiftSemitones += ControlSemitoneAmount;

                return true;
            }

            return false;
        }

        public bool KeyUp(Keys key)
        {
            if (keyVoiceBindings.TryGetValue(key, out ViewableList<Voice> voices))
            {
                for (int index = 0; index < voices.Count; index++)
                {
                    AudioSourceCommand voiceOffCommand = SynthesizerCommands.VoiceOff(voices[index]);

                    game.DSP.SendAudioSourceCommand(game.Synthesizer, voiceOffCommand);
                }
            }

            if (key == Keys.LeftShift && !game.Geo.Input.keyboard.IsKeyDown(Keys.RightShift)
                || key == Keys.RightShift && !game.Geo.Input.keyboard.IsKeyDown(Keys.LeftShift))
            {
                game.Synthesizer.GlobalVoicePitchShiftSemitones -= currentShiftShiftedSemitoneAmount.Value;

                currentShiftShiftedSemitoneAmount = null;

                return true;
            }

            if (key == Keys.LeftControl && !game.Geo.Input.keyboard.IsKeyDown(Keys.RightControl)
                || key == Keys.RightControl && !game.Geo.Input.keyboard.IsKeyDown(Keys.LeftControl))
            {
                game.Synthesizer.GlobalVoicePitchShiftSemitones -= currentControlShiftedSemitoneAmount.Value;

                currentControlShiftedSemitoneAmount = null;

                return true;
            }

            return false;
        }

        public void AddVoiceKeyBinding(Keys key, Voice voice)
        {
            if (keyVoiceBindings.TryGetValue(key, out ViewableList<Voice> currentKeyVoices))
            {
                currentKeyVoices.Add(voice);
            }
            else
            {
                keyVoiceBindings.Add(key, new ViewableList<Voice>(voice));
            }
        }

        public void InitUI(UIManager uiManager)
        {
            string xml = $@"
<Layout>

    <Window Title=""Voices"" X=""50"" Y=""50"" W=""25%"" H=""60%"">

        <ScrollPane X=""0%"" Y=""7.5%"" W=""100%"" H=""87.5%""/>

        <DropDown Position=""(2.5%, 1%)""
                  Size=""(17.5%, 5%)"" 
                  DropDownWidth=""350%""
                  ButtonSize=""(325%, 100%)""
                  Items=""[Match Notes (by frequency), Match Notes (by name)]""
                  CoverButtonText=""Utilities""
                  Name=""{VoiceUtilitiesDropDownName}""/>

    </Window>

</Layout>";


            ViewableList<Widget> widgets = uiXmlParser.Parse(xml);

            Window window = (Window)widgets[0];

            voicesGroup = (GroupWidget)window[window.ComputeEffectiveChildBeginOffset()];

            float currentY = voicesGroup.Position.Y;

            AudioSourceCommand forEachVoiceCommand = SynthesizerCommands.ForEachVoiceAction(delegate (Voice voice)
            {
                InitVoiceGroup(voice, ref currentY, offsetYFirst: false);
            });

            synthesizer.SendCommand(ref forEachVoiceCommand);

            game.AddUIWidgets(widgets);

            synthesizer.OnVoiceAdded += delegate (PolyphonicSynthesizer polyphonic, Voice voice)
            {
                float currentY = voicesGroup[voicesGroup.Count - 1].Position.Y;

                InitVoiceGroup(voice, ref currentY, offsetYFirst: true);
            };

            voiceUtilitiesDropDown = window.FindAsByNameDeepSearch<DropDownWidget>(VoiceUtilitiesDropDownName);

            voiceUtilitiesDropDown.OnSelect += OnVoiceUtilitySelect;
        }

        private void OnVoiceUtilitySelect(Button _, int index)
        {
            Action action = utilityActions[index];

            action();
        }

        private void InitVoiceGroup(Voice voice, ref float currentY, bool offsetYFirst)
        {
            float scrollPaneGroupSpacing = voicesGroup.Size.Min() * 0.02f;

            float groupX = voicesGroup.Position.X + scrollPaneGroupSpacing;
            float groupY = currentY + scrollPaneGroupSpacing;
            float groupW = voicesGroup.Size.X * 0.925f;
            float groupH = voicesGroup.Size.Y * 0.35f;

            if (offsetYFirst)
            {
                currentY += groupH + voicesGroup.Size.Y * 0.1f;
            }

            string xml = "<Layout>" + Environment.NewLine;

            xml +=
            $@"
                    <VoiceGroup X=""{groupX}"" Y=""{groupY}"" W=""{groupW}"" H=""{groupH}""/>
            ";

            currentY += groupH + voicesGroup.Size.Y * 0.05f;

            xml += Environment.NewLine + "</Layout>";

            ViewableList<Widget> voiceGroups = uiXmlParser.Parse(xml);

            voiceGroups.ForEach(voiceGroup =>
            {
                ((VoiceGroup)voiceGroup).Voice = voice;
            });

            voicesGroup.AddChildRange(voiceGroups);
        }

        private void InitUtilityActions(out string[] actionNames, out Action[] actions)
        {
            actionNames = new string[] { "Match notes (from frequency", "Match notes (from voice name)" };

            actions = new Action[] { MatchNotes_FromFrequency, MatchNotes_FromName };
        }

        // TODO: In the MatchNotes_ methods, I'm locking. Maybe find another way without locking.
        private void MatchNotes_FromFrequency()
        {
            void MatchNoteFrequency(VoiceGroup voiceGroup)
            {
                Utils.Assert(synthesizer.ContainsVoice(voiceGroup.Voice), "voice was not contained in the synthesizer. This should never be reached!");

                if (MidiUtils.TryMatchFrequency(voiceGroup.Voice.CenterFrequency, out MidiNote note))
                {
                    voiceGroup.SetVoiceName(note.ToString());
                }
            }

            lock (lockObject)
            {
                voicesGroup.ForEachOfType<VoiceGroup>(MatchNoteFrequency);
            }
        }

        private void MatchNotes_FromName()
        {
            void MatchNoteFrequency(VoiceGroup voiceGroup)
            {
                Utils.Assert(synthesizer.ContainsVoice(voiceGroup.Voice), "voice was not contained in the synthesizer. This should never be reached!");

                if (MidiUtils.TryMatchNoteName(voiceGroup.Voice.Name, out MidiNote note))
                {
                    voiceGroup.SetFrequency(MidiUtils.GetFrequency(note));
                }
            }

            lock (lockObject)
            {
                voicesGroup.ForEachOfType<VoiceGroup>(MatchNoteFrequency);
            }
        }

        private const string VoiceUtilitiesDropDownName = "VoiceUtiltiesDropDown";

        // TODO: Finish
        /*private void DropAllByOctave()
        {
            void DropFrequenciesByOctave(VoiceGroup voiceGroup)
            {
                Utils.Assert(synthesizer.ContainsVoice(voiceGroup.Voice), "voice was not contained in the synthesizer. This should never be reached!");

                int currentOctave = MidiUtils.GetOctave(voiceGroup.Voice.CenterFrequency);
            }
        }*/

        private static Dictionary<Keys, ImmutableArray<Voice>> GetDefaultKeyVoiceBindings()
        {
            MidiNote startNote = MidiNote.C4;

            int[] semitoneOffsets = { 0, 2, 4, 5, 7, 9, 11 }; // C, D, E, F, G, A, B

            int startOctave = MidiUtils.GetOctave(startNote);
            int additionalLayerCount = 0;

            ViewableList<ValueTuple<Keys, ImmutableArray<Voice>>> voices = new ViewableList<ValueTuple<Keys, ImmutableArray<Voice>>>();

            foreach (int offset in semitoneOffsets)
            {
                Keys key = offset switch
                {
                    0 => Keys.C,
                    2 => Keys.D,
                    4 => Keys.E,
                    5 => Keys.F,
                    7 => Keys.G,
                    9 => Keys.A,
                    11 => Keys.B,

                    _ => throw new Exception("Bad semitone offset. This should never be reached!")
                };

                ViewableList<Voice> voiceList = new ViewableList<Voice>();

                for (int octaveLayer = startOctave; octaveLayer < startOctave + 1 + additionalLayerCount; octaveLayer++)
                {
                    int noteValue = (octaveLayer + 1) * 12 + offset;

                    MidiNote midiNote = (MidiNote)noteValue;

                    voiceList.Add(Voice.FromMidi(midiNote));
                }

                ValueTuple<Keys, ImmutableArray<Voice>> binding = (key, new ImmutableArray<Voice>(voiceList.ToArray()));

                voices.Add(binding);
            }

            return voices.ToDictionary(x => x.Item1, x => x.Item2);
        }
    }
}
