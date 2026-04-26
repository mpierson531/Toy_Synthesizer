using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoInput;
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
    // TODO: Implement more advanced UI widget support for utilities
    public class VoiceFrontend
    {
        public const double DEFAULT_SHIFT_SEMITONE_AMOUNT = 12.0;
        public const double DEFAULT_CONTROL_SEMITONE_AMOUNT = -12.0;
        public static readonly NumberRange<double> ShiftAndControlSemitoneRange;
        private static readonly Dictionary<Voice, KeyBinding> defaultKeyVoiceBindings;
        public static readonly ImmutableArray<Keys> InvalidVoiceKeybindingKeys;

        static VoiceFrontend()
        {
            ShiftAndControlSemitoneRange = NumberRange<double>.From(-24.0, 24.0);

            defaultKeyVoiceBindings = GetDefaultKeyVoiceBindings();

            InvalidVoiceKeybindingKeys = new ImmutableArray<Keys>(new Keys[]
            {
                Keys.Enter,
                Keys.Escape
            });
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

        private readonly Dictionary<Voice, KeyBinding> voiceKeybindings;
        private readonly Dictionary<Voice, bool> voiceKeybindingsInputHandled;

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

            voiceKeybindings = new Dictionary<Voice, KeyBinding>(defaultKeyVoiceBindings.Count);
            voiceKeybindingsInputHandled = new Dictionary<Voice, bool>();

            foreach ((Voice voice, KeyBinding keybinding) in defaultKeyVoiceBindings)
            {
                Voice voiceCopy = voice.Copy(deepCopy: true);

                voiceKeybindings.Add(voiceCopy, keybinding);

                voiceKeybindingsInputHandled.Add(voiceCopy, false);
            }

            foreach (Voice voice in voiceKeybindings.Keys)
            {
                voice.LPF = new StateVariableLPF(20000, 0.25, game.Synthesizer.SampleRate);

                AudioSourceCommand addVoiceCommand = SynthesizerCommands.AddVoice(voice);

                game.Synthesizer.SendCommand(ref addVoiceCommand);
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

        public void BeginInputEvents()
        {

        }

        public void EndInputEvents()
        {

        }

        public bool KeyDown(Keys key, bool isRepeat, float holdTime)
        {
            if (!isRepeat && TryTurnVoicesOn(key))
            {
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
            // Even if TryTurnVoiceOff is successful here, not returning.

            TryTurnVoicesOff(key);

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

        private bool TryTurnVoicesOn(Keys key)
        {
            bool anyTurnedOn = false;

            foreach ((Voice voice, KeyBinding keybinding) in voiceKeybindings)
            {
                GeoDebug.Assert(!keybinding.RespectRepeatDelay && keybinding.RepeatDelay == 0f && keybinding.HoldDelay == 0f);

                if (!voiceKeybindingsInputHandled[voice] && keybinding.IsPressed(game.Geo.Input.keyboard, key))
                {
                    TurnVoiceOn(voice);

                    if (!anyTurnedOn)
                    {
                        anyTurnedOn = true;
                    }

                    voiceKeybindingsInputHandled[voice] = true;
                }
            }

            return anyTurnedOn;
        }

        private bool TryTurnVoicesOff(Keys key)
        {
            bool anyTurnedOff = false;

            foreach ((Voice voice, KeyBinding keybinding) in voiceKeybindings)
            {
                GeoDebug.Assert(!keybinding.RespectRepeatDelay && keybinding.RepeatDelay == 0f && keybinding.HoldDelay == 0f);

                if (keybinding.IsPressed(game.Geo.Input.keyboard, key))
                {
                    if (!voiceKeybindingsInputHandled[voice])
                    {
                        voiceKeybindingsInputHandled[voice] = true;
                    }

                    continue;
                }

                ReadOnlySpan<KeyBinding.Key> keybindingKeysSpan = keybinding.KeysSpan;

                for (int keyIndex = 0; keyIndex < keybindingKeysSpan.Length; keyIndex++)
                {
                    if (keybindingKeysSpan[keyIndex].key == key)
                    {
                        TurnVoiceOff(voice);

                        if (!anyTurnedOff)
                        {
                            anyTurnedOff = true;
                        }
                    }
                }

                voiceKeybindingsInputHandled[voice] = false;
            }

            return anyTurnedOff;
        }

        private void TurnVoiceOn(Voice voice)
        {
            AudioSourceCommand voiceOnCommand = SynthesizerCommands.VoiceOn(voice);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, voiceOnCommand);
        }

        private void TurnVoiceOff(Voice voice)
        {
            AudioSourceCommand voiceOffCommand = SynthesizerCommands.VoiceOff(voice);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, voiceOffCommand);
        }

        public void SetVoiceKeybinding(Voice voice, KeyBinding keybinding)
        {
            foreach ((Voice existingVoice, _) in voiceKeybindings)
            {
                if (existingVoice == voice)
                {
                    TurnVoiceOff(voice);

                    voiceKeybindings.Remove(existingVoice);

                    break;
                }
            }

            if (keybinding is null)
            {
                return;
            }

            voiceKeybindings[voice] = keybinding;
            voiceKeybindingsInputHandled[voice] = false;

            if (keybinding.IsPressed(game.Geo.Input.keyboard))
            {
                TurnVoiceOn(voice);

                voiceKeybindingsInputHandled[voice] = true;
            }
        }

        public bool TryFindVoiceKeybinding(Voice voice, out KeyBinding keybinding)
        {
            return voiceKeybindings.TryGetValue(voice, out keybinding);
        }

        public void InitUI(UIManager uiManager)
        {
            string utilitiesItemsNames = "[";

            for (int index = 0; index < utilityActionNames.Length; index++)
            {
                utilitiesItemsNames += utilityActionNames[index];

                if (index + 1 < utilityActionNames.Length)
                {
                    utilitiesItemsNames += ", ";
                }
            }

            utilitiesItemsNames += "]";

            string xml = $@"
<Layout>

    <Window Title=""Voices"" X=""50"" Y=""50"" W=""25%"" H=""60%"">

        <ScrollPane X=""0%"" Y=""7.5%"" W=""100%"" H=""87.5%""/>

        <DropDown Position=""(2.5%, 1%)""
                  Size=""(17.5%, 5%)"" 
                  DropDownWidth=""350%""
                  ButtonSize=""(325%, 100%)""
                  Items=""{utilitiesItemsNames}""
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
            actionNames = new string[] 
            { 
                "Match notes (from frequency",
                "Match notes (from voice name)",

                "Shift all by 1 octave",
                "Shfit all by -1 octave"
            };

            actions = new Action[] 
            { 
                MatchNotes_FromFrequency,
                MatchNotes_FromName,

                ShiftAllByOctave_One,
                ShiftAllByOctave_MinusOne };
        }

        // TODO: In the MatchNotes_ methods, I'm locking. Maybe find another way without locking.
        private void MatchNotes_FromFrequency()
        {
            void MatchNoteFrequency(VoiceGroup voiceGroup)
            {
                Utils.Assert(synthesizer.ContainsVoice(voiceGroup.Voice), "voice was not contained in the synthesizer. This should never be reached!");

                TryMatchNote_FromFrequency(voiceGroup);
            }

            lock (lockObject)
            {
                voicesGroup.ForEachOfType<VoiceGroup>(MatchNoteFrequency);
            }
        }

        private void MatchNotes_FromName()
        {
            void MatchNoteName(VoiceGroup voiceGroup)
            {
                Utils.Assert(synthesizer.ContainsVoice(voiceGroup.Voice), "voice was not contained in the synthesizer. This should never be reached!");

                TryMatchNote_FromName(voiceGroup);
            }

            lock (lockObject)
            {
                voicesGroup.ForEachOfType<VoiceGroup>(MatchNoteName);
            }
        }

        private void ShiftAllByOctave_One()
        {
            ShiftAllByOctave(1);
        }

        private void ShiftAllByOctave_MinusOne()
        {
            ShiftAllByOctave(-1);
        }

        private void ShiftAllByOctave(int octaveAmount)
        {
            void ShiftOctave(VoiceGroup voiceGroup)
            {
                Utils.Assert(synthesizer.ContainsVoice(voiceGroup.Voice), "voice was not contained in the synthesizer. This should never be reached!");

                double newFrequency = DSPUtils.ShiftOctave(voiceGroup.Voice.CenterFrequency, octaveAmount);

                voiceGroup.SetFrequency(newFrequency);

                TryMatchNote_FromFrequency(voiceGroup, newFrequency);
            }

            lock (lockObject)
            {
                voicesGroup.ForEachOfType<VoiceGroup>(ShiftOctave);
            }
        }

        private static void TryMatchNote_FromFrequency(VoiceGroup voiceGroup)
        {
            TryMatchNote_FromFrequency(voiceGroup, voiceGroup.Voice.CenterFrequency);
        }

        private static void TryMatchNote_FromFrequency(VoiceGroup voiceGroup, double centerFrequency)
        {
            if (MidiUtils.TryMatchFrequency(centerFrequency, out MidiNote note))
            {
                voiceGroup.SetVoiceName(note.ToString());
            }
        }

        private static void TryMatchNote_FromName(VoiceGroup voiceGroup)
        {
            if (MidiUtils.TryMatchNoteName(voiceGroup.Voice.Name, out MidiNote note))
            {
                voiceGroup.SetFrequency(MidiUtils.GetFrequency(note));
            }
        }

        private const string VoiceUtilitiesDropDownName = "VoiceUtiltiesDropDown";

        private static Dictionary<Voice, KeyBinding> GetDefaultKeyVoiceBindings()
        {
            MidiNote startNote = MidiNote.C4;

            int[] semitoneOffsets = { 0, 2, 4, 5, 7, 9, 11 }; // C, D, E, F, G, A, B

            int startOctave = MidiUtils.GetOctave(startNote);
            int additionalLayerCount = 0;

            ViewableList<ValueTuple<Voice, KeyBinding>> voices = new ViewableList<ValueTuple<Voice, KeyBinding>>();

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

                for (int octaveLayer = startOctave; octaveLayer < startOctave + 1 + additionalLayerCount; octaveLayer++)
                {
                    KeyBinding keybinding = new KeyBinding(modifiers: null, keys: new KeyBinding.Key[] { new KeyBinding.Key(key, PressMode.Down) },
                                                       holdDelay: 0,
                                                       repeatDelay: 0,
                                                       respectRepeatDelay: false);

                    int noteValue = (octaveLayer + 1) * 12 + offset;

                    MidiNote midiNote = (MidiNote)noteValue;

                    Voice voice = Voice.FromMidi(midiNote);

                    voices.Add((voice, keybinding));
                }
            }

            return voices.ToDictionary(x => x.Item1, x => x.Item2);
        }
    }
}
