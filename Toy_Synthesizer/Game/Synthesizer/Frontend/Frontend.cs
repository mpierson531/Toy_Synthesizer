using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoInput;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Toy_Synthesizer.Game.Data;
using Toy_Synthesizer.Game.Data.Generic;
using Toy_Synthesizer.Game.Midi;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.Synthesizer.Backend.BuiltinAudioEffects;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Console;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend
{
    public class Frontend
    {
        public const double DEFAULT_SHIFT_SEMITONE_AMOUNT = 12.0;
        public const double DEFAULT_CONTROL_SEMITONE_AMOUNT = -12.0;
        public static readonly NumberRange<double> ShiftAndControlSemitoneRange;
        private static readonly Dictionary<Keys, Voice[]> defaultKeyVoiceBindings;

        static Frontend()
        {
            ShiftAndControlSemitoneRange = NumberRange<double>.From(-24.0, 24.0);

            defaultKeyVoiceBindings = GetDefaultKeyVoiceBindings();
        }

        private readonly UIXmlParser uiXmlParser;

        private readonly Game game;

        private readonly Backend.Backend backend;

        private readonly Dictionary<Keys, Voice[]> keyVoiceBindings;

        private readonly ViewableList<Property<Voice>> voiceProperties;

        private readonly Console.Console console;

        private GroupWidget voicesGroup;

        private double shiftSemitoneAmount;
        private double controlSemitoneAmount;

        private double? currentShiftShiftedSemitoneAmount;
        private double? currentControlShiftedSemitoneAmount;

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
                    backend.PolyphonicSynthesizer.GlobalVoicePitchShiftSemitones += delta;

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
                    backend.PolyphonicSynthesizer.GlobalVoicePitchShiftSemitones += delta;

                    currentControlShiftedSemitoneAmount += delta;
                }

                OnControlSemitoneAmountChanged?.Invoke(controlSemitoneAmount);
            }
        }

        public Action<double> OnShiftSemitoneAmountChanged;
        public Action<double> OnControlSemitoneAmountChanged;

        public Frontend(Game game, Backend.Backend backend)
        {
            this.backend = backend;
            this.game = game;

            this.uiXmlParser = new UIXmlParser(game.UIManager);

            currentShiftShiftedSemitoneAmount = null;
            currentControlShiftedSemitoneAmount = null;

            ShiftSemitoneAmount = DEFAULT_SHIFT_SEMITONE_AMOUNT;
            ControlSemitoneAmount = DEFAULT_CONTROL_SEMITONE_AMOUNT;

            AddUIParserTypeFactories();
            AddUIManagerStyles();

            keyVoiceBindings = new Dictionary<Keys, Voice[]>(defaultKeyVoiceBindings.Count);

            foreach (KeyValuePair<Keys, Voice[]> binding in defaultKeyVoiceBindings)
            {
                Voice[] voicesCopy = ArrayUtils.ArrayCopy(binding.Value, voice => voice.Copy(deepCopy: true));

                keyVoiceBindings.Add(binding.Key, voicesCopy);
            }

            foreach (Voice[] voices in keyVoiceBindings.Values)
            {
                for (int index = 0; index < voices.Length; index++)
                {
                    voices[index].LPF = new StateVariableLPF(12000, 0.2, backend.PolyphonicSynthesizer.SampleRate);
                }

                backend.PolyphonicSynthesizer.AddVoices(voices);
            }

            // Distribute the oscillator amplitudes equally, to ensure a decent volume.
            backend.PolyphonicSynthesizer.ForEachVoice(voice =>
            {
                if (voice.Oscillators.Count > 1)
                {
                    voice.Oscillators.ForEach(oscillator => oscillator.Amplitude /= (double)voice.Oscillators.Count);
                }
            });

            voiceProperties = new ViewableList<Property<Voice>>();

            console = new Console.Console(game);

            game.OnInitialized += (_) => InitUI();
        }

        private void InitUI()
        {
            InitMasterVolumeUI();
            InitGlobalGainUI();
            InitGlobalPanUI();
            InitShiftAndControlKeyShiftUI();
            InitRecordingUI();
            InitVoicesUI();
        }

        private void InitMasterVolumeUI()
        {
            string xml = @"
<Layout>

    <MasterVolumeControlGroup X=""83%"" Y=""1%"" W=""17%"" H=""10%""/>

</Layout>";

            ViewableList<Widget> widgets = uiXmlParser.Parse(xml);

            game.AddUIWidgets(widgets);
        }

        private void InitGlobalGainUI()
        {
            string xml = @"
<Layout>

    <GlobalGainControlGroup X=""83%"" Y=""16%"" W=""17%"" H=""10%""/>

</Layout>";

            ViewableList<Widget> widgets = uiXmlParser.Parse(xml);

            game.AddUIWidgets(widgets);
        }

        private void InitGlobalPanUI()
        {
            string xml = @"
<Layout>

    <GlobalPanControlGroup X=""83%"" Y=""31%"" W=""17%"" H=""10%""/>

</Layout>";

            ViewableList<Widget> widgets = uiXmlParser.Parse(xml);

            game.AddUIWidgets(widgets);
        }

        private void InitShiftAndControlKeyShiftUI()
        {
            string xml = @"
<Layout>

    <GlobalSemitoneShiftControlGroup X=""83%"" Y=""48%"" W=""17%"" H=""20%""/>

</Layout>";

            ViewableList<Widget> widgets = uiXmlParser.Parse(xml);

            game.AddUIWidgets(widgets);
        }

        private void InitRecordingUI()
        {
            string xml = @"
<Layout>

    <RecordingControlGroup X=""30%"" Y=""1%"" W=""40%"" H=""10%""/>

</Layout>";

            ViewableList<Widget> widgets = uiXmlParser.Parse(xml);

            game.AddUIWidgets(widgets);
        }

        private void InitVoicesUI()
        {
            string xml = @"
<Layout>

    <Window Title=""Voices"" X=""50"" Y=""50"" W=""400"" H=""600"">

        <ScrollPane X=""0%"" Y=""0%"" W=""100%"" H=""95%""/>

    </Window>

</Layout>";


            ViewableList<Widget> widgets = uiXmlParser.Parse(xml);

            Window window = (Window)widgets[0];

            voicesGroup = (GroupWidget)window[window.ComputeEffectiveChildBeginOffset()];

            float currentY = voicesGroup.Position.Y;

            backend.PolyphonicSynthesizer.ForEachVoice(delegate (Voice voice)
            {
                InitVoiceGroup(voice, ref currentY, offsetYFirst: false);
            });

            game.AddUIWidgets(widgets);

            backend.PolyphonicSynthesizer.OnVoiceAdded += delegate (PolyphonicSynthesizer polyphonic, Voice voice)
            {
                float currentY = voicesGroup[voicesGroup.Count - 1].Position.Y;

                InitVoiceGroup(voice, ref currentY, offsetYFirst: true);
            };
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
                currentY += groupH + (voicesGroup.Size.Y * 0.1f);
            }

            string xml = "<Layout>" + Environment.NewLine;

            xml +=
            $@"
                    <VoiceGroup X=""{groupX}"" Y=""{groupY}"" W=""{groupW}"" H=""{groupH}""/>
            ";

            currentY += groupH + (voicesGroup.Size.Y * 0.05f);

            xml += Environment.NewLine + "</Layout>";

            ViewableList<Widget> voiceGroups = uiXmlParser.Parse(xml);

            voiceGroups.ForEach(voiceGroup =>
            {
                ((VoiceGroup)voiceGroup).Voice = voice;
            });

            voicesGroup.AddChildRange(voiceGroups);
        }

        public void AddVoiceToExistingKeyBinding(Keys key, Voice voice)
        {
            AddVoicesToExistingKeyBinding(key, new Voice[] { voice });
        }

        public void AddVoicesToExistingKeyBinding(Keys key, Voice[] voices)
        {
            if (!keyVoiceBindings.TryGetValue(key, out Voice[] existingVoices))
            {
                throw new InvalidOperationException($"No binding for key \"{key}\" exists.");
            }

            keyVoiceBindings.Remove(key);

            Voice[] allVoices = ArrayUtils.Concatenate(voices, existingVoices);

            keyVoiceBindings.Add(key, allVoices);

            backend.PolyphonicSynthesizer.AddVoices(voices);
        }

        public void AddVoiceKeyBinding(Keys key, Voice voice)
        {
            AddVoicesKeyBinding(key, new Voice[] { voice });
        }

        public void AddVoicesKeyBinding(Keys key, Voice[] voices)
        {
            keyVoiceBindings.Add(key, voices);

            backend.PolyphonicSynthesizer.AddVoices(voices);
        }

        public bool KeyDown(Keys key, bool isRepeat, float holdTime)
        {
            if (!isRepeat && key == Keys.OemTilde)
            {
                console.Toggle();

                return true;
            }

            if (!isRepeat && keyVoiceBindings.TryGetValue(key, out Voice[] voices))
            {
                backend.PolyphonicSynthesizer.VoicesOn(voices);

                return true;
            }

            if (!isRepeat && (key == Keys.LeftShift || key == Keys.RightShift) && !currentShiftShiftedSemitoneAmount.HasValue)
            {
                currentShiftShiftedSemitoneAmount = ShiftSemitoneAmount;

                backend.PolyphonicSynthesizer.GlobalVoicePitchShiftSemitones += ShiftSemitoneAmount;

                return true;
            }

            if (!isRepeat && (key == Keys.LeftControl || key == Keys.RightControl) && !currentControlShiftedSemitoneAmount.HasValue)
            {
                currentControlShiftedSemitoneAmount = ControlSemitoneAmount;

                backend.PolyphonicSynthesizer.GlobalVoicePitchShiftSemitones += ControlSemitoneAmount;

                return true;
            }

            return false;
        }

        public bool KeyUp(Keys key)
        {
            if (keyVoiceBindings.TryGetValue(key, out Voice[] voices))
            {
                backend.PolyphonicSynthesizer.VoicesOff(voices);
            }

            if ((key == Keys.LeftShift && !game.Geo.Input.keyboard.IsKeyDown(Keys.RightShift)) 
                || (key == Keys.RightShift && !game.Geo.Input.keyboard.IsKeyDown(Keys.LeftShift)))
            {
                backend.PolyphonicSynthesizer.GlobalVoicePitchShiftSemitones -= currentShiftShiftedSemitoneAmount.Value;

                currentShiftShiftedSemitoneAmount = null;

                return true;
            }

            if ((key == Keys.LeftControl && !game.Geo.Input.keyboard.IsKeyDown(Keys.RightControl))
                || (key == Keys.RightControl && !game.Geo.Input.keyboard.IsKeyDown(Keys.LeftControl)))
            {
                backend.PolyphonicSynthesizer.GlobalVoicePitchShiftSemitones -= currentControlShiftedSemitoneAmount.Value;

                currentControlShiftedSemitoneAmount = null;

                return true;
            }

            return false;
        }

        public bool MouseDown(float x, float y, MouseStates.Button button)
        {
            return false;
        }

        public bool MouseUp(float x, float y, MouseStates.Button button)
        {
            return false;
        }

        public bool MouseMoved(float previousX, float previousY, float x, float y)
        {
            return false;
        }

        public bool MouseDragged(float previousX, float previousY, float x, float y, MouseStates.Button button)
        {
            return false;
        }

        public bool MouseScrolled(float x, float y, int verticalAmount, int horizontalAmount)
        {
            return false;
        }

        public bool GamePadDown(Buttons button, bool isRepeat, float holdTime)
        {
            return false;
        }

        public bool GamePadUp(Buttons button)
        {
            return false;
        }

        public bool GamePadSticks(bool leftRepeat, bool rightRepeat, float lx, float ly, float rx, float ry)
        {
            return false;
        }

        public bool GamePadTriggers(bool leftRepeat, bool rightRepeat, float left, float right)
        {
            return false;
        }

        public void WindowResized(int width, int height)
        {
            console.WindowResized(width, height);
        }

        public void Init(Stage stage)
        {
            InitUI(stage);
        }

        private void InitUI(Stage stage)
        {
            console.Init();

            console.InitUI(stage);
        }

        private static Dictionary<Keys, Voice[]> GetDefaultKeyVoiceBindings()
        {
            MidiNote startNote = MidiNote.C4;

            int[] semitoneOffsets = { 0, 2, 4, 5, 7, 9, 11 }; // C, D, E, F, G, A, B

            int startOctave = MidiUtils.GetOctave(startNote);
            int additionalLayerCount = 3;

            ViewableList<ValueTuple<Keys, Voice[]>> voices = new ViewableList<ValueTuple<Keys, Voice[]>>();

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

                ValueTuple<Keys, Voice[]> binding = (key, voiceList.ToArray());

                voices.Add(binding);
            }

            return voices.ToDictionary(x => x.Item1, x => x.Item2);

            /*Voice a3 = Voice.FromMidi(MidiNote.A3);

            Voice a4 = Voice.FromMidi(MidiNote.A4);

            Voice b3 = Voice.FromMidi(MidiNote.B3);

            Voice b4 = Voice.FromMidi(MidiNote.B4);

            Voice c3 = Voice.FromMidi(MidiNote.C3);

            Voice c4 = Voice.FromMidi(MidiNote.C4);

            Voice d3 = Voice.FromMidi(MidiNote.D3);

            Voice d4 = Voice.FromMidi(MidiNote.D4);

            Voice e3 = Voice.FromMidi(MidiNote.E3);

            Voice e4 = Voice.FromMidi(MidiNote.E4);

            Voice f3 = Voice.FromMidi(MidiNote.F3);

            Voice f4 = Voice.FromMidi(MidiNote.F4);

            Voice g3 = Voice.FromMidi(MidiNote.G3);

            Voice g4 = Voice.FromMidi(MidiNote.G4);

            return new Dictionary<Keys, Voice[]>
            {
                { Keys.A, new Voice[] { a3, a4 } },

                { Keys.B, new Voice[] { b3, b4 } },

                { Keys.C, new Voice[] { c3, c4 } },

                { Keys.D, new Voice[] { d3, d4 } },

                { Keys.E, new Voice[] { e3, e4 } },

                { Keys.F, new Voice[] { f3, f4 } },

                { Keys.G, new Voice[] { g3, g4 } }
            };*/
        }

        private void AddUIParserTypeFactories()
        {
            uiXmlParser.AddTypeFactory(new SliderDisplayWidgetFactory());
            uiXmlParser.AddTypeFactory(new MasterVolumeControlGroupFactory());
            uiXmlParser.AddTypeFactory(new GlobalGainControlGroupFactory());
            uiXmlParser.AddTypeFactory(new GlobalPanControlGroupFactory());
            uiXmlParser.AddTypeFactory(new GlobalSemitoneShiftControlGroupFactory());
            uiXmlParser.AddTypeFactory(new RecordingControlGroupFactory());
            uiXmlParser.AddTypeFactory(new VoiceGroupFactory());
        }

        private void AddUIManagerStyles()
        {
            InitPlayPauseButtonStyles();
            InitRecordStopButtonStyles();
            InitTrashButtonStyle();
        }

        private void InitPlayPauseButtonStyles()
        {
            Vec2f playButtonVertexStart = new Vec2f(0.225f, 0.225f);
            Vec2f playButtonVertexEnd = new Vec2f(0.725f, 0.825f);

            Vec2f[] playButtonVertices = GeoLib.GeoShapes.Shapes.GeneratePolygonVertices(playButtonVertexStart, playButtonVertexEnd, 3);

            Vec2f pauseButtonVertexStart = new Vec2f(0.3f, 0.25f);
            Vec2f pauseButtonVertexEnd = new Vec2f(0.7f, 0.775f);

            Vec2f size = new Vec2f(0.12f, pauseButtonVertexEnd.Y - pauseButtonVertexStart.Y);

            Vec2f min = pauseButtonVertexStart;
            Vec2f max = min + size;

            Vec2f[] pauseButtonVertices = new Vec2f[4]
            {
                min,
                new Vec2f(max.X, min.Y),
                max,
                new Vec2f(min.X, max.Y)
            };

            float horizontalSpacing = pauseButtonVertexEnd.X - size.X - pauseButtonVertexStart.X;

            Vec2fValue secondBarOffset = Vec2fValue.Normalized(new Vec2f(horizontalSpacing, 0f));

            RenderData pauseButtonRectPrimitive_2 = RenderData.SolidPolygon(pauseButtonVertices, 4, BuiltinColors.DarkWhite, offset: secondBarOffset);

            RenderData pauseButtonNormal = RenderData.SolidPolygon(pauseButtonVertices, 4, BuiltinColors.DarkWhite, compound: pauseButtonRectPrimitive_2);
            RenderData playButtonNormal = RenderData.SolidPolygon(playButtonVertices, 3, BuiltinColors.DarkWhite);
            RenderData playButtonPressed = playButtonNormal.Copy(deepCopy: false, colorScalar: 1.25f);
            RenderData pauseButtonPressed = pauseButtonNormal.Copy(deepCopy: false, colorScalar: 0.8f);
            RenderData playButtonDisabled = playButtonNormal.Copy(deepCopy: false, color: UIManager.WidgetDisabledTint);
            RenderData pauseButtonDisabled = pauseButtonNormal.Copy(deepCopy: false, color: UIManager.WidgetDisabledTint);

            RenderData normal = game.UIManager.PropertyGroupStyle.RenderData;
            RenderData hovered = normal.Copy(deepCopy: false, colorScalar: 1.25f);
            RenderData pressed = normal.Copy(deepCopy: false, colorScalar: 0.8f);

            ImageButton.ImageButtonStyle playButtonStyle = new ImageButton.ImageButtonStyle
            {
                Normal = null,
                ImageNormal = playButtonNormal,

                Hovered = hovered,

                Pressed = pressed,
                ImagePressed = playButtonPressed,

                ImageDisabled = playButtonDisabled,

                HandOnHover = game.UIManager.DefaultButtonUXData.HandOnHover
            };

            ImageButton.ImageButtonStyle pauseButtonStyle = new ImageButton.ImageButtonStyle
            {
                Normal = null,
                ImageNormal = pauseButtonNormal,

                Hovered = hovered,

                Pressed = pressed,
                ImagePressed = pauseButtonPressed,

                ImageDisabled = pauseButtonDisabled,

                HandOnHover = game.UIManager.DefaultButtonUXData.HandOnHover
            };

            game.UIManager.AddOrSetStyle("PlayButtonStyle", playButtonStyle);
            game.UIManager.AddOrSetStyle("PauseButtonStyle", pauseButtonStyle);
        }

        private void InitRecordStopButtonStyles()
        {
            RenderData recordButtonNormal = RenderData.SolidCircle(Color.Red, segments: 50);
            RenderData stopButtonNormal = RenderData.Texture(TexMaker.WhitePixel, tint: BuiltinColors.DarkWhite);
            RenderData stopButtonPressed = stopButtonNormal.Copy(deepCopy: false, colorScalar: 1.25f);
            RenderData recordButtonPressed = recordButtonNormal.Copy(deepCopy: false, colorScalar: 0.8f);
            RenderData recordButtonDisabled = recordButtonNormal.Copy(deepCopy: false, color: UIManager.WidgetDisabledTint);
            RenderData stopButtonDisabled = stopButtonNormal.Copy(deepCopy: false, color: UIManager.WidgetDisabledTint);

            RenderData normal = game.UIManager.PropertyGroupStyle.RenderData;
            RenderData hovered = normal.Copy(deepCopy: false, colorScalar: 1.25f);
            RenderData pressed = normal.Copy(deepCopy: false, colorScalar: 0.8f);

            ImageButton.ImageButtonStyle stopButtonStyle = new ImageButton.ImageButtonStyle
            {
                Normal = null,
                ImageNormal = stopButtonNormal,

                Hovered = hovered,

                Pressed = pressed,
                ImagePressed = stopButtonPressed,

                ImageDisabled = stopButtonDisabled,

                HandOnHover = game.UIManager.DefaultButtonUXData.HandOnHover
            };

            ImageButton.ImageButtonStyle recordButtonStyle = new ImageButton.ImageButtonStyle
            {
                Normal = null,
                ImageNormal = recordButtonNormal,

                Hovered = hovered,

                Pressed = pressed,
                ImagePressed = recordButtonPressed,

                ImageDisabled = recordButtonDisabled,

                HandOnHover = game.UIManager.DefaultButtonUXData.HandOnHover
            };

            game.UIManager.AddOrSetStyle("StopButtonStyle", stopButtonStyle);
            game.UIManager.AddOrSetStyle("RecordButtonStyle", recordButtonStyle);
        }

        private void InitTrashButtonStyle()
        {
            Vec2f verticesMin = new Vec2f(0.1f, 0.225f);
            Vec2f verticesMax = 1f - verticesMin - new Vec2f(0.125f, 0f);
            Vec2f verticesSize = verticesMax - verticesMin;

            float lineThickness = 0.05f;

            LineData[] lines = new LineData[]
            {
                new LineData
                {
                    Line = new Line
                    {
                        Start = verticesMin + verticesSize * new Vec2f(0.35f, 0.25f),
                        End = new Vec2f(verticesMin.X + verticesSize.X * 0.4f, verticesMax.Y)
                    },

                    Thickness = lineThickness,

                    Color = BuiltinColors.DarkWhite
                },

                new LineData
                {
                    Line = new Line
                    {
                        Start = new Vec2f(verticesMin.X + verticesSize.X * 0.4f, verticesMax.Y),
                        End = new Vec2f(verticesMin.X + verticesSize.X * 0.8f, verticesMax.Y)
                    },

                    Thickness = lineThickness,

                    Color = BuiltinColors.DarkWhite
                },

                new LineData
                {
                    Line = new Line
                    {
                        Start = new Vec2f(verticesMin.X + verticesSize.X * 0.8f, verticesMax.Y),
                        End = new Vec2f(verticesMax.X - 0.1f, verticesMin.Y + verticesSize.Y * 0.25f)
                    },

                    Thickness = lineThickness,

                    Color = BuiltinColors.DarkWhite
                },

                new LineData
                {
                    Line = new Line
                    {
                        Start = new Vec2f(verticesMax.X - 0.1f, verticesMin.Y + verticesSize.Y * 0.25f),
                        End = new Vec2f(verticesSize.X * 0.6f, verticesMin.Y - 0.05f)
                    },

                    Thickness = lineThickness,

                    Color = BuiltinColors.DarkWhite
                }

                /*new Vec2f(verticesMin.X + 0.25f, verticesMax.Y),
                new Vec2f(verticesMin.X + verticesSize.X * 0.3f, verticesMax.Y),
                new Vec2f(verticesMax.X, verticesMin.Y + 0.25f),
                new Vec2f(verticesMin.X, verticesMin.Y + 0.25f),
                verticesMin*/
            };

            RenderData trashButtonNormal = RenderData.Lines(lines, lines.Length, linesClosed: false);
            RenderData trashButtonPressed = trashButtonNormal.Copy(deepCopy: false, colorScalar: 1.25f);
            // Deep copy true for this one, because it will modify the color of all lines in the line array.
            RenderData trashButtonDisabled = trashButtonNormal.Copy(deepCopy: true, color: UIManager.WidgetDisabledTint);

            RenderData normal = game.UIManager.PropertyGroupStyle.RenderData;
            RenderData hovered = normal.Copy(deepCopy: false, colorScalar: 1.25f);
            RenderData pressed = normal.Copy(deepCopy: false, colorScalar: 0.8f);

            ImageButton.ImageButtonStyle trashButtonStyle = new ImageButton.ImageButtonStyle
            {
                Normal = null,
                ImageNormal = trashButtonNormal,

                Hovered = hovered,

                Pressed = pressed,
                ImagePressed = trashButtonPressed,

                ImageDisabled = trashButtonDisabled,

                HandOnHover = game.UIManager.DefaultButtonUXData.HandOnHover
            };

            game.UIManager.AddOrSetStyle("TrashButtonStyle", trashButtonStyle);
        }

        public class SliderDisplayWidgetFactory : UIXmlParser.TypeFactory
        {
            private static readonly ReadOnlyMemory<string> allowedLabelPositionNames;
            private static readonly ReadOnlyMemory<string> allowedLabelPositionXNames;
            private static readonly ReadOnlyMemory<string> allowedLabelPositionYNames;

            private static readonly ReadOnlyMemory<string> allowedSliderPositionNames;
            private static readonly ReadOnlyMemory<string> allowedSliderPositionXNames;
            private static readonly ReadOnlyMemory<string> allowedSliderPositionYNames;

            private static readonly ReadOnlyMemory<string> allowedSliderSizeNames;
            private static readonly ReadOnlyMemory<string> allowedSliderWidthNames;
            private static readonly ReadOnlyMemory<string> allowedSliderHeightNames;

            private static readonly ReadOnlyMemory<string> allowedTextFieldPositionNames;
            private static readonly ReadOnlyMemory<string> allowedTextFieldPositionXNames;
            private static readonly ReadOnlyMemory<string> allowedTextFieldPositionYNames;
            
            private static readonly ReadOnlyMemory<string> allowedTextFieldSizeNames;
            private static readonly ReadOnlyMemory<string> allowedTextFieldWidthNames;
            private static readonly ReadOnlyMemory<string> allowedTextFieldHeightNames;

            private static readonly ReadOnlyMemory<string> allowedResetButtonPositionNames;
            private static readonly ReadOnlyMemory<string> allowedResetButtonPositionXNames;
            private static readonly ReadOnlyMemory<string> allowedResetButtonPositionYNames;

            private static readonly ReadOnlyMemory<string> allowedResetButtonSizeNames;
            private static readonly ReadOnlyMemory<string> allowedResetButtonWidthNames;
            private static readonly ReadOnlyMemory<string> allowedResetButtonHeightNames;

            static SliderDisplayWidgetFactory()
            {
                allowedLabelPositionNames = new string[] { "labelposition" };
                allowedLabelPositionXNames = new string[] { "labelx" };
                allowedLabelPositionYNames = new string[] { "labely" };

                allowedSliderPositionNames = new string[] { "sliderposition" };
                allowedSliderPositionXNames = new string[] { "sliderx" };
                allowedSliderPositionYNames = new string[] { "slidery" };

                allowedSliderSizeNames = new string[] { "slidersize" };
                allowedSliderWidthNames = new string[] { "sliderwidth" };
                allowedSliderHeightNames = new string[] { "sliderheight" };

                allowedTextFieldPositionNames = new string[] { "textfieldposition" };
                allowedTextFieldPositionXNames = new string[] { "textfieldx" };
                allowedTextFieldPositionYNames = new string[] { "textfieldy" };

                allowedTextFieldSizeNames = new string[] { "textfieldsize" };
                allowedTextFieldWidthNames = new string[] { "textfieldwidth" };
                allowedTextFieldHeightNames = new string[] { "textfieldheight" };

                allowedResetButtonPositionNames = new string[] { "resetbuttonposition" };
                allowedResetButtonPositionXNames = new string[] { "resetbuttonx" };
                allowedResetButtonPositionYNames = new string[] { "resetbuttony" };

                allowedResetButtonSizeNames = new string[] { "resetbuttonsize" };
                allowedResetButtonWidthNames = new string[] { "resetbuttonw" };
                allowedResetButtonHeightNames = new string[] { "resetbuttonh" };
            }

            public SliderDisplayWidgetFactory() : base("SliderDisplayWidget")
            {

            }

            public override Widget Create(UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                if (!UIXmlParser.TryGetDoubleNumberRangeAndDefaultValue(attributes,
                                                                        out NumberRange<double>? range,
                                                                        out double defaultValue))
                {
                    throw new InvalidOperationException("Could not find number range or default value.");
                }

                UIXmlParser.TryGetBool(attributes, "treatasscalarpercentage", out bool treatAsScalarPercentage);
                UIXmlParser.TryGetString(attributes, "propertyname", out string propertyName);


                UIXmlParser.TryGetVec2f(attributes,
                                        allowedLabelPositionNames,
                                        allowedLabelPositionXNames,
                                        allowedLabelPositionYNames,
                                        out Vec2f? labelPosition,
                                        out _, out _);

                UIXmlParser.TryGetVec2f(attributes,
                                        allowedSliderPositionNames,
                                        allowedSliderPositionXNames,
                                        allowedSliderPositionYNames,
                                        out Vec2f? sliderPosition,
                                        out _, out _);

                UIXmlParser.TryGetVec2f(attributes,
                                        allowedSliderSizeNames,
                                        allowedSliderWidthNames,
                                        allowedSliderHeightNames,
                                        out Vec2f? sliderSize,
                                        out _, out _);

                UIXmlParser.TryGetVec2f(attributes,
                                        allowedTextFieldPositionNames,
                                        allowedTextFieldPositionXNames,
                                        allowedTextFieldPositionYNames,
                                        out Vec2f? textFieldPosition,
                                        out _, out _);

                UIXmlParser.TryGetVec2f(attributes,
                                        allowedTextFieldSizeNames,
                                        allowedTextFieldWidthNames,
                                        allowedTextFieldHeightNames,
                                        out Vec2f? textFieldSize,
                                        out _, out _);

                UIXmlParser.TryGetVec2f(attributes,
                                        allowedResetButtonPositionNames,
                                        allowedResetButtonPositionXNames,
                                        allowedResetButtonPositionYNames,
                                        out Vec2f? resetButtonPosition,
                                        out _, out _);

                UIXmlParser.TryGetVec2f(attributes,
                                        allowedResetButtonSizeNames,
                                        allowedResetButtonWidthNames,
                                        allowedResetButtonHeightNames,
                                        out Vec2f? resetButtonSize,
                                        out _, out _);

                if (!UIXmlParser.TryGetFloat(attributes, "dragincrement", out float dragIncrement, out _))
                {
                    dragIncrement = 1.0f;
                }

                return new SliderDisplayWidget(position, size,
                                               style: null,
                                               uiManager: uiManager,
                                               range: range.Value,
                                               defaultValue: defaultValue,
                                               treatAsScalarPercentage: treatAsScalarPercentage,
                                               propertyName: propertyName,
                                               labelPosition: labelPosition,
                                               sliderPosition: sliderPosition,
                                               sliderSize: sliderSize,
                                               textFieldPosition: textFieldPosition,
                                               textFieldSize: textFieldSize,
                                               resetButtonPosition: resetButtonPosition,
                                               resetButtonSize: resetButtonSize)
                {
                    DragIncrement = dragIncrement
                };
            }
        }

        private class VoiceGroupFactory : UIXmlParser.TypeFactory
        {
            public VoiceGroupFactory() : base("VoiceGroup")
            {

            }

            public override Widget Create(UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new VoiceGroup(position, size, 
                                      voice: null, 
                                      uiManager: uiManager);
            }
        }

        private class MasterVolumeControlGroupFactory : UIXmlParser.TypeFactory
        {
            public MasterVolumeControlGroupFactory() : base("MasterVolumeControlGroup")
            {

            }

            public override Widget Create(UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new MasterVolumeControlGroup(position, size, uiManager: uiManager);
            }
        }

        private class GlobalGainControlGroupFactory : UIXmlParser.TypeFactory
        {
            public GlobalGainControlGroupFactory() : base("GlobalGainControlGroup")
            {

            }

            public override Widget Create(UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new GlobalGainControlGroup(position, size, uiManager: uiManager);
            }
        }

        private class GlobalPanControlGroupFactory : UIXmlParser.TypeFactory
        {
            public GlobalPanControlGroupFactory() : base("GlobalPanControlGroup")
            {

            }

            public override Widget Create(UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new GlobalPanControlGroup(position, size, uiManager: uiManager);
            }
        }

        private class GlobalSemitoneShiftControlGroupFactory : UIXmlParser.TypeFactory
        {
            public GlobalSemitoneShiftControlGroupFactory() : base("GlobalSemitoneShiftControlGroup")
            {

            }

            public override Widget Create(UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new GlobalVoiceSemitonePitchShiftControlGroup(position, size, uiManager: uiManager);
            }
        }

        private class RecordingControlGroupFactory : UIXmlParser.TypeFactory
        {
            public RecordingControlGroupFactory() : base("RecordingControlGroup")
            {

            }

            public override Widget Create(UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new RecordingControlGroup(position, size, uiManager: uiManager);
            }
        }
    }
}