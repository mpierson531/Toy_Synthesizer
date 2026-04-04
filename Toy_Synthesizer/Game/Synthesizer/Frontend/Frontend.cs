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

using Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend
{
    // TODO: Implement adding/removing voices and oscillators.
    public class Frontend
    {
        private readonly UIXmlParser uiXmlParser;

        private readonly VoiceFrontend voiceFrontend;

        private readonly Game game;

        private readonly Console.Console console;

        public VoiceFrontend VoiceFrontend
        {
            get => voiceFrontend;
        }

        public Frontend(Game game)
        {
            this.game = game;

            uiXmlParser = new UIXmlParser(game.UIManager);

            voiceFrontend = new VoiceFrontend(game, uiXmlParser);

            AddUIParserTypeFactories();
            AddUIManagerStyles();

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

            voiceFrontend.InitUI();

            InitConsole();
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

        private void InitConsole()
        {
            console.Init();

            console.InitUI();
        }

        public bool KeyDown(Keys key, bool isRepeat, float holdTime)
        {
            if (!isRepeat && key == Keys.OemTilde)
            {
                console.Toggle();

                return true;
            }

            return voiceFrontend.KeyDown(key, isRepeat, holdTime);
        }

        public bool KeyUp(Keys key)
        {
            return voiceFrontend.KeyUp(key);
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

            Vec2f[] playButtonVertices = Shapes.GeneratePolygonVertices(playButtonVertexStart, playButtonVertexEnd, 3);

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
                                      game: uiManager.Game);
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