using System;
using System.Collections.Generic;
using System.Xml.Linq;

using FontStashSharp;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.Slicing;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Actions;
using GeoLib.GeoGraphics.UI.WidgetAdapters;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoInput;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;
using GeoLib.GeoUtils.Converters;
using GeoLib.GeoUtils.Pooling;
using GeoLib.GeoUtils.Providers;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Toy_Synthesizer.Game.UI
{
    public class UIManager : Disposable
    {
        public struct ButtonUXData
        {
            public float ScaleUpDelta;
            public float ScaleUpDuration;
            public Interpolation ScaleUpInterpolation;

            public bool ScaleDownOnPress;

            public float ScaleDownDuration;
            public Interpolation ScaleDownInterpolation;

            public bool HandOnHover;

            public void Deconstruct(out float scaleUpDelta, out float scaleUpDuration, out Interpolation scaleUpInterpolation,
                                    out bool scaleDownOnPress,
                                    out float scaleDownDuration, out Interpolation scaleDownInterpolation,
                                    out bool handOnHover)
            {
                scaleUpDelta = ScaleUpDelta;
                scaleUpDuration = ScaleUpDuration;
                scaleUpInterpolation = ScaleUpInterpolation;

                scaleDownOnPress = ScaleDownOnPress;

                scaleDownDuration = ScaleDownDuration;
                scaleDownInterpolation = ScaleDownInterpolation;

                handOnHover = HandOnHover;
            }
        }

        public struct ScrollBarUXData
        {
            public bool HideHorizontal;
            public bool HideVertical;

            public FloatValue Position; // Y for horizontal bar, X for vertical
            public FloatValue Size; // Height for horizontal bar, Width for vertical

            public float GrowAmount;
            public float GrowDuration;
            public Interpolation GrowInterpolation;
        }

        public struct BaseButtonStyles
        {
            public Color HoveredTint;
            public Color PressedTint;
            public Color CheckedTint;
            public Color CheckedHoveredTint;
            public Color CheckedPressedTint;
            public Color DisabledTint;

            public Vec2fValue HoveredOffset;
            public Vec2fValue PressedOffset;

            public bool HandOnHover;
        }

        public struct DropDownUXData
        {
            public bool UseScrollPane;

            public Vec2fValue DropDownPosition;
            public Vec2fValue? DropDownMaxSize;
            public Vec2fValue ButtonStartPosition;
            public Vec2fValue ButtonSize;
            public Vec2fValue ButtonSpacing;

            public Vec2fValue? AdditionalDropDownSize;

            public ButtonUXData? ButtonUXData;
        }

        public struct DrawerUXData
        {
            public Vec2f NormalizedChildStartPosition;
            public Vec2f NormalizedChildSize;
            public Vec2f NormalizedAdditionalSpacing;
            public Vec2f? NormalizedRetreatAmount;

            public Drawer.RetreatFunction RetreatMode;

            public float ShowDuration;

            public Interpolation Interpolation;

            public bool ApplyAnimationsToHiding;

            public bool AnimateParentChildren;
            public bool AnimateParentChildrenWithInterpolation;
        }

        private static int RoundedRectangleTextureSize = 64;

        public const int PrimitiveSegments = 30;
        public const Alignment DEFAULT_TEXTBUTTON_ALIGNMENT = Alignment.Center;
        public const WindowBehaviorFlags WindowBehavior = WindowBehaviorFlags.ResizeTitle
                                                          | GeoLib.GeoGraphics.UI.Widgets.Window.DEFAULT_BEHAVIOR & ~WindowBehaviorFlags.OnlyDragOnTitleBar
                                                          | WindowBehaviorFlags.ShowBorderWhenInactive;

        public const bool DEFAULT_WINDOWS_HAVE_CLOSE_BUTTON = true;

        public static readonly Color DarkWhite;
        public static readonly Color DarkerWhite;

        private static readonly Color generalTextColor;
        private static readonly Color generalTextDisabledColor;

        public static readonly Color TextButtonNormalColor;
        public static readonly Color TextButtonHoveredColor;
        public static readonly Color TextButtonPressedColor;
        public static readonly Color TextButtonTextHoveredColor;
        public static readonly Color TextButtonTextPressedColor;

        public static readonly Color DefaultSliderKnobTint;
        public static readonly Color CheckboxUncheckedTint;
        public static readonly Color CheckboxPressedTint;
        public static readonly Color CheckboxCheckedTint;
        public static readonly Color CheckboxHoveredTint;
        public static readonly Color CheckmarkHoveredTint;
        public static readonly Color CheckmarkCheckedTint;
        public static readonly Color WidgetDisabledTint;
        public static readonly Color DropDownBackgroundColor;
        public static readonly Color DropDownBorderColor;

        public static readonly Color WindowTitleBarTint;
        public static readonly Color WindowBorderTint;
        public static readonly Color WindowInactiveBorderTint;
        public static readonly Color WindowInactiveTitleBarTint;
        public static readonly Color WindowCloseButtonHoverTint;
        public static readonly Color WindowCloseButtonDownTint;
        public static readonly Color WindowCloseButtonImageTint;

        public static readonly bool TextLinkUseHasBeenClickedStyle;

        static UIManager()
        {
            InitActionPools();

            DarkWhite = BuiltinColors.DarkWhite;
            DarkerWhite = BuiltinColors.DarkerWhite;

            WidgetDisabledTint = new Color(115, 115, 115, 255);

            generalTextColor = DarkWhite;
            generalTextDisabledColor = new Color(175, 175, 175, 255);

            TextButtonNormalColor = Color.Gray;
            TextButtonHoveredColor = TextButtonNormalColor.ScaleRGB(1.12f);
            TextButtonPressedColor = TextButtonNormalColor.ScaleRGB(0.9f);
            TextButtonTextHoveredColor = generalTextColor.ScaleRGB(1.05f);
            TextButtonTextPressedColor = generalTextColor.ScaleRGB(1.05f);

            DefaultSliderKnobTint = new Color(140, 140, 140, 255);
            CheckboxUncheckedTint = new Color(100, 100, 100, 255);
            CheckboxHoveredTint = new Color(180, 180, 180, 255);
            CheckboxPressedTint = new Color(200, 200, 200, 255);
            CheckboxCheckedTint = DarkWhite;
            //CheckboxCheckedTint = new Color(0, 100, 200, 255);
            CheckmarkHoveredTint = CheckboxHoveredTint;
            CheckmarkCheckedTint = CheckboxCheckedTint;
            //CheckmarkCheckedTint = CheckboxCheckedTint;
            DropDownBackgroundColor = new Color(50, 50, 50, 255);
            DropDownBorderColor = new Color(110, 110, 255, 255);

            WindowTitleBarTint = new Color(0, 100, 200, 255);
            WindowBorderTint = WindowTitleBarTint;
            WindowInactiveBorderTint = Color.DarkSlateGray;
            WindowInactiveTitleBarTint = Color.DarkSlateGray;
            WindowCloseButtonHoverTint = new Color(200, 20, 20, 255);
            WindowCloseButtonDownTint = WindowCloseButtonHoverTint.ScaleRGB(0.85f);
            WindowCloseButtonImageTint = DarkWhite;

            TextLinkUseHasBeenClickedStyle = true;
        }

        private readonly Game game;
        private readonly Geo geo;
        private Vec2f uiScalar;
        private Vec2f previousUIScalar;

        public Game Game
        {
            get => game;
        }

        public Vec2f UIScalar
        {
            get => uiScalar;

            set
            {
                previousUIScalar = uiScalar;
                uiScalar = value;
            }
        }

        private TextLinkStyle globalTextLinkStyle;
        private Color textLinkHoveredColor;
        private Color textLinkPressedColor;
        private Color textLinkUnclickedColor;
        private Color textLinkClickedColor;
        private Color textLinkDisabledColor;

        private ButtonUXData defaultButtonUXData;
        private ButtonUXData defaultDropDownButtonUXData;
        private ScrollBarUXData defaultScrollBarUXData;
        private DrawerUXData defaultDrawerUXData;

        private TextButton.TextButtonStyle drawerCoverTextButtonStyle; // For text.
        private Button.ButtonStyle drawerCoverButtonStyle; // For a plain button.

        public readonly Color TextHeaderTextColor;
        public readonly float TextHeaderTextScale;
        public readonly bool TextHeaderDecorationsHaveUnderline;
        public readonly float TextUnderlineThickness;

        public readonly Vec2fValue DrawerCoverButtonPosition;
        public readonly Vec2fValue DrawerCoverButtonSize;

        public ButtonUXData DefaultButtonUXData
        {
            get => defaultButtonUXData;
        }

        public ButtonUXData DefaultDropDownButtonUXData
        {
            get => defaultDropDownButtonUXData;
        }

        public ScrollBarUXData DefaultScrollBarUXData
        {
            get => defaultScrollBarUXData;
        }

        public DrawerUXData DefaultDrawerUXData
        {
            get => defaultDrawerUXData;
        }

        public DynamicSpriteFont MainFont { get; set; }

        public readonly Texture2D TextButtonTexture;

        public readonly Texture2D TextFieldTexture;
        /*public readonly Texture2D TextFieldInactiveTexture;
        public readonly Texture2D TextFieldHoverTexture;*/

        public readonly Texture2D CheckboxTexture;

        public readonly Texture2D TooltipTexture;

        public readonly Texture2D WindowCloseButtonBackground;

        public Color GeneralTextColor
        {
            get => generalTextColor;
        }

        public Color GeneralTextDisabledColor
        {
            get => generalTextDisabledColor;
        }

        public readonly int TextButtonCornerRadius;
        public readonly int TextFieldCornerRadius;
        /*public readonly int TextFieldActiveBorderThickness;
        public readonly int TextFieldHoverBorderThickness;*/
        public readonly int ScrollbarCornerRadius;
        public readonly int CheckboxBackgroundCornerRadius;
        public readonly int CheckboxBackgroundBorderThickness;
        public readonly int DropDownCornerRadius;
        public readonly FloatValue DropDownBorderThickness;
        public readonly int WindowBorderThickness;

        public FontSystemEffect GeneralPlainLabelTextEffect { get; set; }

        public readonly BaseButtonStyles BaseButtonStyle;
        public readonly TextButton.TextButtonStyle TextButtonStyle;
        public readonly TextField.TextFieldStyle TextFieldStyle;
        public readonly TextField.TextFieldStyle TextFieldStyle_SharpCorners;
        public readonly Label.LabelStyle BackgroundedLabelStyle;
        public readonly Label.LabelStyle BackgroundedPropertyLabelStyle;
        public readonly ImageButton.ImageButtonStyle CheckboxStyle;
        public readonly TextButton.TextButtonStyle TextCheckButtonStyle;
        public readonly Window.WindowStyle WindowStyle;
        public readonly ImageButton.ImageButtonStyle WindowCloseButtonStyle;
        public readonly GroupWidget.GroupStyle DrawerStyle;
        public readonly GroupWidget.GroupStyle PropertyGroupStyle;
        public readonly GroupWidget.GroupStyle PropertyGroupStyle_Circle;

        public readonly LineData[] WindowCloseButtonLineData;

        public Color GeneralTabDefaultColor { get; set; }
        public Color GeneralTabActiveColor { get; set; }

        public Color TextFieldTint { get; set; }
        public Color TextFieldTextHoverColor { get; set; }
        public Color TextFieldTextTint { get; set; }
        public Color TextFieldDisabledTextTint { get; set; }
        public Color TextFieldDisabledTint { get; set; }
        public Color TextFieldDefaultTextColor { get; set; }
        public Color TextFieldDefaultTextHoveredColor { get; set; }
        public TextStyle TextFieldDefaultTextTextStyle { get; set; }
        public TextStyle TextFieldDefaultTextHoveredTextStyle { get; set; }
        public FontSystemEffect TextFieldDefaultTextFontEffect { get; set; }
        public FontSystemEffect TextFieldDefaultTextHoveredFontEffect { get; set; }
        public Color TextFieldCaretTint { get; set; }
        public float TextFieldTextPaddingX { get; set; }
        public float TextFieldTextPaddingY { get; set; }
        public float TextFieldCaretWidth { get; set; }
        public float NormalizedTextFieldCaretHeight { get; set; } // Normalized

        public Color WindowTint { get; set; }

        public Color ScrollPaneColor { get; set; }
        public Color ScrollBarOffColor { get; set; }
        public Color ScrollBarHoverColor { get; set; }
        public Color ScrollBarDownColor { get; set; }
        public float ScrollPaneWheelSpeed { get; set; }
        public float ScrollPanePageKeySpeed { get; set; } // Page Up/Down
        public float ScrollPaneHomeEndKeySpeed { get; set; } // Home key and End key
        public float ScrollPaneDamping { get; set; }
        public Interpolation ScrollPaneInterpolation { get; set; }

        public Vec2f SpinnerViewPosition { get; set; }
        public Vec2f SpinnerViewSize { get; set; }
        public Vec2f SpinnerIncrementButtonPosition { get; set; }
        public Vec2f SpinnerDecrementButtonPosition { get; set; }
        public Vec2f SpinnerButtonSize { get; set; }
        public Color NumberSpinnerLabelFontColor { get; set; }

        public Color BackgroundedLabelTint { get; set; }
        public Color BackgroundedLabelTextTint { get; set; }

        public float TooltipHoverTime { get; set; }
        public float TooltipOffTime { get; set; }
        public readonly Vec2f TooltipSizePadding;

        public readonly float WindowLineCheckTolerance;

        public readonly SaveLoadDialog.SaveGroupStyle SaveLoadDialogGroupStyle;
        public readonly RenderData SaveLoadDialogGroupSelectedRenderData;

        private readonly Dictionary<string, object> widgetStyles;

        public UIManager(Game game, Stage stage)
        {
            this.game = game;
            this.geo = game.Geo;

            widgetStyles = new Dictionary<string, object>(1000);

            UIScalar = Vec2f.One;

            // NOTE:
            // If TextHelpers.MeasureTrueSize is true, underlines will be below the end of the text, including "hangers" like lowercase 'g'.
            // If false, it will use the line height of the font to render the underline.
            TextHelpers.MeasureTrueSize = false;

            /*TextButtonCornerRadius = game.ScaleByDisplayResolutionMin(2f);
            TextFieldCornerRadius = TextButtonCornerRadius;
            ScrollbarCornerRadius = game.ScaleByDisplayResolutionMin(1f);
            CheckboxBackgroundCornerRadius = game.ScaleByDisplayResolutionMin(1f);
            CheckboxBackgroundBorderThickness = (int)RoundAwayFromZero(game.ScaleByDisplayResolutionMin(1.5f));
            DropDownCornerRadius = game.ScaleByDisplayResolutionMin(1.5f);
            DropDownBorderThickness = (int)RoundAwayFromZero(game.ScaleByDisplayResolutionMin(0.5f));
            WindowBorderThickness = (int)RoundAwayFromZero(game.ScaleByDisplayResolutionMin(1f));*/

            TextHeaderTextColor = GeneralTextColor;
            TextHeaderTextScale = 1.3f;
            TextHeaderDecorationsHaveUnderline = true;
            TextUnderlineThickness = game.ScaleByDisplayResolution_Min(1f);

            WindowLineCheckTolerance = MathF.Max(game.ScaleByDisplayResolution_Min(2f), 1f); // It should be at least 1.

            stage.UseGlobalWindowBorderCheckTolerance = true;
            stage.GlobalWindowBorderCheckTolerance = WindowLineCheckTolerance;

            WindowCloseButtonLineData = GetWindowCloseButtonXLineData(out _);

            int globalCornerRadius = 4;
            //float globalCornerRadius = game.ScaleByDisplayResolutionMin(1f);

            TextButtonCornerRadius = globalCornerRadius;
            TextFieldCornerRadius = globalCornerRadius;
            ScrollbarCornerRadius = globalCornerRadius;
            CheckboxBackgroundCornerRadius = globalCornerRadius;
            CheckboxBackgroundBorderThickness = (int)GeoMath.RoundAwayFromZero(game.ScaleByDisplayResolution_Min(1f));
            DropDownCornerRadius = globalCornerRadius;
            DropDownBorderThickness = new FloatValue(ValueMode.Absolute, (int)GeoMath.RoundAwayFromZero(game.ScaleByDisplayResolution_Min(1f)));
            WindowBorderThickness = (int)GeoMath.RoundAwayFromZero(game.ScaleByDisplayResolution_Min(1f));

            TextButtonTexture = TexMaker.RoundedRectangle(geo.GraphicsDevice, RoundedRectangleTextureSize, RoundedRectangleTextureSize, TextButtonCornerRadius, 0, Color.White, Color.Black);

            // TextButton texture for white border when hovered.
            //TextButtonHoveredTexture = TexMaker.RoundedRectangle(geo.GraphicsDevice, 128, 128, TextButtonCornerRadius, TextButtonCornerRadius, TextButtonHoveredColor, DarkWhite);

            TextFieldTexture = TexMaker.RoundedRectangle(geo.GraphicsDevice, RoundedRectangleTextureSize, RoundedRectangleTextureSize, TextFieldCornerRadius, 0, Color.White, Color.Black);
            /*TextFieldTexture = TexMaker.RoundedRectangle(geo.GraphicsDevice, 64, 64, TextFieldCornerRadius, TextFieldActiveBorderThickness, Color.White, Color.Black);
            TextFieldHoverTexture = TexMaker.RoundedRectangle(geo.GraphicsDevice, 64, 64, TextFieldCornerRadius, TextFieldHoverBorderThickness, Color.White, Color.Black);
            TextFieldInactiveTexture = TexMaker.RoundedRectangle(geo.GraphicsDevice, 64, 64, TextFieldCornerRadius, 0, Color.White, Color.Black);*/

            CheckboxTexture = TexMaker.RoundedRectangle(geo.GraphicsDevice, RoundedRectangleTextureSize, RoundedRectangleTextureSize,
                                                        CheckboxBackgroundCornerRadius,
                                                        CheckboxBackgroundBorderThickness,
                                                        Color.Black,
                                                        Color.White);

            // With the default scrollpane values and interpolation set in this class,
            // a ScrollPane's velocity seems to take a very long time to get to zero or below ScrollPane's default velocity epsilon.
            // Setting a high epsilon to counter this.
            GeoLib.GeoGraphics.UI.Widgets.ScrollPane.DefaultVelocityEpsilon = 0.1f;

            WindowTint = new Color(50, 50, 50, 255);

            ScrollPaneColor = WindowTint;
            ScrollBarOffColor = DarkWhite;
            ScrollBarHoverColor = new Color(150, 150, 150, 255);
            ScrollBarDownColor = DarkWhite;
            ScrollPaneWheelSpeed = 5f;
            ScrollPanePageKeySpeed = 2.5f;
            ScrollPaneHomeEndKeySpeed = 10f;
            ScrollPaneDamping = 1.65f;
            ScrollPaneInterpolation = new Interpolation.SwingInterp(2f);
            defaultScrollBarUXData = new ScrollBarUXData
            {
                HideHorizontal = true,
                HideVertical = false,

                Position = FloatValue.Normalized(0.25f),
                Size = FloatValue.Normalized(0.50f),

                GrowAmount = 5f,
                GrowDuration = 0.05f,
                GrowInterpolation = Interpolation.Smooth2
            };

            Color tooltipBackgroundColor = new Color(25, 25, 25, 255);

            TooltipTexture = TexMaker.GetPixel(geo.GraphicsDevice, tooltipBackgroundColor);

            TextFieldTint = DarkWhite;
            TextFieldTextHoverColor = Color.Black;
            TextFieldTextTint = Color.Black;
            TextFieldDisabledTextTint = new Color(70, 70, 70, 255);
            TextFieldDisabledTint = WidgetDisabledTint;
            TextFieldDefaultTextColor = new Color(40, 40, 235, 255);
            TextFieldDefaultTextHoveredColor = new Color(40, 40, 235, 166);
            TextFieldDefaultTextTextStyle = TextStyle.Underline;
            TextFieldDefaultTextHoveredTextStyle = TextFieldDefaultTextTextStyle;
            TextFieldDefaultTextFontEffect = FontSystemEffect.None;
            TextFieldDefaultTextHoveredFontEffect = TextFieldDefaultTextFontEffect;
            TextFieldCaretTint = Color.Black;
            TextFieldTextPaddingX = 0.02f;
            TextFieldTextPaddingY = 0f;
            TextFieldCaretWidth = game.ScaleByDisplayResolution_Min(1f);
            NormalizedTextFieldCaretHeight = 0.9f;

            TextFieldStyle = new TextField.TextFieldStyle
            {
                Hovered = RenderData.Custom(new Slice(TextFieldTexture, SliceCountMode.Nine, new SliceSizeData(GeoMath.RoundAwayFromZero(TextFieldCornerRadius)),
                                           tint: TextFieldTint.ScaleRGB(0.8f))),

                Active = RenderData.Custom(new Slice(TextFieldTexture, SliceCountMode.Nine, new SliceSizeData(GeoMath.RoundAwayFromZero(TextFieldCornerRadius)),
                                           tint: TextFieldTint)),

                /*Active = RenderData.Custom(new Slice(TextFieldTexture, SliceCountMode.Nine, new SliceSizeData(GeoMath.RoundAwayFromZero(TextFieldCornerRadius + TextFieldActiveBorderThickness)), 
                                           tint: TextFieldTint)),
                Inactive = RenderData.Custom(new Slice(TextFieldInactiveTexture, SliceCountMode.Nine, new SliceSizeData(GeoMath.RoundAwayFromZero(TextFieldCornerRadius)), 
                                             tint: TextFieldTint)),
                Hovered = RenderData.Custom(new Slice(TextFieldHoverTexture, SliceCountMode.Nine, new SliceSizeData(GeoMath.RoundAwayFromZero(TextFieldCornerRadius + TextFieldHoverBorderThickness)), 
                                            tint: TextFieldTint)),*/

                Disabled = RenderData.Custom(new Slice(TextFieldTexture, SliceCountMode.Nine, new SliceSizeData(GeoMath.RoundAwayFromZero(TextFieldCornerRadius)), tint: TextFieldDisabledTint)),

                TextNormal = new TextRenderData
                {
                    FontColor = TextFieldTextTint,

                    Padding = Vec2fValue.Normalized(TextFieldTextPaddingX, TextFieldTextPaddingY)
                },

                TextHovered = new TextRenderData
                {
                    FontColor = TextFieldTextHoverColor,

                    Padding = Vec2fValue.Normalized(TextFieldTextPaddingX, TextFieldTextPaddingY)
                },

                TextDisabled = new TextRenderData
                {
                    FontColor = TextFieldDisabledTextTint,

                    Padding = Vec2fValue.Normalized(TextFieldTextPaddingX, TextFieldTextPaddingY)
                },

                CaretTint = TextFieldCaretTint,

                TextSelected = RenderData.SolidRectangle(new Color(80, 80, 255, 255)),

                DefaultTextEnabled = false,
                ShowDefaultTextWhenActive = false,
                DefaultText = null,

                DefaultTextNormal = new TextRenderData
                {
                    FontColor = TextFieldDefaultTextColor,
                    FontEffect = TextFieldDefaultTextFontEffect,
                    Padding = Vec2fValue.Normalized(TextFieldTextPaddingX, TextFieldTextPaddingY)
                },

                DefaultTextHovered = new TextRenderData
                {
                    FontColor = TextFieldDefaultTextHoveredColor,
                    FontEffect = TextFieldDefaultTextHoveredFontEffect,
                    Padding = Vec2fValue.Normalized(TextFieldTextPaddingX, TextFieldTextPaddingY)
                }
            };

            TextFieldStyle_SharpCorners = Copyables.Cast<TextField.TextFieldStyle>(TextFieldStyle, deepCopy: false);
            TextFieldStyle_SharpCorners.Active = RenderData.Texture(TexMaker.WhitePixel, tint: TextFieldTint);
            TextFieldStyle_SharpCorners.Disabled = RenderData.Texture(TexMaker.WhitePixel, tint: TextFieldDisabledTint);

            BaseButtonStyle = new BaseButtonStyles
            {
                HoveredTint = Color.Black,
                PressedTint = Color.DarkGray,

                CheckedTint = Color.White,
                CheckedHoveredTint = Color.Black,
                CheckedPressedTint = Color.DarkGray,

                DisabledTint = WidgetDisabledTint,

                HoveredOffset = Vec2fValue.AbsoluteZero,
                PressedOffset = Vec2fValue.AbsoluteZero,

                HandOnHover = false,
            };

            GeneralPlainLabelTextEffect = FontSystemEffect.None;

            Interpolation defaultButtonUXScaleInterpolation = Interpolation.Smooth2;
            const float DEFAULT_BUTTON_UX_SCALE_DURATION = 0.0046875f * 0.75f;

            defaultButtonUXData = new ButtonUXData
            {
                HandOnHover = false,

                ScaleUpDelta = game.ScaleByDisplayResolution_Min(1.5f), // Set when UIScalar is set (which is set when the window is resized)
                ScaleUpDuration = DEFAULT_BUTTON_UX_SCALE_DURATION,
                ScaleUpInterpolation = defaultButtonUXScaleInterpolation,

                ScaleDownOnPress = true,

                ScaleDownDuration = DEFAULT_BUTTON_UX_SCALE_DURATION,
                ScaleDownInterpolation = defaultButtonUXScaleInterpolation
            };

            defaultDropDownButtonUXData = defaultButtonUXData;
            defaultDropDownButtonUXData.HandOnHover = true;

            Vec2fValue textButtonTextHoverOffset = Vec2fValue.Normalized(0f, game.ScaleByDisplayResolution_Min(-0.04f));
            Vec2fValue textButtonTextPressedOffset = Vec2fValue.NormalizedZero;

            RenderData textButtonNormalRenderData = RenderData.Custom(new Slice(TextButtonTexture, SliceCountMode.Nine, new SliceSizeData(TextButtonCornerRadius), tint: TextButtonNormalColor));

            RenderData textButtonHoverRenderData = RenderData.Copy(textButtonNormalRenderData, false);
            textButtonHoverRenderData.SetColor(TextButtonHoveredColor);

            // TextButton RenderData for a white border when hovered.
            //RenderData textButtonHoverRenderData = RenderData.Custom(new Slice(TextButtonHoveredTexture, SliceCountMode.Nine, new SliceSizeData(TextButtonCornerRadius * 2), tint: Color.White));

            RenderData textButtonPressedRenderData = RenderData.Copy(textButtonNormalRenderData, false);
            textButtonPressedRenderData.SetColor(TextButtonPressedColor);

            RenderData textButtonDisabledRenderData = RenderData.Copy(textButtonNormalRenderData, false);
            textButtonDisabledRenderData.SetColor(WidgetDisabledTint);

            TextButtonStyle = new TextButton.TextButtonStyle
            {
                Normal = textButtonNormalRenderData,
                Hovered = textButtonHoverRenderData,
                Pressed = textButtonPressedRenderData,

                Disabled = textButtonDisabledRenderData,

                TextNormal = new TextRenderData { FontColor = GeneralTextColor },
                TextHovered = new TextRenderData { FontColor = TextButtonTextHoveredColor, Padding = textButtonTextHoverOffset },
                TextPressed = new TextRenderData { FontColor = TextButtonTextPressedColor, Padding = textButtonTextPressedOffset },
                TextDisabled = new TextRenderData { FontColor = GeneralTextDisabledColor },

                HandOnHover = false,
            };

            CheckboxStyle = InitCheckboxStyle();

            RenderData textCheckButton = RenderData.Copy(TextButtonStyle.Normal, false);
            RenderData textCheckButtonHovered = RenderData.Copy(TextButtonStyle.Hovered, false);
            RenderData textCheckButtonPressed = RenderData.Copy(TextButtonStyle.Pressed, false);
            TextRenderData textCheckButtonText = TextRenderData.Copy(TextButtonStyle.TextNormal, false);
            TextRenderData textCheckButtonTextHovered = TextRenderData.Copy(TextButtonStyle.TextHovered, false);
            TextRenderData textCheckButtonTextPressed = TextRenderData.Copy(TextButtonStyle.TextPressed, false);

            RenderData textCheckButtonCheckedData = RenderData.Copy(TextButtonStyle.Normal, false);
            textCheckButtonCheckedData.SetColor(Color.SeaGreen);

            RenderData textCheckButtonCheckedHovered = RenderData.Copy(TextButtonStyle.Normal, false);
            textCheckButtonCheckedHovered.SetColor(Color.SeaGreen.ScaleRGB(1.1f));

            RenderData textCheckButtonCheckedPressed = RenderData.Copy(TextButtonStyle.Normal, false);
            textCheckButtonCheckedPressed.SetColor(Color.SeaGreen.ScaleRGB(0.8f));

            TextRenderData textCheckButtonTextChecked = new TextRenderData { FontColor = DarkWhite, Padding = Vec2fValue.Absolute(0f, 1f) };
            TextRenderData textCheckButtonTextCheckedHovered = TextRenderData.Copy(TextButtonStyle.TextCheckedHovered, false);
            TextRenderData textCheckButtonTextCheckedPressed = TextRenderData.Copy(TextButtonStyle.TextCheckedPressed, false);

            TextCheckButtonStyle = new TextButton.TextButtonStyle
            {
                Normal = textCheckButton,
                Hovered = textCheckButtonHovered,
                Pressed = textCheckButtonPressed,
                TextNormal = textCheckButtonText,
                TextHovered = textCheckButtonTextHovered,
                TextPressed = textCheckButtonTextPressed,

                Checked = textCheckButtonCheckedData,
                CheckedHovered = textCheckButtonCheckedHovered,
                CheckedPressed = textCheckButtonCheckedPressed,

                TextChecked = textCheckButtonTextChecked,
                TextCheckedHovered = textCheckButtonTextCheckedHovered,
                TextCheckedPressed = textCheckButtonTextCheckedPressed,

                Disabled = TextButtonStyle.Disabled,
                TextDisabled = TextButtonStyle.TextDisabled,

                HandOnHover = false
            };

            WindowCloseButtonBackground = TexMaker.WhitePixel;

            // This commented code below is for doing windows with a rounded border and a rounded close button.
            /*RenderData windowBorder = RenderData.Custom(new Slice(TexMaker.RoundedRectangle(geo.GraphicsDevice, 128, 128, globalCornerRadius, WindowBorderThickness, Color.Transparent, Color.White),
                                                          SliceCountMode.Nine, new SliceSizeData(globalCornerRadius + WindowBorderThickness)));
            windowBorder.SetColor(WindowBorderTint);

            RenderData windowBorderUnfocused = windowBorder.Copy(deepCopy: false, color: WindowInactiveBorderTint);

            WindowStyle = new Window.WindowStyle
            {
                RenderData = RenderData.Texture(TexMaker.WhitePixel, tint: WindowTint),

                Border = windowBorder,
                TitleBar = RenderData.Texture(TexMaker.WhitePixel, tint: WindowTitleBarTint),

                UnfocusedBorder = windowBorderUnfocused,
                UnfocusedTitleBar = RenderData.Texture(TexMaker.WhitePixel, tint: WindowInactiveTitleBarTint),

                UniformBorder = true
            };

            RenderData windowCloseButtonData = RenderData.Custom(new Slice(TexMaker.RoundedRectangle(geo.GraphicsDevice, 64, 64, globalCornerRadius, 0, Color.White, Color.White),
                                                                 SliceCountMode.Nine, new SliceSizeData(globalCornerRadius), tint: Color.Transparent));
            RenderData windowCloseButtonHoverData = windowCloseButtonData.Copy(deepCopy: false, color: WindowCloseButtonHoverTint);
            RenderData windowCloseButtonPressedData = windowCloseButtonData.Copy(deepCopy: false, color: WindowCloseButtonDownTint);*/

            WindowStyle = new Window.WindowStyle
            {
                RenderData = RenderData.Texture(TexMaker.WhitePixel, tint: WindowTint),

                Border = RenderData.Texture(TexMaker.WhitePixel, tint: WindowBorderTint),
                TitleBar = RenderData.Texture(TexMaker.WhitePixel, tint: WindowTitleBarTint),

                UnfocusedBorder = RenderData.Texture(TexMaker.WhitePixel, tint: WindowInactiveBorderTint),
                UnfocusedTitleBar = RenderData.Texture(TexMaker.WhitePixel, tint: WindowInactiveTitleBarTint),

                UniformBorder = false
            };

            RenderData windowCloseButtonData = RenderData.Texture(WindowCloseButtonBackground, Color.Transparent);
            RenderData windowCloseButtonHoverData = RenderData.Texture(WindowCloseButtonBackground, WindowCloseButtonHoverTint);
            RenderData windowCloseButtonPressedData = RenderData.Texture(WindowCloseButtonBackground, WindowCloseButtonDownTint);

            // The image render data for this is set when creating the window.
            WindowCloseButtonStyle = new ImageButton.ImageButtonStyle
            {
                Normal = windowCloseButtonData,
                Hovered = windowCloseButtonHoverData,
                Pressed = windowCloseButtonPressedData
            };

            GeneralTabDefaultColor = DarkWhite;
            GeneralTabActiveColor = TextButtonPressedColor;

            /*SpinnerViewPosition = Vec2f.Zero;
            SpinnerViewSize = new Vec2f(0.8f, 1f);
            SpinnerIncrementButtonPosition = new Vec2f(0.8f, 0f);
            SpinnerDecrementButtonPosition = new Vec2f(0.8f, 0.5f);
            SpinnerButtonSize = new Vec2f(0.2f, 0.5f);
            NumberSpinnerLabelFontColor = Color.Black;*/

            SpinnerViewPosition = Vec2f.Zero;
            SpinnerViewSize = new Vec2f(0.75f, 1f);
            SpinnerIncrementButtonPosition = new Vec2f(0.75f, 0f);
            SpinnerDecrementButtonPosition = new Vec2f(0.75f, 0.5f);
            SpinnerButtonSize = new Vec2f(0.25f, 0.5f);
            NumberSpinnerLabelFontColor = Color.Black;

            TooltipHoverTime = 0.4f;
            TooltipOffTime = 0f;
            TooltipSizePadding = game.ScaleByDisplayResolution(new Vec2f(6f));

            BackgroundedLabelTint = new Color(75, 75, 75, 255);
            BackgroundedLabelTextTint = GeneralTextColor;

            InitPropertyGroupStyles(globalCornerRadius, WindowBorderThickness, out PropertyGroupStyle, out PropertyGroupStyle_Circle);

            InitBackgroundedLabelStyles(out BackgroundedLabelStyle, out BackgroundedPropertyLabelStyle);

            InitDrawerData(out DrawerStyle, out DrawerCoverButtonPosition, out DrawerCoverButtonSize);

            InitGlobalTextLinkStyle();

            SaveLoadDialogGroupSelectedRenderData = InitSaveLoadDialogGroupSelectedRenderData();
            SaveLoadDialogGroupStyle = InitSaveLoadDialogGroupStyle();

            InitWidgetStyles();
        }

        public TextLinkStyle GetGlobalTextLinkStyle()
        {
            return globalTextLinkStyle;
        }

        public float GetMenuBarHorizontalSpacing()
        {
            return ScaleWidth(10f);
        }

        public float GetMenuBarVerticalSpacing()
        {
            return 0f;
        }

        public float GetMenuBarMinX()
        {
            return ScaleWidth(10f);
        }

        public Vec2f GetDefaultConfigSpacing()
        {
            return Scale(75f, 65f);
        }

        public float GetMainButtonHeight()
        {
            return ScaleHeight(40f);
        }

        public Vec2f GetLabelSize()
        {
            return Scale(new Vec2f(100, 40));
        }

        public float GetWindowTitleBarHeight()
        {
            return ScaleHeight(25f);
        }

        public float GetWindowWidgetBeginPaddingX()
        {
            return ScaleWidth(7f);
        }

        public float GetScrollPaneWidgetBeginPaddingY()
        {
            return ScaleHeight(10f);
        }

        public DropDownUXData GetDropDownUXData()
        {
            Vec2fValue buttonStartPosition = Vec2fValue.Normalized(0.025f, 1.25f);
            Vec2fValue dropDownPosition = Vec2fValue.Normalized(0f, 1.05f);

            return new DropDownUXData
            {
                UseScrollPane = false,

                DropDownPosition = dropDownPosition,
                DropDownMaxSize = null,

                ButtonStartPosition = buttonStartPosition,
                ButtonSize = Vec2fValue.Normalized(0.95f, 1f),

                ButtonSpacing = Vec2fValue.Normalized(0f, 0.2f),

                AdditionalDropDownSize = Vec2fValue.Normalized(0.05f, 0.4f),

                ButtonUXData = DefaultDropDownButtonUXData,
            };
        }

        public DropDownUXData GetScrollableDropDownUXData()
        {
            DropDownUXData uxData = GetDropDownUXData();

            uxData.UseScrollPane = true;

            // Since it can scroll, clamp the height of the drop down.

            uxData.DropDownMaxSize = Vec2fValue.Normalized(1f, 5f);

            return uxData;
        }

        // Helps prevent UI clickthrough when it isn't supposed to happen
        public static void AddDefaultMouseClickListener(Widget widget)
        {
            InputListener listener = new InputListener()
            {
                MouseDown = delegate (InputEvent e, float x, float y, MouseStates.Button button)
                {
                    e.HandleAndStop();
                }
            };

            widget.AddListener(listener);
        }

        public static InputListener AddEscapeListener(Widget widget, Action onEscape, bool isCapture = false)
        {
            return AddEscapeListener(widget, onEscape, out _, isCapture);
        }

        public static InputListener AddEscapeListener(Widget widget, Action onEscape,
                                                      out InputListener.KeyD keyDown,
                                                      bool isCapture = false)
        {
            InputListener listener = new InputListener();

            AddEscapeListener(listener, onEscape, out keyDown);

            if (isCapture)
            {
                widget.AddCaptureListener(listener);
            }
            else
            {
                widget.AddListener(listener);
            }

            return listener;
        }

        public static void AddEscapeListener(InputListener listener, Action onEscape)
        {
            AddEscapeListener(listener, onEscape, out _);
        }

        public static void AddEscapeListener(InputListener listener, Action onEscape, 
                                             out InputListener.KeyD keyDown)
        {
            keyDown = delegate (InputEvent e, Keys key)
            {
                if (key == Keys.Escape && !e.Keyboard.IsRepeat)
                {
                    e.HandleAndStop();

                    onEscape();
                }
            };

            listener.KeyDown += keyDown;
        }

        public static void SetTextWidgetWrapBreakpointToGroup(GroupWidget group, ITextWidget textWidget,
                                                              float extraSpacing = 0f)
        {
            if (group is ScrollPane scrollPane)
            {
                textWidget.NormalizedWrapBreakpointX = (scrollPane.GetViewportBoundsAABB().MaxX() - extraSpacing - textWidget.Position.X) / textWidget.TextSize.X;
            }
            else
            {
                textWidget.NormalizedWrapBreakpointX = (group.GetMaxX() - extraSpacing - textWidget.Position.X) / textWidget.TextSize.X;
            }

            textWidget.Layout();
        }

        public Vec2f Scale(Vec2f value)
        {
            return Scale(value.X, value.Y);
        }

        public Vec2f Scale(float x, float y)
        {
            return new Vec2f(x * uiScalar.X, y * uiScalar.Y);
        }

        public float ScaleWidth(float value)
        {
            return value * uiScalar.X;
        }

        public float ScaleHeight(float value)
        {
            return value * uiScalar.Y;
        }

        public float ScaleMin(float value)
        {
            return value * uiScalar.Min();
        }

        public Vec2f ScaleMin(Vec2f value)
        {
            return value * uiScalar.Min();
        }

        public Vec2f ScaleShrinking(Vec2f value)
        {
            return new Vec2f(ScaleWidthShrinking(value.X), ScaleHeightShrinking(value.Y));
        }

        private float ScaleWidthShrinking(float value)
        {
            return uiScalar.X < previousUIScalar.X ? value * uiScalar.X : value;
        }

        private float ScaleHeightShrinking(float value)
        {
            return uiScalar.Y < previousUIScalar.Y ? value * uiScalar.Y : value;
        }

        public GroupWidget GroupWithDefaultStyle(Vec2f position, Vec2f size,
                                               bool tintChildren = true,
                                               bool sizeChildren = true)
        {
            GroupWidget.GroupStyle style = GetDefaultGroupStyle();

            return new GroupWidget(position, size, style: style, tintChildren: tintChildren, sizeChildren: sizeChildren);
        }

        public void ApplyDefaultGroupStyle(GroupWidget groupWidget)
        {
            groupWidget.Style = GetDefaultGroupStyle();
        }

        public GroupWidget.GroupStyle GetDefaultGroupStyle()
        {
            GroupWidget.GroupStyle style = new GroupWidget.GroupStyle
            {
                RenderData = RenderData.Texture(TexMaker.WhitePixel, tint: WindowTint)
            };

            return style;
        }

        public Button Button(Vec2f position, Vec2f size, RenderData normalRenderData,
                             bool applyButtonUXData = true,
                             ButtonUXData? uxData = null)
        {
            RenderData hovered = RenderData.Copy(normalRenderData, false);
            hovered.SetColor(BaseButtonStyle.HoveredTint);
            hovered.Offset = BaseButtonStyle.HoveredOffset;

            RenderData pressed = RenderData.Copy(normalRenderData, false);
            pressed.SetColor(BaseButtonStyle.PressedTint);
            pressed.Offset = BaseButtonStyle.PressedOffset;

            RenderData _checked = RenderData.Copy(normalRenderData, false);
            _checked.SetColor(BaseButtonStyle.CheckedTint);

            RenderData checkedHovered = RenderData.Copy(normalRenderData, false);
            checkedHovered.SetColor(BaseButtonStyle.CheckedHoveredTint);
            checkedHovered.Offset = BaseButtonStyle.HoveredOffset;

            RenderData checkedPressed = RenderData.Copy(normalRenderData, false);
            checkedPressed.SetColor(BaseButtonStyle.CheckedPressedTint);
            checkedPressed.Offset = BaseButtonStyle.PressedOffset;

            RenderData disabled = RenderData.Copy(normalRenderData, false);
            disabled.SetColor(BaseButtonStyle.DisabledTint);

            Button.ButtonStyle style = new Button.ButtonStyle
            {
                Normal = normalRenderData,
                Hovered = hovered,
                Pressed = pressed,

                Checked = _checked,
                CheckedHovered = checkedHovered,
                CheckedPressed = checkedPressed,

                Disabled = disabled,

                HandOnHover = BaseButtonStyle.HandOnHover
            };

            Button button = new Button(position, size, style);

            if (applyButtonUXData)
            {
                ApplyButtonUXDataOrDefault(button, uxData);
            }

            return button;
        }

        public TextButton TextButton(Vec2f position, Vec2f size, string text, TextButton.TextButtonStyle style,
                                     bool applyUXData = true,
                                     ButtonUXData? uxData = null,
                                     Alignment alignment = DEFAULT_TEXTBUTTON_ALIGNMENT)
        {
            TextButton button = new TextButton(position, size, MainFont, text, style,
                                              scaleTextOnScale: false,
                                              tintText: false,
                                              alignment: alignment);

            if (applyUXData)
            {
                ApplyButtonUXDataOrDefault(button, uxData);
            }

            return button;
        }

        public TextButton TextButton(Vec2f position, Vec2f size, string text,
                                     bool applyUXData = true,
                                     ButtonUXData? uxData = null,
                                     Alignment alignment = DEFAULT_TEXTBUTTON_ALIGNMENT)
        {
            TextButton.TextButtonStyle style = Copyables.Cast<TextButton.TextButtonStyle>(TextButtonStyle, deepCopy: true);

            return TextButton(position, size, text, style, applyUXData, uxData, alignment);
        }

        public TextLink TextLink(GroupWidget parent, Vec2f position, Vec2f size,
                                 string url, string text,
                                 bool isEmail = false,
                                 bool? useHasBeenClickedStyle = null,
                                 TextLink.FailedToOpenDelegate onFailedToOpen = null,
                                 bool applyUXData = true,
                                 ButtonUXData? uxData = null,
                                 Alignment alignment = DEFAULT_TEXTBUTTON_ALIGNMENT)
        {
            TextLink link = new TextLink(position: position,
                                         size: size,
                                         font: MainFont,
                                         url: url,
                                         text: text,
                                         isEmail: isEmail,
                                         useGlobalLinkStyle: true,
                                         useHasBeenClickedStyle: useHasBeenClickedStyle.GetValueOrDefault(TextLinkUseHasBeenClickedStyle),
                                         onFailedToOpen: onFailedToOpen,
                                         alignment: alignment,
                                         scaleTextOnScale: false)
            {
                TintText = false
            };

            if (parent is not null)
            {
                link.FitText = false;
                link.GrowWithText = true;
                link.WrapText = true;

                SetTextWidgetWrapBreakpointToGroup(parent, link);
            }

            if (applyUXData)
            {
                ApplyButtonUXDataOrDefault(link, uxData);
            }

            return link;
        }

        public void SetTextButtonStyleColors(TextButton.TextButtonStyle style)
        {
            style.Normal.SetColor(TextButtonNormalColor);
            style.Hovered.SetColor(TextButtonHoveredColor);
            style.Pressed.SetColor(TextButtonPressedColor);

            style.Disabled.SetColor(WidgetDisabledTint);

            style.Checked.SetColor(TextButtonNormalColor);
            style.CheckedHovered.SetColor(TextButtonHoveredColor);
            style.CheckedPressed.SetColor(TextButtonPressedColor);

            style.TextNormal.FontColor = GeneralTextColor;
            style.TextHovered.FontColor = TextButtonTextHoveredColor;
            style.TextPressed.FontColor = TextButtonTextPressedColor;
            style.TextDisabled.FontColor = GeneralTextDisabledColor;
        }

        public ImageButton ImageButton(Vec2f position, Vec2f size,
                                       ImageButton.ImageButtonStyle style,
                                       bool applyUXData = true,
                                       ButtonUXData? uxData = null)
        {
            ImageButton imageButton = new ImageButton(position, size, style.ImageNormal, style,
                                                   scaleImageOnScale: false,
                                                   tintImageWhenOff: true,
                                                   tintImageWhenEngaged: true);

            if (applyUXData)
            {
                ApplyButtonUXDataOrDefault(imageButton, uxData);
            }

            return imageButton;
        }

        public Button Checkbox(Vec2f position, Vec2f size,
                               bool applyUXData = true,
                               ButtonUXData? uxData = null)
        {
            ImageButton.ImageButtonStyle style = Copyables.Cast<ImageButton.ImageButtonStyle>(CheckboxStyle);

            ImageButton checkbox = new ImageButton(position, size, style.ImageNormal, style,
                                                   scaleImageOnScale: false,
                                                   tintImageWhenOff: true,
                                                   tintImageWhenEngaged: true);

            if (applyUXData)
            {
                ApplyButtonUXDataOrDefault(checkbox, uxData);
            }

            return checkbox;
        }

        public TextButton TextCheckButton(Vec2f position, Vec2f size, string text,
                                         bool addGeneralActions = true,
                                         ButtonUXData? uxData = null,
                                         Alignment alignment = DEFAULT_TEXTBUTTON_ALIGNMENT)
        {
            TextButton.TextButtonStyle style = Copyables.Cast<TextButton.TextButtonStyle>(TextCheckButtonStyle);

            return TextButton(position, size, text, style, addGeneralActions, uxData, alignment);
        }

        // Handles scaling based on screen size.
        private void AddButtonUXEventDelegatesInternal(Button button, ButtonUXData buttonUXData)
        {
            buttonUXData.Deconstruct(out float scaleUpDelta, out float scaleUpDuration, out Interpolation scaleUpInterpolation,
                                     out bool scaleDownOnPress,
                                     out float scaleDownDuration, out Interpolation scaleDownInterpolation,
                                     out _);

            button.OnOver += delegate
            {
                ScaleToAbsoluteAction action = GetNewAction<ScaleToAbsoluteAction>();

                action.TargetSizeX = button.Size.X + ScaleWidth(scaleUpDelta);
                action.TargetSizeY = button.Size.Y + ScaleHeight(scaleUpDelta);

                action.Duration = scaleUpDuration;
                action.Interpolation = scaleUpInterpolation;

                button.AddAction(action);
            };

            button.OnExit += delegate
            {
                if (!button.Listener.IsDown())
                {
                    ScaleToAction action = GetNewAction<ScaleToAction>();
                    action.TargetX = 1f;
                    action.TargetY = 1f;
                    action.Duration = scaleDownDuration;
                    action.Interpolation = scaleDownInterpolation;

                    button.AddAction(action);
                }
            };

            if (scaleDownOnPress)
            {
                button.OnDown += delegate
                {
                    ScaleToAction action = GetNewAction<ScaleToAction>();
                    action.TargetX = 1f;
                    action.TargetY = 1f;
                    action.Duration = scaleDownDuration;
                    action.Interpolation = scaleDownInterpolation;

                    button.AddAction(action);
                };

                button.OnUp += delegate
                {
                    if (button.Listener.IsOver())
                    {
                        ScaleToAbsoluteAction action = GetNewAction<ScaleToAbsoluteAction>();

                        action.TargetSizeX = button.Size.X + ScaleWidth(scaleUpDelta);
                        action.TargetSizeY = button.Size.Y + ScaleHeight(scaleUpDelta);

                        action.Duration = scaleUpDuration;
                        action.Interpolation = scaleUpInterpolation;

                        button.AddAction(action);
                    }
                };
            }
        }

        // Handles scaling based on screen size.
        private void AddButtonUXEventDelegatesInternal(ButtonAdapter buttonAdapter, ButtonUXData buttonUXData)
        {
            buttonUXData.Deconstruct(out float scaleUpDelta, out float scaleUpDuration, out Interpolation scaleUpInterpolation,
                                     out bool scaleDownOnPress,
                                     out float scaleDownDuration, out Interpolation scaleDownInterpolation,
                                     out _);

            buttonAdapter.OnOver += delegate
            {
                ScaleToAbsoluteAction action = GetNewAction<ScaleToAbsoluteAction>();

                action.TargetSizeX = buttonAdapter.Widget.Size.X + ScaleWidth(scaleUpDelta);
                action.TargetSizeY = buttonAdapter.Widget.Size.Y + ScaleHeight(scaleUpDelta);

                action.Duration = scaleUpDuration;
                action.Interpolation = scaleUpInterpolation;

                buttonAdapter.Widget.AddAction(action);
            };

            buttonAdapter.OnExit += delegate
            {
                if (!buttonAdapter.Listener.IsDown())
                {
                    ScaleToAction action = GetNewAction<ScaleToAction>();

                    action.TargetX = 1f;
                    action.TargetY = 1f;

                    action.Duration = scaleDownDuration;
                    action.Interpolation = scaleDownInterpolation;

                    buttonAdapter.Widget.AddAction(action);
                }
            };

            if (scaleDownOnPress)
            {
                buttonAdapter.OnDown += delegate
                {
                    ScaleToAction action = GetNewAction<ScaleToAction>();

                    action.TargetX = 1f;
                    action.TargetY = 1f;

                    action.Duration = scaleDownDuration;
                    action.Interpolation = scaleDownInterpolation;

                    buttonAdapter.Widget.AddAction(action);
                };

                buttonAdapter.OnUp += delegate
                {
                    if (buttonAdapter.Listener.IsOver())
                    {
                        ScaleToAbsoluteAction action = GetNewAction<ScaleToAbsoluteAction>();

                        action.TargetSizeX = buttonAdapter.Widget.Size.X + ScaleWidth(scaleUpDelta);
                        action.TargetSizeY = buttonAdapter.Widget.Size.Y + ScaleHeight(scaleUpDelta);

                        action.Duration = scaleUpDuration;
                        action.Interpolation = scaleUpInterpolation;

                        buttonAdapter.Widget.AddAction(action);
                    }
                };
            }
        }

        public void ApplyButtonUXDataOrDefault(Button button, ButtonUXData? data)
        {
            ApplyButtonUXData(button, ButtonUXDataOrDefault(data));
        }

        public void ApplyButtonUXDataOrDefault(ButtonAdapter buttonAdapter, ButtonUXData? data)
        {
            ApplyButtonUXData(buttonAdapter, ButtonUXDataOrDefault(data));
        }

        public void ApplyButtonUXData(Button button, ButtonUXData data)
        {
            // The TextLinkStyle is global at the stage level. Do not modify.
            if (button.Style is not null && button is not GeoLib.GeoGraphics.UI.Widgets.TextLink)
            {
                button.Style.HandOnHover = data.HandOnHover;
            }

            AddButtonUXEventDelegatesInternal(button, data);
        }

        public void ApplyButtonUXData(ButtonAdapter buttonAdapter, ButtonUXData data)
        {
            buttonAdapter.Style.HandOnHover = data.HandOnHover;

            AddButtonUXEventDelegatesInternal(buttonAdapter, data);
        }

        private ButtonUXData ButtonUXDataOrDefault(ButtonUXData? uxData)
        {
            if (uxData.HasValue)
            {
                return uxData.Value;
            }

            return DefaultButtonUXData;
        }

        public Slider Slider(Vec2f position, Vec2f size,
                             float startValue, NumberRange<float> range, float dragIncrement)
        {
            Slider.SliderStyle style = new Slider.SliderStyle();

            Button rail = Button(Vec2f.Zero, Vec2f.Zero, RenderData.Texture(TexMaker.WhitePixel, tint: Color.White), applyButtonUXData: false);
            Button knob = Button(Vec2f.Zero, Vec2f.Zero, RenderData.SolidCircle(DefaultSliderKnobTint, segments: PrimitiveSegments));

            rail.Style.Normal.SetColor(DarkWhite);
            rail.Style.Hovered.SetColor(DarkWhite);
            rail.Style.Pressed.SetColor(DarkWhite);
            rail.Style.Checked.SetColor(DarkWhite);
            rail.Style.CheckedHovered.SetColor(DarkWhite);
            rail.Style.CheckedHovered.SetColor(DarkWhite);

            knob.Style.Hovered.SetColor(new Color(120, 120, 120, 255));
            knob.Style.CheckedHovered.SetColor(new Color(120, 120, 120, 255));
            knob.Style.Pressed.SetColor(new Color(120, 120, 120, 255).ScaleRGB(0.8f));
            knob.Style.CheckedPressed.SetColor(new Color(120, 120, 120, 255).ScaleRGB(0.8f));
            knob.Style.Checked.SetColor(DefaultSliderKnobTint);
            knob.Style.HandOnHover = true;

            Slider slider = new Slider(position, size, startValue, range,
                                       dragIncrement: dragIncrement,
                                       smoothDragging: false,
                                       style: style,
                                       layoutDirection: LayoutOrientation.Horizontal)
            {
                RailPosition = new Vec2f(0f, 0.4f),
                RailSize = new Vec2f(1f, 0.25f),
                KnobPosition = new Vec2f(0f, 0.05f),

                KnobSizeGetter = delegate (Vec2f position, Vec2f size)
                {
                    return new Vec2f(size.Y * 0.95f);
                },

                UseKnobSizeGetter = true,

                Knob = knob,
                Rail = rail,

                TintKnob = false,
                TintRail = true,
                TintChildren = true
            };

            return slider;
        }

        public static void AddNumberFieldValueListener(TextField textField, Action<float> boundWidgetSetter)
        {
            textField.OnTextInput += delegate (string text)
            {
                if (!text.Contains('.'))
                {
                    if (float.TryParse(textField.Text, out float value))
                    {
                        boundWidgetSetter?.Invoke(value);
                    }
                }
                else
                {
                    // Decimals.
                    // Do not set if last char is '.' or '0'.
                    // User may still be typing, and depending on formatting, it may overwrite it.

                    char lastChar = TextUtils.LastCharIgnoreWhitespace(text);

                    if (lastChar != TextUtils.CharNull && lastChar != '.' && lastChar != '0' && float.TryParse(textField.Text, out float value))
                    {
                        boundWidgetSetter?.Invoke(value);
                    }
                }
            };
        }

        // For the style, this just instantiates a new TextWidget.TextWidgetStyle.
        // It is safe to mutate the style on the label this returns.
        public PlainLabel PlainLabel(Vec2f position, Vec2f size, string text,
                                     Alignment alignment = Alignment.Left,
                                     bool fitText = true,
                                     bool wrapText = false,
                                     bool growWithText = false)
        {
            TextWidget.TextWidgetStyle style = new TextWidget.TextWidgetStyle
            {
                TextNormal = new TextRenderData
                {
                    FontColor = GeneralTextColor,
                    FontEffect = GeneralPlainLabelTextEffect,
                }
            };

            PlainLabel label = new PlainLabel(position, size, Vec2f.Half, MainFont, style,
                                              fitText: fitText,
                                              wrapText: wrapText,
                                              growWithText: growWithText,
                                              addDefaultListeners: false,
                                              text: text,
                                              alignment: alignment);

            return label;
        }

        // For the style, this just instantiates a new TextWidget.TextWidgetStyle.
        // It is safe to mutate the style on the label this returns.
        public PlainLabel PlainHeaderLabel(Vec2f position, string text,
                                           Alignment alignment = Alignment.Left,
                                           bool fitText = true)
        {
            Vec2f size = TextHelpers.Measure(MainFont, text, scale: TextHeaderTextScale);

            return PlainHeaderLabel(position, size, text, alignment, fitText);
        }

        // For the style, this just instantiates a new TextWidget.TextWidgetStyle.
        // It is safe to mutate the style on the label this returns.
        public PlainLabel PlainHeaderLabel(Vec2f position, Vec2f size, string text,
                                           Alignment alignment = Alignment.Left,
                                           bool fitText = true)
        {
            TextWidget.TextWidgetStyle style = new TextWidget.TextWidgetStyle
            {
                TextNormal = new TextRenderData
                {
                    FontColor = TextHeaderTextColor,
                    FontEffect = GeneralPlainLabelTextEffect,
                }
            };

            style.TextNormal.Decoration.Underline = TextHeaderDecorationsHaveUnderline;
            style.TextNormal.Decoration.UnderlineColor = TextHeaderTextColor;
            style.TextNormal.Decoration.UnderlineThickness = TextUnderlineThickness;

            PlainLabel label = new PlainLabel(position, size, Vec2f.Half, MainFont, style,
                                              fitText: fitText,
                                              addDefaultListeners: false,
                                              text: text,
                                              alignment: alignment)
            {
                FontScale = TextHeaderTextScale
            };

            return label;
        }

        public Label BackgroundedLabel(Vec2f position, Vec2f size, string text,
                                       Alignment alignment = Alignment.Left,
                                       Color? backgroundColor = null)
        {
            Label.LabelStyle style = Copyables.Cast<Label.LabelStyle>(BackgroundedLabelStyle, deepCopy: false);

            if (backgroundColor.HasValue)
            {
                style.Normal.SetColor(backgroundColor.Value);
            }

            Label label = new Label(position, size, MainFont, text, style, Vec2f.Half, addDefaultListeners: false, alignment: alignment);

            return label;
        }

        public DynamicPlainLabel DynamicPlainLabel(Vec2f position, Vec2f size, string text,
                                                   Alignment alignment = Alignment.Left)
        {
            TextWidget.TextWidgetStyle style = new TextWidget.TextWidgetStyle
            {
                TextNormal = new TextRenderData
                {
                    FontColor = GeneralTextColor,
                    FontEffect = GeneralPlainLabelTextEffect,
                }
            };

            DynamicPlainLabel label = new DynamicPlainLabel(position, size, Vec2f.Half, MainFont, style, text, addDefaultListeners: false, alignment: alignment);

            return label;
        }

        public TextField GeneralTextField(Vec2f position, Vec2f size, int maxCharacters,
                                          string defaultText = null,
                                          NumberRange<double>? numberModeRange = null,
                                          bool numberModeAllowFractional = true,
                                          sbyte numberModeAllowedSign = 0,
                                          double numberModeDefaultValue = 0.0)
        {
            TextField.TextFieldStyle style = Copyables.Cast<TextField.TextFieldStyle>(TextFieldStyle, deepCopy: false);

            TextField textField = GeneralTextField_Common(position, size, style, defaultText, maxCharacters);

            if (numberModeRange.HasValue)
            {
                TextNumberFilterData<double> textFilterData = new TextNumberFilterData<double>(numberModeRange.Value,
                                                                                               numberModeAllowedSign,
                                                                                               numberModeAllowFractional);

                InitNumberTextField(textField, textFilterData, numberModeDefaultValue, onTextInput: null);
            }

            return textField;
        }

        public TextField GeneralTextField_SharpCorners(Vec2f position, Vec2f size, int maxCharacters,
                                          string defaultText = null)
        {
            TextField.TextFieldStyle style = Copyables.Cast<TextField.TextFieldStyle>(TextFieldStyle_SharpCorners, deepCopy: false);

            return GeneralTextField_Common(position, size, style, defaultText, maxCharacters);
        }

        private TextField GeneralTextField_Common(Vec2f position, Vec2f size, TextField.TextFieldStyle style, string defaultText, int maxCharacters)
        {
            if (defaultText is not null)
            {
                style.DefaultTextEnabled = true;
                style.DefaultText = defaultText;
                style.ShowDefaultTextWhenActive = true;
            }

            Caret.CaretStyle caretStyle = new Caret.CaretStyle
            {
                RenderData = RenderData.Texture(TexMaker.WhitePixel, tint: style.CaretTint),
                NormalizedHeight = NormalizedTextFieldCaretHeight
            };

            Caret caret = new Caret(Vec2f.Zero, TextFieldCaretWidth, caretStyle);

            TextField textField = new TextField(position, size, style, caret, Vec2f.Half, MainFont, maxCharacters: maxCharacters,
                                                tintText: false);

            return textField;
        }

        public ScrollPane ScrollPane(Vec2f position, Vec2f size, ScrollBarUXData? scrollBarUXData = null)
        {
            ScrollPane.ScrollPaneStyle style = ScrollPaneStyle();

            ScrollPane scrollPane = new ScrollPane(position, size, 
                                                   scrollBarWidth: GetScrollBarTrackSize(), 
                                                   style: style);

            InitScrollPane(scrollPane, scrollBarUXData);

            return scrollPane;
        }

        // Inits the more logical parts of a scrollpane, as well as scrollbars.
        public void InitScrollPane(ScrollPane scrollPane,
                                   ScrollBarUXData? scrollbarUXData = null)
        {
            ScrollBarUXData uxData = scrollbarUXData ?? DefaultScrollBarUXData;

            scrollPane.AllowDragging = true;
            scrollPane.DragOnMiddleClick = true;
            scrollPane.DragOnNormalClick = false;
            scrollPane.TintChildren = true;
            scrollPane.LinearScrolling = false;
            scrollPane.Interpolation = ScrollPaneInterpolation;
            scrollPane.DampingFunction = DampingFunction.Linear; // can't pick between Linear and Exponential

            scrollPane.ScrollbarPosition = uxData.Position;
            scrollPane.ScrollbarSize = uxData.Size;

            AddScrollBarUXActions(scrollPane.HScrollBar, scrollPane.VScrollBar, uxData);

            scrollPane.Layout();

            scrollPane.RotateChildren = false;
        }

        public FloatValue GetScrollBarTrackSize()
        {
            return FloatValue.Absolute(ScaleMin(15f));
        }

        public ScrollPane.ScrollPaneStyle ScrollPaneStyle()
        {
            ScrollBar.ScrollBarStyle barStyle = new ScrollBar.ScrollBarStyle
            {
                ShowContainer = false,
                ShowScrollButtons = false,

                /*Normal = RenderData.SolidRectangle(ScrollBarOffColor, cornerRadius: ScrollbarCornerRadius),
                Hovered = RenderData.SolidRectangle(ScrollBarHoverColor, cornerRadius: ScrollbarCornerRadius),
                Down = RenderData.SolidRectangle(ScrollBarDownColor, cornerRadius: ScrollbarCornerRadius),*/

                Normal = RenderData.Texture(TexMaker.WhitePixel, tint: ScrollBarOffColor),
                Hovered = RenderData.Texture(TexMaker.WhitePixel, tint: ScrollBarHoverColor),
                Down = RenderData.Texture(TexMaker.WhitePixel, tint: ScrollBarDownColor),
            };

            //RenderData renderData = RenderData.Custom(new Slice(TextButtonTexture, SliceCountMode.Nine, new SliceSizeData(TextButtonCornerRadius)));

            RenderData renderData = RenderData.Texture(TexMaker.WhitePixel);

            ScrollPane.ScrollPaneStyle style = new ScrollPane.ScrollPaneStyle()
            {
                BarStyle = barStyle,

                RenderData = renderData,

                WheelSpeed = new Vec2f(ScrollPaneWheelSpeed),
                ScrollDamping = new Vec2f(ScrollPaneDamping),

                PageKeySpeed = ScrollPanePageKeySpeed,
                HomeEndKeySpeed = ScrollPaneHomeEndKeySpeed,
            };

            style.RenderData.SetColor(ScrollPaneColor);

            return style;
        }

        public Window WindowWithScrollPane(Vec2f position, Vec2f size, string title, float scrollPaneNegativePaddingY,
                                           ScrollBarUXData? scrollBarUXData = null,
                                           bool withCloseButton = DEFAULT_WINDOWS_HAVE_CLOSE_BUTTON,
                                           Action onClose = null)
        {
            // Initial size offset to account for window title bar height.
            scrollPaneNegativePaddingY += GetWindowTitleBarHeight();

            Vec2f scrollPanePosition = new Vec2f(position.X, position.Y);
            Vec2f scrollPaneSize = new Vec2f(size.X, ((position.Y + size.Y) - scrollPanePosition.Y) - scrollPaneNegativePaddingY);
            ScrollPane scrollPane = ScrollPane(scrollPanePosition, scrollPaneSize, scrollBarUXData);

            Window window = Window(position, size, title, withCloseButton, onClose);
            window.AddChild(scrollPane);

            return window;
        }

        // Measures string values of all values and computes the width from that.
        // The additionalSize parameter is computed against the base computed size.
        public DropDownListView DropDownList(Vec2f position, ViewableList<object> values, int defaultIndex,
                                             string coverButtonText = null,
                                             ToStringConverter toStringConverter = null,
                                             DropDownUXData? uxData = null,
                                             float? buttonHeight = null,
                                             Vec2fValue? additionalSize = null)
        {
            // If applicable, also measure the cover button text.
            if (coverButtonText is not null)
            {
                values.Add(coverButtonText);
            }

            float width = TextHelpers.FindGreatestSize(values, MainFont, toStringConverter).X;

            // If applicable, remove the cover button text.
            if (coverButtonText is not null)
            {
                values.Pop();
            }

            float realButtonHeight = buttonHeight ?? GetMainButtonHeight();

            Vec2f size = new Vec2f(width, realButtonHeight);

            if (additionalSize.HasValue)
            {
                size += additionalSize.Value.Compute(size);
            }

            return DropDownList(position, size, values, defaultIndex, coverButtonText, toStringConverter, uxData);
        }

        public DropDownListView DropDownList(Vec2f position, Vec2f size, ViewableList<object> values, int defaultIndex,
                                             string coverButtonText = null,
                                             ToStringConverter toStringConverter = null,
                                             DropDownUXData? uxData = null)
        {
            DropDownUXData nonNullUXData = uxData ?? GetScrollableDropDownUXData();

            if (!nonNullUXData.ButtonUXData.HasValue)
            {
                nonNullUXData.ButtonUXData = DefaultButtonUXData;
            }

            if (coverButtonText is null && !CollectionUtils.IsNullOrEmpty(values))
            {
                coverButtonText = ToStringConverter.Convert(toStringConverter, values[defaultIndex]);
            }

            Func<Vec2f, Vec2f, Button> coverButtonProvider = GetDropDownCoverButtonProvider(coverButtonText, nonNullUXData.ButtonUXData.Value);

            Func<string, int, Vec2f, Vec2f, Button> childProvider = GetDropDownChildProvider(nonNullUXData.ButtonUXData.Value);

            Func<Vec2f, Vec2f, GroupWidget> groupProvider = GetDropDownGroupProvider(ref nonNullUXData);

            DropDownListView dropDown = new DropDownListView(position, size,
                                                                   coverButtonProvider,
                                                                   childProvider,
                                                                   groupProvider,
                                                                   dropDownPosition: nonNullUXData.DropDownPosition,
                                                                   dropDownMaxSize: nonNullUXData.DropDownMaxSize,
                                                                   buttonStartPosition: nonNullUXData.ButtonStartPosition,
                                                                   buttonSize: nonNullUXData.ButtonSize,
                                                                   buttonSpacing: nonNullUXData.ButtonSpacing,
                                                                   additionalDropDownSize: nonNullUXData.AdditionalDropDownSize,
                                                                   values: values,
                                                                   defaultIndex: defaultIndex,
                                                                   toStringProvider: toStringConverter);

            DropDownCommons(dropDown, ref nonNullUXData);

            return dropDown;
        }

        public DropDownWidget DropDown(Vec2f position, Vec2f size,
                                       string coverButtonText,
                                       string[] itemNames,
                                       int itemCount,
                                       Action<Button, int> onSelect = null,
                                       DropDownUXData? uxData = null)
        {
            DropDownUXData nonNullUXData = uxData ?? GetScrollableDropDownUXData();

            if (!nonNullUXData.ButtonUXData.HasValue)
            {
                nonNullUXData.ButtonUXData = DefaultButtonUXData;
            }

            Func<Vec2f, Vec2f, Button> coverButtonProvider = GetDropDownCoverButtonProvider(coverButtonText, nonNullUXData.ButtonUXData.Value);

            Func<string, int, Vec2f, Vec2f, Button> childProvider = GetDropDownChildProvider(nonNullUXData.ButtonUXData.Value);

            Func<Vec2f, Vec2f, GroupWidget> groupProvider = GetDropDownGroupProvider(ref nonNullUXData);

            Func<DropDownWidget, DropDownAdapter> adapterProvider = delegate (DropDownWidget dropDown)
            {
                return new DropDownAdapter(group: dropDown,
                                           itemNames: itemNames,
                                           itemCount: itemCount,
                                           coverButtonProvider: coverButtonProvider,
                                           childProvider: childProvider,
                                           groupProvider: groupProvider,
                                           dropDownPosition: nonNullUXData.DropDownPosition,
                                           dropDownMaxSize: nonNullUXData.DropDownMaxSize,
                                           buttonStartPosition: nonNullUXData.ButtonStartPosition,
                                           buttonSize: nonNullUXData.ButtonSize,
                                           buttonSpacing: nonNullUXData.ButtonSpacing,
                                           additionalDropDownSize: nonNullUXData.AdditionalDropDownSize,
                                           onSelect: onSelect);
            };

            DropDownWidget dropDown = new DropDownWidget(position, size, adapterProvider);

            DropDownCommons(dropDown, ref nonNullUXData);

            return dropDown;
        }

        public Drawer Drawer(Vec2f position, Vec2f size, LayoutOrientation direction, string coverButtonText)
        {
            return Drawer(position, size, direction, coverButtonText, defaultDrawerUXData);
        }

        public Drawer Drawer(Vec2f position, Vec2f size, LayoutOrientation direction, string coverButtonText, DrawerUXData uxData)
        {
            //Vec2f coverButtonOrigin = new Vec2f(0.045f, 0.5f);

            //Vec2f coverButtonOrigin = Vec2f.Half;

            Vec2f coverButtonOrigin = new Vec2f(1f, 0.725f);

            TextButton drawerCoverButton = CreateDrawerTextCoverButton(position, size, coverButtonText);

            return DrawerCommon(position, size, direction, uxData, drawerCoverButton, coverButtonOrigin);
        }

        public Drawer Drawer(Vec2f position, Vec2f size, LayoutOrientation direction)
        {
            return Drawer(position, size, direction, defaultDrawerUXData);
        }

        public Drawer Drawer(Vec2f position, Vec2f size, LayoutOrientation direction, DrawerUXData uxData)
        {
            Vec2f coverButtonOrigin = new Vec2f(0.045f, 0.5f);

            return Drawer(position: position,
                          size: size,
                          direction: direction,
                          uxData: uxData,
                          coverButtonOrigin: coverButtonOrigin);
        }

        public Drawer Drawer(Vec2f position, Vec2f size, LayoutOrientation direction, DrawerUXData uxData,
                             Vec2f coverButtonOrigin)
        {
            Button drawerCoverButton = CreateDrawerCoverButton(position, size);

            return DrawerCommon(position, size, direction, uxData, drawerCoverButton, coverButtonOrigin);
        }

        public TextButton CreateDrawerTextCoverButton(Vec2f drawerPosition, Vec2f drawerSize, string coverButtonText)
        {
            TextButton.TextButtonStyle coverButtonStyle = Copyables.Cast<TextButton.TextButtonStyle>(drawerCoverTextButtonStyle);

            (Vec2f position, Vec2f size) = GetDrawerCoverButtonBounds(drawerPosition, drawerSize);

            TextButton button = new TextButton(position, size, MainFont, coverButtonText, coverButtonStyle,
                                               alignment: Alignment.Right,
                                               tintText: false,
                                               rotateText: false,
                                               fitText: false,
                                               wrapText: true);

            // The cover button style has a triangle drop down sort of thing; make it render nice with the text.
            button.RenderSize = Vec2fValue.NormalizedMin(0.5f, 1f);
            button.RenderTextPosition = Vec2fValue.Normalized(-0.125f, 0f);

            return button;
        }

        public Button CreateDrawerCoverButton(Vec2f drawerPosition, Vec2f drawerSize)
        {
            Button.ButtonStyle coverButtonStyle = Copyables.Cast<Button.ButtonStyle>(drawerCoverButtonStyle);

            (Vec2f position, Vec2f size) = GetDrawerCoverButtonBounds(drawerPosition, drawerSize);

            return new Button(position, size, coverButtonStyle);
        }

        public AABB GetDrawerCoverButtonBounds(Vec2f drawerPosition, Vec2f drawerSize)
        {
            Vec2f position = drawerPosition + (DrawerCoverButtonPosition.Compute(drawerSize));
            Vec2f size = DrawerCoverButtonSize.Compute(drawerSize);

            return new AABB(position, size);
        }

        private Drawer DrawerCommon(Vec2f position, Vec2f size, LayoutOrientation direction, DrawerUXData uxData, Button coverButton,
                                    Vec2f coverButtonOrigin,
                                    GroupWidget.GroupStyle style = null,
                                    bool withDefaultMouseHandler = true)
        {
            AddDrawerButtonRotation(coverButton, uxData.ShowDuration, uxData.Interpolation, coverButtonOrigin);

            if (style is null)
            {
                style = DrawerStyle;
            }
    
            Drawer drawer = new Drawer(position, size, coverButton, direction,
                                       showDuration: uxData.ShowDuration,
                                       showInterpolation: uxData.Interpolation,
                                       showActionGetter: GetNewAction<MoveToAction>,
                                       onMoveActionCompleted: null,
                                       applyShowActionToHiding: uxData.ApplyAnimationsToHiding,
                                       resizeParent: false,
                                       normalizedRetreatAmount: uxData.NormalizedRetreatAmount,
                                       extraSizePadding: Vec2fValue.NormalizedMin(0.05f),
                                       retreatMode: uxData.RetreatMode)
            {
                Style = style
            };

            drawer.CurrentRenderData = drawer.Style.RenderData;

            if (withDefaultMouseHandler)
            {
                AddDefaultMouseClickListener(drawer);
            }

            return drawer;
        }

        // Adds the necessary actions and events for rotating a button when it is checked/unchecked.
        // Also sets the origin of the button.
        // Also sets RotationAppliesToHits to false.
        // Angle should be in radians.
        public static void AddDrawerButtonRotation(Button button, DrawerUXData uxData, Vec2f origin)
        {
            AddDrawerButtonRotation(button, uxData.ShowDuration, uxData.Interpolation, origin);
        }

        // Adds the necessary actions and events for rotating a button when it is checked/unchecked.
        // Also sets the origin of the button.
        // Also sets RotationAppliesToHits to false.
        // Angle should be in radians.
        public static void AddDrawerButtonRotation(Button button, float duration, Interpolation interpolation, Vec2f origin)
        {
            float buttonAnimationAngle = MathF.PI * 0.5f;

            button.Origin = origin;
            button.RotationAppliesToHits = false;

            RotateByAction buttonRotateAction = ActorActions.RotateBy(buttonAnimationAngle, duration, interpolation);

            button.OnCheck += delegate
            {
                buttonRotateAction.Restart();
                buttonRotateAction.Amount = buttonAnimationAngle - button.Rotation;

                button.AddAction(buttonRotateAction);
            };

            button.OnUncheck += delegate
            {
                buttonRotateAction.Restart();
                buttonRotateAction.Amount = -buttonAnimationAngle - (-buttonAnimationAngle + button.Rotation);

                button.AddAction(buttonRotateAction);
            };
        }

        private void DropDownCommons(DropDownWidget dropDown, ref DropDownUXData uxData)
        {
            if (uxData.UseScrollPane) // Then the dropDownGroup's style is not null
            {
                dropDown.DropDownGroup.Style.RenderData = RenderData.BorderedRectangle(DropDownBorderThickness, DropDownBorderColor, DropDownBackgroundColor,
                                                                                       segments: PrimitiveSegments,
                                                                                       cornerRadius: DropDownCornerRadius);

                dropDown.DropDownGroup.CurrentRenderData = dropDown.DropDownGroup.Style.RenderData;
            }
            else // It should be a normal GroupWidget and the Style needs to be instantiated
            {
                dropDown.DropDownGroup.Style = new GroupWidget.GroupStyle
                {
                    RenderData = RenderData.BorderedRectangle(DropDownBorderThickness, DropDownBorderColor, DropDownBackgroundColor,
                                                              segments: PrimitiveSegments,
                                                              cornerRadius: DropDownCornerRadius)
                };

                dropDown.DropDownGroup.CurrentRenderData = dropDown.DropDownGroup.Style.RenderData;
            }

            dropDown.CoverButton.OnCheck += delegate
            {
                SetCharacterSpacingOfGroup(dropDown.DropDownGroup);
            };

            AddEscapeListener(dropDown.DropDownGroup, dropDown.Hide);
        }

        public Func<Vec2f, Vec2f, Button> GetDropDownCoverButtonProvider(string text, ButtonUXData uxData)
        {
            return delegate (Vec2f position, Vec2f size)
            {
                return TextButton(position, size, text, uxData: uxData);
            };
        }

        public Func<Vec2f, Vec2f, GroupWidget> GetDropDownGroupProvider(DropDownUXData uxData)
        {
            return GetDropDownGroupProvider(ref uxData);
        }

        public Func<Vec2f, Vec2f, GroupWidget> GetDropDownGroupProvider(ref DropDownUXData uxData)
        {
            if (uxData.UseScrollPane)
            {
                return delegate (Vec2f position, Vec2f size)
                {
                    return ScrollPane(position, size);
                };
            }
            else
            {
                return delegate (Vec2f position, Vec2f size)
                {
                    return new GroupWidget(position, size);
                };
            }
        }

        public Func<string, int, Vec2f, Vec2f, Button> GetDropDownChildProvider(ButtonUXData uxData)
        {
            return delegate (string name, int index, Vec2f position, Vec2f size)
            {
                return TextButton(position, size, name, uxData: uxData);
            };
        }

        public TextFieldSearchAdapter AddSearchAdapter(StringComparison stringComparisonType,
                                                       Widget targetWidget, 
                                                       Vec2fValue targetPosition,
                                                       Vec2fValue targetSize,
                                                       Vec2fValue targetCancelButtonPosition,
                                                       Vec2fValue targetCancelButtonSize, 
                                                       IProvider<IObjectStream<string>> searchStreamProvider,
                                                       Action onSearchBegin,
                                                       Action<ReadOnlyMemory<int>> onSearchFound,
                                                       Action onSearchNotFound,
                                                       Action onReset,
                                                       Action<TextFieldSearchAdapter> onHide,
                                                       bool focusOnSubsequentActivation = false,
                                                       GroupWidget searchGroup = null,
                                                       Vec2fValue searchGroupPosition = default,
                                                       Vec2fValue searchGroupSize = default)
        {
            Vec2f textFieldSize = searchGroup is null ? targetSize.Compute(targetWidget.Size) : targetSize.Compute(searchGroupSize.Compute(targetWidget.Size));

            TextField field = GeneralTextField(Vec2f.Zero, textFieldSize,
                                               maxCharacters: int.MaxValue,
                                               defaultText: "Search");

            ImageButton.ImageButtonStyle cancelSearchButtonStyle = new ImageButton.ImageButtonStyle
            {
                Normal = RenderData.Copy(TextButtonStyle.Normal, deepCopy: false),
                Hovered = RenderData.Copy(TextButtonStyle.Hovered, deepCopy: false),
                Pressed = RenderData.Copy(TextButtonStyle.Pressed, deepCopy: false),

                ImageNormal = RenderData.Lines(WindowCloseButtonLineData, WindowCloseButtonLineData.Length, linesClosed: false),
            };

            ImageButton cancelSearchButton = new ImageButton(Vec2f.Zero, new Vec2f(200, 200), cancelSearchButtonStyle.Normal, cancelSearchButtonStyle)
            {
                RenderIfTransparent = false,

                TintImageWhenEngaged = false,
                TintImageWhenOff = false,

                ScaleImageOnScale = false
            };

            cancelSearchButton.ImageSize = new Vec2f(0.5f, 0.5f);
            cancelSearchButton.ImagePosition = new Vec2f(0.25f, 0.25f);

            ApplyButtonUXData(cancelSearchButton, DefaultButtonUXData);

            DefaultProvider<TextField> fieldProvider = new DefaultProvider<TextField>(field);
            DefaultProvider<Button> cancelButtonProvider = new DefaultProvider<Button>(cancelSearchButton);

            TextFieldSearchAdapter searchAdapter = new TextFieldSearchAdapter(stringComparisonType: stringComparisonType,
                                                                              targetPosition: targetPosition,
                                                                              targetCancelButtonPosition: targetCancelButtonPosition,
                                                                              targetSearchButtonPosition: Vec2fValue.AbsoluteZero,
                                                                              targetSize: targetSize,
                                                                              targetCancelButtonSize: targetCancelButtonSize,
                                                                              targetSearchButtonSize: Vec2fValue.AbsoluteZero,
                                                                              fieldProvider: fieldProvider,
                                                                              cancelButtonProvider: cancelButtonProvider,
                                                                              searchButtonProvider: null,
                                                                              searchStreamProvider: searchStreamProvider,
                                                                              onHide: onHide,
                                                                              onSearchBegin: onSearchBegin,
                                                                              onSearchFound: onSearchFound,
                                                                              onSearchNotFound: onSearchNotFound,
                                                                              onReset: onReset,
                                                                              focusOnSubsequentActivation: focusOnSubsequentActivation,
                                                                              maxParallelism: 4)
            {
                Group = searchGroup,
                TargetGroupPosition = searchGroupPosition,
                TargetGroupSize = searchGroupSize,

                AddToAttachedWidgetIfGroup = searchGroup is null
            };

            targetWidget.Adapters.Add(searchAdapter);

            return searchAdapter;
        }

        private void InitPropertyGroupStyles(int cornerRadius, int borderThickness,
                                            out GroupWidget.GroupStyle style,
                                            out GroupWidget.GroupStyle style_Circle)
        {
            Color fillColor = BackgroundedLabelTint.ScaleRGB(0.6f);
            Color borderColor = BackgroundedLabelTint.ScaleRGB(1.5f);

            Texture2D texture = TexMaker.RoundedRectangle(geo.GraphicsDevice, RoundedRectangleTextureSize, RoundedRectangleTextureSize,
                                                          cornerRadius, 
                                                          borderThickness, 
                                                          fillColor, borderColor);

            Slice slice = new Slice(texture, SliceCountMode.Nine, new SliceSizeData(TextButtonCornerRadius + WindowBorderThickness));

            style = new GroupWidget.GroupStyle
            {
                RenderData = RenderData.Custom(slice)
            };

            style_Circle = new GroupWidget.GroupStyle
            {
                RenderData = RenderData.BorderedCircle(borderThickness: FloatValue.Absolute(WindowBorderThickness),
                                                       borderColor: borderColor,
                                                       fillColor: fillColor,
                                                       segments: 50)
            };
        }

        private void InitBackgroundedLabelStyles(out Label.LabelStyle style, out Label.LabelStyle style_PropertyStyle)
        {
            style = new Label.LabelStyle
            {
                Normal = RenderData.Custom
                (
                    new Slice(TextFieldTexture, SliceCountMode.Nine, new SliceSizeData(GeoMath.RoundAwayFromZero(TextFieldCornerRadius)),
                              tint: BackgroundedLabelTint)
                ),

                TextNormal = new TextRenderData
                {
                    FontColor = BackgroundedLabelTextTint,
                    FontEffect = GeneralPlainLabelTextEffect,
                }
            };

            style_PropertyStyle = new Label.LabelStyle
            {
                Normal = PropertyGroupStyle.RenderData,

                TextNormal = new TextRenderData
                {
                    FontColor = BackgroundedLabelTextTint,
                    FontEffect = GeneralPlainLabelTextEffect,
                }
            };
        }

        private void InitDrawerData(out GroupWidget.GroupStyle style, out Vec2fValue coverButtonPosition, out Vec2fValue coverButtonSize)
        {
            style = PropertyGroupStyle;

            coverButtonPosition = Vec2fValue.Normalized(0f);
            coverButtonSize = Vec2fValue.Normalized(1f);

            defaultDrawerUXData = new DrawerUXData
            {
                NormalizedChildStartPosition = new Vec2f(0.2f, 1.5f),
                NormalizedChildSize = Vec2f.One,
                NormalizedAdditionalSpacing = new Vec2f(0f, 0.3f),
                NormalizedRetreatAmount = new Vec2f(-0.5f),
                RetreatMode = UI.Drawer.RetreatFunction.RelativeToDrawer,

                ShowDuration = 0.2f,

                Interpolation = new Interpolation.PowOutInterp(2),

                ApplyAnimationsToHiding = true,

                AnimateParentChildren = true,
                AnimateParentChildrenWithInterpolation = true
            };

            Vec2f[] triangleButtonNormalVertices = new Vec2f[3]
            {
                new Vec2f(0.25f, 0.1875f),
                new Vec2f(1.1875f, 0.5f),
                new Vec2f(0.25f, 0.8125f)
            };

            int triangleButtonVertexCount = triangleButtonNormalVertices.Length;

            Color buttonNormalColor = TextButtonStyle.TextNormal.FontColor;
            Color buttonHoveredColor = buttonNormalColor.ScaleRGB(0.85f);
            Color buttonPressedColor = buttonNormalColor.ScaleRGB(0.7f);

            drawerCoverButtonStyle = new Button.ButtonStyle
            {
                Normal = RenderData.SolidPolygon(triangleButtonNormalVertices, triangleButtonVertexCount, buttonNormalColor),
                Hovered = RenderData.SolidPolygon(triangleButtonNormalVertices, triangleButtonVertexCount, buttonHoveredColor),
                Pressed = RenderData.SolidPolygon(triangleButtonNormalVertices, triangleButtonVertexCount, buttonPressedColor),

                HandOnHover = TextButtonStyle.HandOnHover,
            };

            drawerCoverTextButtonStyle = new TextButton.TextButtonStyle
            {
                Normal = drawerCoverButtonStyle.Normal,
                Hovered = drawerCoverButtonStyle.Hovered,
                Pressed = drawerCoverButtonStyle.Pressed,

                Checked = drawerCoverButtonStyle.Checked,
                CheckedHovered = drawerCoverButtonStyle.CheckedHovered,
                CheckedPressed = drawerCoverButtonStyle.CheckedPressed,

                TextNormal = TextRenderData.Copy(TextButtonStyle.TextNormal, false),
                TextHovered = TextRenderData.Copy(TextButtonStyle.TextHovered, false),
                TextPressed = TextRenderData.Copy(TextButtonStyle.TextPressed, false),

                HandOnHover = drawerCoverButtonStyle.HandOnHover,
            };

            drawerCoverTextButtonStyle.TextNormal.FontColor = buttonNormalColor;
            drawerCoverTextButtonStyle.TextHovered.FontColor = buttonHoveredColor;
            drawerCoverTextButtonStyle.TextPressed.FontColor = buttonPressedColor;
        }

        // this captures data and ActorAction(s) within the event delegates
        public void AddScrollBarUXActions(ScrollBar hScrollBar, ScrollBar vScrollBar, ScrollBarUXData uxData)
        {
            if (uxData.GrowAmount == 0f) // do not add event delegates if it wouldn't end up doing anything
            {
                if (uxData.HideHorizontal)
                {
                    hScrollBar.DisableAndHide();
                }

                if (uxData.HideVertical)
                {
                    vScrollBar.DisableAndHide();
                }

                return;
            }

            if (uxData.HideHorizontal)
            {
                hScrollBar.DisableAndHide();
            }
            else if (uxData.GrowAmount != 0f)
            {
                AddHorizontalScrollBarUXDelegates(hScrollBar, uxData.GrowDuration, uxData.GrowAmount, uxData.GrowInterpolation);
            }

            if (uxData.HideVertical)
            {
                vScrollBar.DisableAndHide();
            }
            else if (uxData.GrowAmount != 0f)
            {
                AddVerticalScrollBarUXDelegates(vScrollBar, duration: uxData.GrowDuration, uxData.GrowAmount, uxData.GrowInterpolation);
            }
        }

        // FYI: This captures parameters
        private void AddVerticalScrollBarUXDelegates(ScrollBar scrollBar, float duration, float growAmount, Interpolation interpolation)
        {
            scrollBar.Listener.MouseEnter += delegate
            {
                SizeByAction sizeAction = GetNewAction<SizeByAction>();
                sizeAction.Duration = duration;
                sizeAction.Interpolation = interpolation;
                sizeAction.WidthAmount = growAmount;
                sizeAction.HeightAmount = 0f;

                MoveByAction moveAction = GetNewAction<MoveByAction>();
                moveAction.Duration = duration;
                moveAction.Interpolation = interpolation;
                moveAction.AmountX = -growAmount * 0.5f;
                moveAction.AmountY = 0f;

                scrollBar.AddAction(sizeAction);
                scrollBar.AddAction(moveAction);
            };

            scrollBar.Listener.MouseExit += delegate
            {
                if (Game.Instance.Geo.Input.mouse.AnyDown)
                {
                    return;
                }

                SizeByAction sizeAction = GetNewAction<SizeByAction>();
                sizeAction.Duration = duration;
                sizeAction.Interpolation = interpolation;
                sizeAction.WidthAmount = -growAmount;
                sizeAction.HeightAmount = 0f;

                MoveByAction moveAction = GetNewAction<MoveByAction>();
                moveAction.Duration = duration;
                moveAction.Interpolation = interpolation;
                moveAction.AmountX = growAmount * 0.5f;
                moveAction.AmountY = 0f;

                scrollBar.AddAction(sizeAction);
                scrollBar.AddAction(moveAction);
            };
        }

        // FYI: Captures parameters
        private void AddHorizontalScrollBarUXDelegates(ScrollBar scrollBar, float duration, float growAmount, Interpolation interpolation)
        {
            scrollBar.Listener.MouseEnter += delegate
            {
                SizeByAction sizeAction = GetNewAction<SizeByAction>();
                sizeAction.Duration = duration;
                sizeAction.Interpolation = interpolation;
                sizeAction.WidthAmount = 0f;
                sizeAction.HeightAmount = growAmount;

                MoveByAction moveAction = GetNewAction<MoveByAction>();
                moveAction.Duration = duration;
                moveAction.Interpolation = interpolation;
                moveAction.AmountX = 0f;
                moveAction.AmountY = -growAmount * 0.5f;

                scrollBar.AddAction(sizeAction);
                scrollBar.AddAction(moveAction);
            };

            scrollBar.Listener.MouseExit += delegate
            {
                if (Game.Instance.Geo.Input.mouse.AnyDown)
                {
                    return;
                }

                SizeByAction sizeAction = GetNewAction<SizeByAction>();
                sizeAction.Duration = duration;
                sizeAction.Interpolation = interpolation;
                sizeAction.WidthAmount = 0f;
                sizeAction.HeightAmount = -growAmount;
                AddAction_OnRemoved_Return(sizeAction);

                MoveByAction moveAction = GetNewAction<MoveByAction>();
                moveAction.Duration = duration;
                moveAction.Interpolation = interpolation;
                moveAction.AmountX = 0f;
                moveAction.AmountY = growAmount * 0.5f;

                scrollBar.AddAction(sizeAction);
                scrollBar.AddAction(moveAction);
            };
        }

        public Window Window(Vec2f position, Vec2f size, string title,
                             bool withCloseButton = DEFAULT_WINDOWS_HAVE_CLOSE_BUTTON,
                             Action onClose = null)
        {
            float titleBarHeight = GetWindowTitleBarHeight();

            return Window(position, size, title,
                          borderThickness: WindowBorderThickness, 
                          titleBarHeight: titleBarHeight,
                          withCloseButton: withCloseButton,
                          onClose: onClose);
        }

        public Window Window(Vec2f position, Vec2f size, string title,
                             float borderThickness, 
                             float titleBarHeight,
                             bool withCloseButton = DEFAULT_WINDOWS_HAVE_CLOSE_BUTTON,
                             Action onClose = null)
        {
            Window.WindowStyle style = Copyables.Cast<Window.WindowStyle>(WindowStyle);

            Window window = new Window(position, size, style, MainFont, title,
                borderThickness: borderThickness,
                titleBarHeight: titleBarHeight,
                closeButtonWidth: 0.05f,
                behavior: WindowBehavior)
            {
                TintChildren = true
            };

            FinishWindowCommons(window, (int)titleBarHeight, 
                                withCloseButton: withCloseButton,
                                onClose: onClose);

            return window;
        }

        public SaveLoadDialog SaveLoadDialog(Vec2f size,
                                             SaveLoadDialog.SaveRequestedDelegate onSaveRequested,
                                             SaveLoadDialog.SaveBeforeLoadRequestedDelegate onSaveBeforeLoadRequested,
                                             SaveLoadDialog.LoadRequestedDelegate onLoadRequested,
                                             Action<string> onError,
                                             SaveLoadDialog.MetadataGetterDelegate metadataGetter,
                                             string root,
                                             SaveLoadDialog.FileConfiguration fileConfig,
                                             SaveLoadDialog.SaveGroupAdditionalMetadataLabelGetterDelegate saveGroupAdditionalMetadataLabelGetter = null,
                                             bool isModal = true,
                                             bool nonModalHideOnUnfocus = false)
        {
            PlainLabel GroupNameLabelGetter(Vec2f position, Vec2f size)
            {
                return PlainLabel(position, size, TextUtils.EmptyString);
            }

            PlainLabel GroupLastModDateLabelGetter(Vec2f position, Vec2f size)
            {
                return PlainLabel(position, size, TextUtils.EmptyString);
            }

            PlainLabel DefaultAdditionalMetadataLabelGetter(Vec2f position, Vec2f size)
            {
                return PlainLabel(position, size, TextUtils.EmptyString);
            }

            if (saveGroupAdditionalMetadataLabelGetter is null)
            {
                saveGroupAdditionalMetadataLabelGetter = DefaultAdditionalMetadataLabelGetter;
            }

            void OnSaveGroupInitialized(SaveLoadDialog.SaveGroupWidget saveGroup)
            {
                SetFontOfGroup(saveGroup, MainFont);
            }

            void OnSaveGroupDoubleClickButtonAdapterAdded(ButtonAdapter buttonAdapter)
            {
                ApplyButtonUXData(buttonAdapter, DefaultButtonUXData);
            }

            TextButton okButton = TextButton(Vec2f.Zero, Vec2f.Zero, "Yes");
            TextButton cancelButton = TextButton(Vec2f.Zero, Vec2f.Zero, "Cancel");
            TextButton noButton = TextButton(Vec2f.Zero, Vec2f.Zero, "No");
            PlainLabel nameInputLabel = PlainLabel(Vec2f.Zero, Vec2f.Zero, TextUtils.EmptyString);
            TextField nameInputField = GeneralTextField(Vec2f.Zero, Vec2f.Zero, int.MaxValue);
            ScrollPane scrollPane = ScrollPane(Vec2f.Zero, Vec2f.Zero);

            Vec2fValue normalizedEdgeSpacing = Vec2fValue.NormalizedMin(0.01f);

            FloatValue normalizedHorizontalButtonSpacing = FloatValue.Normalized(0.01f);

            FloatValue normalizedButtonPositionY = FloatValue.Normalized(1f - normalizedHorizontalButtonSpacing.Value);

            FloatValue scrollPanePositionY = FloatValue.Normalized(0.01f);

            FloatValue verticalGroupSpacing = FloatValue.Normalized(0.01f);

            Vec2fValue normalizedOkButtonPosition = Vec2fValue.Normalized(normalizedHorizontalButtonSpacing.Value, normalizedButtonPositionY.Value);
            Vec2fValue normalizedCancelButtonPosition = Vec2fValue.Normalized(1f - normalizedHorizontalButtonSpacing.Value, normalizedButtonPositionY.Value);
            Vec2fValue normalizedButtonSize = Vec2fValue.Normalized(0.35f, 0.1f);

            FloatValue normalizedNameInputHeight = FloatValue.Normalized(normalizedButtonSize.Value.Y);

            FloatValue normalizedNameInputPositionY = FloatValue.Normalized(normalizedButtonPositionY.Value - normalizedNameInputHeight.Value - normalizedHorizontalButtonSpacing.Value);

            Vec2fValue normalizedNameInputLabelPosition = Vec2fValue.Normalized
            (
                normalizedOkButtonPosition.Value.X,
                normalizedNameInputPositionY.Value
            );

            Vec2fValue normalizedNameInputFieldSize = Vec2fValue.Normalized
            (
                (normalizedCancelButtonPosition.Value.X + normalizedButtonSize.Value.X) - normalizedButtonPositionY.Value,
                normalizedNameInputHeight.Value
            );

            Vec2fValue normalizedNameInputFieldPosition = Vec2fValue.Normalized
            (
                normalizedNameInputLabelPosition.Value.X + normalizedHorizontalButtonSpacing.Value,
                normalizedNameInputPositionY.Value
            );

            return new SaveLoadDialog(size: size,
                                      style: WindowStyle,
                                      font: MainFont,
                                      isModal: isModal,
                                      nonModalHideOnUnfocus: nonModalHideOnUnfocus,
                                      borderThickness: WindowBorderThickness,
                                      titleBarHeight: GetWindowTitleBarHeight(),
                                      closeButtonWidth: 0.05f,
                                      onShowRequested: game.ShowDialog,
                                      onSaveRequested: onSaveRequested,
                                      onSaveBeforeLoadRequested: onSaveBeforeLoadRequested,
                                      onLoadRequested: onLoadRequested,
                                      onError: onError,
                                      onSaveGroupInitialized: OnSaveGroupInitialized,
                                      behavior: WindowBehavior,
                                      okButton: okButton,
                                      noButton: noButton,
                                      cancelButton: cancelButton,
                                      scrollPane: scrollPane,
                                      saveAsNameLabel: nameInputLabel,
                                      nameInputField: nameInputField,
                                      metadataGetter: metadataGetter,
                                      saveGroupNameLabelGetter: GroupNameLabelGetter,
                                      saveGroupLastModDateLabelGetter: GroupLastModDateLabelGetter,
                                      saveGroupAdditionalMetadataLabelGetter: saveGroupAdditionalMetadataLabelGetter,
                                      onSaveGroupDoubleClickButtonAdapterAdded: OnSaveGroupDoubleClickButtonAdapterAdded,
                                      saveGroupStyle: SaveLoadDialogGroupStyle,
                                      scrollPanePositionY: scrollPanePositionY,
                                      verticalGroupSpacing: verticalGroupSpacing,
                                      okButtonPosition: normalizedOkButtonPosition,
                                      cancelButtonPosition: normalizedCancelButtonPosition,
                                      buttonSize: normalizedButtonSize,
                                      horizontalButtonSpacing: normalizedHorizontalButtonSpacing,
                                      nameInputLabelPosition: normalizedNameInputLabelPosition,
                                      nameInputFieldPosition: normalizedNameInputFieldPosition,
                                      nameInputFieldSize: normalizedNameInputFieldSize,
                                      edgeSpacing: normalizedEdgeSpacing,
                                      root: root,
                                      fileConfig: fileConfig);
        }

        public DialogBox ModalWindowWithScrollPane(Vec2f size, string title, float scrollPaneNegativePaddingY,
                                                   ScrollBarUXData? scrollBarUXData = null, Action onClose = null)
        {
            Vec2f scrollPanePosition = Vec2f.Zero;
            Vec2f scrollPaneSize = new Vec2f(size.X, (size.Y - scrollPanePosition.Y) - scrollPaneNegativePaddingY);
            ScrollPane scrollPane = ScrollPane(scrollPanePosition, scrollPaneSize, scrollBarUXData);
            scrollPane.HScrollBar.DisableAndHide();

            DialogBox window = ModalWindow(size, title, onClose);
            window.AddChild(scrollPane);

            return window;
        }

        public DialogBox NonModalWindowWithScrollPane(Vec2f size, string title, float scrollPaneNegativePaddingY,
                                                      ScrollBarUXData? scrollBarUXData = null,
                                                      Action onClose = null,
                                                      bool withXButton = true)
        {
            return NonModalWindowWithScrollPane(size: size,
                                                title: title,
                                                scrollPaneSpacingY: 0f,
                                                scrollPaneNegativePaddingY: scrollPaneNegativePaddingY,
                                                scrollBarUXData: scrollBarUXData,
                                                onClose: onClose,
                                                withXButton: withXButton);
        }

        public DialogBox NonModalWindowWithScrollPane(Vec2f size, string title,
                                                      float scrollPaneSpacingY,
                                                      float scrollPaneNegativePaddingY,
                                                      ScrollBarUXData? scrollBarUXData = null,
                                                      Action onClose = null,
                                                      bool withXButton = true)
        {
            Vec2f scrollPanePosition = new Vec2f(0f, scrollPaneSpacingY);
            Vec2f scrollPaneSize = new Vec2f(size.X, (size.Y - scrollPanePosition.Y) - scrollPaneNegativePaddingY);
            ScrollPane scrollPane = ScrollPane(scrollPanePosition, scrollPaneSize, scrollBarUXData);
            scrollPane.HScrollBar.DisableAndHide();

            DialogBox window = NonModalWindow(size, title, onClose, withXButton: withXButton);
            window.AddChild(scrollPane);

            return window;
        }

        public DialogBox ModalWindow(Vec2f size, string title, Action onClose = null)
        {
            float titleBarHeight = GetWindowTitleBarHeight();

            return ModalWindow(size, title,
                                 borderThickness: WindowBorderThickness, titleBarHeight: titleBarHeight,
                                 onClose: onClose);
        }

        public DialogBox ModalWindow(Vec2f size, string title,
                                   float borderThickness, float titleBarHeight,
                                   Action onClose = null,
                                   bool withXButton = true)
        {
            return DialogWindow(size, title, isModal: true, nonModalHideOnUnfocus: false, borderThickness, titleBarHeight, onClose, withXButton);
        }

        public DialogBox NonModalWindow(Vec2f size, string title,
                                        Action onClose = null,
                                        bool withXButton = true)
        {
            float titleBarHeight = GetWindowTitleBarHeight();

            return NonModalWindow(size, title,
                                 borderThickness: WindowBorderThickness, titleBarHeight: titleBarHeight,
                                 onClose: onClose,
                                 withXButton: withXButton);
        }

        public DialogBox NonModalWindow(Vec2f size, string title,
                                   float borderThickness, float titleBarHeight,
                                   Action onClose = null,
                                   bool withXButton = true)
        {
            return DialogWindow(size, title, isModal: false, nonModalHideOnUnfocus: false, borderThickness, titleBarHeight, onClose, withXButton);
        }

        public DialogBox DialogWindow(Vec2f size, string title, bool isModal, bool nonModalHideOnUnfocus,
                                   float borderThickness, float titleBarHeight,
                                   Action onClose = null,
                                   bool withXButton = true)
        {
            Window.WindowStyle style = WindowStyle;

            DialogBox window = new DialogBox(size, style, MainFont, title,
                borderThickness: borderThickness,
                titleBarHeight: titleBarHeight,
                isModal: isModal,
                nonModalHideOnUnfocus: nonModalHideOnUnfocus,
                closeButtonWidth: 0.05f,
                behavior: WindowBehavior)
            {
                TintChildren = true
            };

            window.OnShow += delegate (DialogBox dialog)
            {
                if (dialog.IsModal)
                {
                    game.DisableAllUI(dialog);
                }
            };

            window.OnHide += delegate (DialogBox dialog)
            {
                if (dialog.IsModal)
                {
                    game.EnableAllUI();
                }
            };

            FinishWindowCommons(window, (int)titleBarHeight, onClose, withXButton);

            return window;
        }

        public void FinishWindowCommons(Window window, float titleBarHeight,
                                         Action onClose = null,
                                         bool withCloseButton = true,
                                         bool withDefaultMouseHandler = true)
        {
            window.TitleBarLabel.Style.TextNormal = new TextRenderData { FontColor = GeneralTextColor };
            window.TitleBarLabel.CurrentTextRenderData = window.TitleBarLabel.Style.TextNormal;
            window.Style.RenderData.SetColor(WindowTint);

            if (withCloseButton)
            {
                window.CloseButtonWidth = titleBarHeight;

                ImageButton.ImageButtonStyle closeButtonStyle = Copyables.Cast<ImageButton.ImageButtonStyle>(WindowCloseButtonStyle, false);

                closeButtonStyle.ImageNormal = RenderData.Lines(GetWindowCloseButtonXLineData(out int lineCount), lineCount, linesClosed: false);

                ImageButton closeButton = new ImageButton(Vec2f.Zero, new Vec2f(200, 200), closeButtonStyle.Normal, closeButtonStyle)
                {
                    RenderIfTransparent = false,

                    TintImageWhenEngaged = false,
                    TintImageWhenOff = false,

                    ScaleImageOnScale = false,

                    OnClick = onClose
                };

                window.CloseButton = closeButton;

                closeButton.ImageSize = new Vec2f(0.5f, 0.5f);
                closeButton.ImagePosition = new Vec2f(0.25f, 0.25f);

                Interpolation closeButtonScaleInterpolation = Interpolation.Smooth2;
                ScaleToAction closeButtonHoverAction = ActorActions.ScaleTo(new Vec2f(1.15f), 0.05f, closeButtonScaleInterpolation);
                ScaleToAction closeButtonOffAction = ActorActions.ScaleTo(Vec2f.One, 0.05f, closeButtonScaleInterpolation);

                closeButton.OnClick += delegate
                {
                    if (closeButton.ContainsAction(closeButtonOffAction))
                    {
                        closeButton.RemoveAction(closeButtonOffAction);
                    }

                    if (closeButton.ContainsAction(closeButtonHoverAction))
                    {
                        closeButton.RemoveAction(closeButtonHoverAction);
                    }

                    closeButton.Scale = Vec2f.One;
                };

                closeButton.OnOver += delegate
                {
                    if (closeButton.ContainsAction(closeButtonOffAction))
                    {
                        closeButton.RemoveAction(closeButtonOffAction);
                    }

                    closeButtonHoverAction.Restart();

                    closeButton.AddAction(closeButtonHoverAction);
                };

                closeButton.OnExit += delegate
                {
                    if (closeButton.ContainsAction(closeButtonHoverAction))
                    {
                        closeButton.RemoveAction(closeButtonHoverAction);
                    }

                    closeButtonOffAction.Restart();

                    closeButton.AddAction(closeButtonOffAction);
                };
            }

            if (withDefaultMouseHandler)
            {
                AddDefaultMouseClickListener(window);
            }
        }

        public DialogBox HorizontalModalDialog(Vec2f size, Vec2f relativeButtonPadding, float relativeButtonHeight, string okText, string cancelText, Action onOk, Action onCancel)
        {
            Window.WindowStyle style = WindowStyle;

            DialogBox dialog = new DialogBox(size, style, isModal: true, nonModalHideOnUnfocus: true);

            Vec2f buttonBegin = size * relativeButtonPadding;
            Vec2f buttonEnd = size - (size * relativeButtonPadding);
            Vec2f buttonSize = new Vec2f((size.X - (size.X * relativeButtonPadding.X)) - (size.X * relativeButtonPadding.X), size.Y * relativeButtonHeight);
            Vec2f relativeButtonSize = buttonSize / size;

            Vec2f okButtonRelativePosition = relativeButtonPadding;
            Vec2f cancelButtonRelativePosition = new Vec2f(okButtonRelativePosition.X + buttonSize.X + relativeButtonPadding.X, relativeButtonPadding.Y);

            dialog.AddButton(okButtonRelativePosition, relativeButtonSize, MainFont, TextButtonStyle, okText)
                             .OnClick += onOk;

            dialog.AddButton(cancelButtonRelativePosition, relativeButtonSize, MainFont, TextButtonStyle, cancelText)
                            .OnClick += onCancel;

            return dialog;
        }

        public NumberSpinner<float> NumberSpinner(Vec2f position, Vec2f size, float min, float max, float increment, float start, int maxCharacters,
                                                         Widget view,
                                                         Button incrementButton,
                                                         Button decrementButton,
                                                         Vec2f viewPosition,
                                                         Vec2f viewSize,
                                                         Vec2f incrementPosition,
                                                         Vec2f decrementPosition,
                                                         Vec2f buttonSize,
                                                         bool allowCycling = false,
                                                         Texture2D background = null)
        {
            GroupWidget.GroupStyle style = new GroupWidget.GroupStyle
            {
                RenderData = background is null ? null : RenderData.Texture(background),
            };

            NumberSpinner<float> spinner = new NumberSpinner<float>(position, size, min, max, increment, start)
            {
                ViewPosition = viewPosition,
                ViewSize = viewSize,
                IncrementButtonPosition = incrementPosition,
                DecrementButtonPosition = decrementPosition,
                ButtonSize = buttonSize,

                AllowCycling = allowCycling,

                Style = style,

                TintChildren = true,

                Precision = 5
            };

            spinner.View = view;
            spinner.IncrementButton = incrementButton;
            spinner.DecrementButton = decrementButton;
            spinner.IncrementButton.AllowHold = true;
            spinner.DecrementButton.AllowHold = true;
            spinner.IncrementButton.AllowHoldRepeat = true;
            spinner.DecrementButton.AllowHoldRepeat = true;
            spinner.IncrementButton.OnHold = spinner.Increment;
            spinner.DecrementButton.OnHold = spinner.Decrement;

            spinner.CurrentValue = start;

            spinner.Layout();

            return spinner;
        }

        public NumberSpinner<float> DefaultNumberSpinner(Vec2f position, Vec2f size, float min, float max, float increment, float start, int maxCharacters,
                                                         bool allowCycling = false,
                                                         Texture2D background = null)
        {
            NumberRange<double> range = NumberRange<double>.From(min, max);
            TextNumberFilterData<double> numberFilterData = new TextNumberFilterData<double>(range,
                                                                                             allowedSign: 0,
                                                                                             allowFractional: true);

            TextField view = GeneralTextField(Vec2f.Zero, size * SpinnerViewSize, maxCharacters, defaultText: null);
            view.TextFilter = TextFilter.Numeric(numberFilterData);
            view.TintCaret = false;
            view.TintText = false;
            view.SelectAllOnFocus = false;
            view.Text = start.ToString();

            Vec2f baseButtonSize = size * SpinnerButtonSize;

            //Button incrementButton = Button(Vec2f.Zero, new Vec2f(50, 50), SpinnerIncrementButtonTexture);
            //Button decrementButton = Button(Vec2f.Zero, new Vec2f(50, 50), SpinnerDecrementButtonTexture);
            TextButton incrementButton = TextButton(Vec2f.Zero, baseButtonSize, "+");
            TextButton decrementButton = TextButton(Vec2f.Zero, baseButtonSize, "-");

            return NumberSpinner(position, size, min, max, increment, start, maxCharacters,
                                 view,
                                 incrementButton,
                                 decrementButton,
                                 SpinnerViewPosition,
                                 SpinnerViewSize,
                                 SpinnerIncrementButtonPosition,
                                 SpinnerDecrementButtonPosition,
                                 SpinnerButtonSize,
                                 allowCycling,
                                 background);
        }

        public LabelTooltip AddTextTooltip(Widget widget, string text)
        {
            LabelTooltip tooltip = TextTooltip(text);

            widget.AddListener(tooltip);

            return tooltip;
        }

        public LabelTooltip TextTooltip(string text)
        {
            return TextTooltip(text, DefaultLabelTooltipCursorOffsetGetter);
        }

        public LabelTooltip TextTooltip(string text, LabelTooltip.CursorOffsetDelegate cursorOffsetGetter)
        {
            Label.LabelStyle style = new Label.LabelStyle
            {
                Normal = RenderData.Texture(TooltipTexture),

                TextNormal = new TextRenderData
                {
                    FontColor = GeneralTextColor,
                    Padding = Vec2fValue.Absolute(TooltipSizePadding)
                }
            };

            LabelTooltip tooltip = new LabelTooltip(text, MainFont, style,
                                                    hoverTime: TooltipHoverTime,
                                                    offTime: TooltipOffTime,
                                                    lockToMouse: true,
                                                    positionAtMouse: true,
                                                    fitTooltip: true,
                                                    fitTooltipIfTargetClips: true,
                                                    cursorOffsetGetter: cursorOffsetGetter);

            tooltip.OnShow += delegate (Label label)
            {
                label.CharacterSpacing = game.Config.Game.GlobalCharacterSpacing;

                if (label.Font != MainFont)
                {
                    label.Font = MainFont;
                }
            };

            return tooltip;
        }

        public Vec2f DefaultLabelTooltipCursorOffsetGetter(Widget attachedWidget, Label label)
        {
            // Positions the bottom-left corner of the Label at the cursor position

            return new Vec2f(0f, -label.Size.Y - ComputeTooltipAdditionalCursorOffset());
        }

        public float ComputeTooltipAdditionalCursorOffset()
        {
            return ScaleMin(game.ScaleByDisplayResolution_Min(1f));
        }

        public LineData[] GetCheckmarkUncheckedLineData()
        {
            float thickness = 0.0475f;

            return new LineData[2]
            {
                // Checkmark lines

                new LineData
                {
                    Thickness = thickness,

                    Color = Color.Transparent,

                    Line = new Line
                    {
                        Start = new Vec2f(0.34f, 0.49f),
                        End = new Vec2f(0.5f, 0.7f)
                    }
                },

                new LineData
                {
                    Thickness = thickness,

                    Color = Color.Transparent,

                    Line = new Line
                    {
                        Start = new Vec2f(0.5f, 0.7f),
                        End = new Vec2f(0.8f, 0.2f),
                    },
                }
            };
        }

        private LineData[] GetWindowCloseButtonXLineData(out int lineCount)
        {
            float thickness = 0.15f;
            Color color = WindowCloseButtonImageTint;

            lineCount = 2;

            return new LineData[2]
            {
                new LineData
                (
                    line: new Line
                    (
                        start: new Vec2f(0.1f),
                        end: new Vec2f(0.9f)
                    ),

                    thickness: thickness,

                    color: color
                ),

                new LineData
                (
                    line: new Line
                    (
                        start: new Vec2f(0.9f, 0.1f),
                        end: new Vec2f(0.1f, 0.9f)
                    ),

                    thickness: thickness,

                    color: color
                )
            };
        }

        public static void SetFontOfGroup(GroupWidget group, DynamicSpriteFont font)
        {
            group.SetFontOfTextWidgets(font);
        }

        public static void SetFontOfGroup<T>(T[] widgets, DynamicSpriteFont font) where T : ITextWidget
        {
            for (int index = 0; index < widgets.Length; index++)
            {
                widgets[index].Font = font;
            }
        }

        // Sets the character spacing of every ITextWidget in group to game.GlobalCharacterSpacing.
        public void SetCharacterSpacingOfGroup(GroupWidget group)
        {
            group.ForEachOfType<ITextWidget>(textWidget => textWidget.CharacterSpacing = game.Config.Game.GlobalCharacterSpacing);
        }

        public bool TrySetStyle(Widget widget, string styleName)
        {
            if (widget is null || TextUtils.IsNullEmptyOrWhitespace(styleName))
            {
                return false;
            }

            object style = GetStyle(styleName);

            if (style is null)
            {
                return false;
            }

            switch (widget)
            {
                case TextButton textButton:
                    textButton.Style = (TextButton.TextButtonStyle)style;
                    break;

                case ImageButton imageButton:
                    imageButton.Style = (ImageButton.ImageButtonStyle)style;
                    break;

                case TextField textField:
                    textField.Style = (TextField.TextFieldStyle)style;
                    break;

                case Label label:
                    label.Style = (Label.LabelStyle)style;
                    break;

                case Window window:
                    window.Style = (Window.WindowStyle)style;
                    break;

                case Drawer drawer:
                    SetGroupStyle(drawer, (GroupWidget.GroupStyle)style);
                    break;

                case ScrollPane scrollPane:
                    SetScrollPaneStyle(scrollPane, (ScrollPane.ScrollPaneStyle)style);
                    break;

                case GroupWidget group:
                    SetGroupStyle(group, (GroupWidget.GroupStyle)style);
                    break;

                default:
                    return false;
            }

            return true;
        }

        public T GetStyle<T>(string name)
        {
            object style = GetStyle(name);

            if (style is null)
            {
                return default;
            }

            return (T)style;
        }

        public object GetStyle(string name)
        {
            if (!widgetStyles.TryGetValue(name, out object style))
            {
                return null;
            }

            return style;
        }

        public void AddOrSetStyle(string name, object style)
        {
            widgetStyles[name] = style;
        }

        public static void SetGroupStyle(GroupWidget groupStyle, GroupWidget.GroupStyle style)
        {
            groupStyle.Style = style;

            groupStyle.CurrentRenderData = style?.RenderData;
        }

        public static void SetScrollPaneStyle(ScrollPane scrollPane, ScrollPane.ScrollPaneStyle style)
        {
            scrollPane.Style = style;

            scrollPane.CurrentRenderData = style.RenderData;

            TrySetScrollbarStyle(scrollPane.HScrollBar, style.BarStyle);
            TrySetScrollbarStyle(scrollPane.VScrollBar, style.BarStyle);
        }

        public static bool TrySetScrollbarStyle(ScrollBar scrollbar, ScrollBar.ScrollBarStyle style)
        {
            if (scrollbar is null)
            {
                return false;
            }

            scrollbar.Style = style;

            scrollbar.CurrentRenderData = style?.Normal;

            return true;
        }

        private void InitWidgetStyles()
        {
            widgetStyles.Add("TextLinkStyle", globalTextLinkStyle);
            widgetStyles.Add("DrawerCoverTextButtonStyle", drawerCoverTextButtonStyle);
            widgetStyles.Add("DrawerCoverButtonStyle", drawerCoverButtonStyle);
            widgetStyles.Add("SaveLoadDialogGroupStyle", SaveLoadDialogGroupStyle);
            widgetStyles.Add("TextButtonStyle", TextButtonStyle);
            widgetStyles.Add("TextFieldStyle", TextFieldStyle);
            widgetStyles.Add("TextFieldStyle_SharpCorners", TextFieldStyle_SharpCorners);
            widgetStyles.Add("BackgroundedLabelStyle", BackgroundedLabelStyle);
            widgetStyles.Add("BackgroundedPropertyLabelStyle", BackgroundedPropertyLabelStyle);
            widgetStyles.Add("CheckboxStyle", CheckboxStyle);
            widgetStyles.Add("TextCheckButtonStyle", TextCheckButtonStyle);
            widgetStyles.Add("WindowStyle", WindowStyle);
            widgetStyles.Add("WindowCloseButtonStyle", WindowCloseButtonStyle);
            widgetStyles.Add("DrawerStyle", DrawerStyle);
            widgetStyles.Add("PropertyGroupStyle", PropertyGroupStyle);
        }

        private ImageButton.ImageButtonStyle InitCheckboxStyle()
        {
            RenderData checkboxNormalRenderData = RenderData.Custom(new Slice(CheckboxTexture, SliceCountMode.Nine,
                                                                    new SliceSizeData(CheckboxBackgroundCornerRadius + CheckboxBackgroundBorderThickness),
                                                                    tint: CheckboxUncheckedTint));

            RenderData checkboxHoveredRenderData = RenderData.Copy(checkboxNormalRenderData, false);
            checkboxHoveredRenderData.SetColor(CheckboxHoveredTint);

            RenderData checkboxPressedRenderData = RenderData.Copy(checkboxNormalRenderData, false);
            checkboxPressedRenderData.SetColor(CheckboxPressedTint);

            RenderData checkboxCheckedRenderData = RenderData.Copy(checkboxNormalRenderData, false);
            checkboxCheckedRenderData.SetColor(CheckboxCheckedTint);

            LineData[] checkmarkUncheckedLineData = GetCheckmarkUncheckedLineData();

            LineData[] checkmarkHoveredLineData = new LineData[] { checkmarkUncheckedLineData[0], checkmarkUncheckedLineData[1] };
            checkmarkHoveredLineData[0].Color = CheckmarkHoveredTint;
            checkmarkHoveredLineData[1].Color = CheckmarkHoveredTint;

            LineData[] checkmarkCheckedLineData = new LineData[] { checkmarkUncheckedLineData[0], checkmarkUncheckedLineData[1] };
            checkmarkCheckedLineData[0].Color = CheckmarkCheckedTint;
            checkmarkCheckedLineData[1].Color = CheckmarkCheckedTint;

            return new ImageButton.ImageButtonStyle
            {
                Normal = checkboxNormalRenderData,
                Hovered = checkboxHoveredRenderData,
                Pressed = checkboxPressedRenderData,

                Checked = checkboxCheckedRenderData,
                CheckedHovered = checkboxHoveredRenderData,
                CheckedPressed = checkboxPressedRenderData,

                ImageNormal = RenderData.Lines(checkmarkUncheckedLineData, checkmarkUncheckedLineData.Length, linesClosed: false),
                ImageHovered = RenderData.Lines(checkmarkHoveredLineData, checkmarkHoveredLineData.Length, linesClosed: false),
                ImageChecked = RenderData.Lines(checkmarkCheckedLineData, checkmarkCheckedLineData.Length, linesClosed: false),

                HandOnHover = true
            };
        }

        private RenderData InitSaveLoadDialogGroupSelectedRenderData()
        {
            int cornerRadius = TextButtonCornerRadius;
            int borderThickness = WindowBorderThickness;

            SliceSizeData sliceSizeData = new SliceSizeData(cornerRadius + borderThickness);

            Texture2D texture = TexMaker.RoundedRectangle(geo.GraphicsDevice,
                                                          64, 64,
                                                          cornerRadius,
                                                          borderThickness,
                                                          TextButtonHoveredColor,
                                                          DarkWhite);

            return RenderData.Custom(new Slice(texture, SliceCountMode.Nine, sliceSizeData));
        }

        private SaveLoadDialog.SaveGroupStyle InitSaveLoadDialogGroupStyle()
        {
            int groupCornerRadius = TextButtonCornerRadius;

            SliceSizeData groupSliceSizeData = new SliceSizeData(groupCornerRadius);

            SaveLoadDialog.SaveGroupStyle style = new SaveLoadDialog.SaveGroupStyle
            {
                Normal = RenderData.Custom(new Slice(TextButtonTexture, SliceCountMode.Nine, groupSliceSizeData, tint: Color.DarkSlateGray)),
                Hovered = RenderData.Custom(new Slice(TextButtonTexture, SliceCountMode.Nine, groupSliceSizeData, tint: TextButtonHoveredColor)),
                Pressed = RenderData.Custom(new Slice(TextButtonTexture, SliceCountMode.Nine, groupSliceSizeData, tint: TextButtonPressedColor)),

                Selected = this.SaveLoadDialogGroupSelectedRenderData,

                HandOnHover = false,
            };

            return style;
        }

        private void InitGlobalTextLinkStyle()
        {
            textLinkUnclickedColor = new Color(75, 200, 235, 255);
            textLinkHoveredColor = textLinkUnclickedColor.ScaleRGB(1.2f);
            textLinkPressedColor = textLinkUnclickedColor.ScaleRGB(0.9f);
            textLinkClickedColor = textLinkUnclickedColor.ScaleRGB(0.8f);
            textLinkDisabledColor = textLinkUnclickedColor.ScaleRGB(0.65f);

            float underlineThickness_Actuated = TextUnderlineThickness * 2;

            TextDecoration textDecoration_Unactuated = new TextDecoration
            {
                Underline = true,
                UnderlineThickness = TextUnderlineThickness,
            };

            TextDecoration textDecoration_Actuated = new TextDecoration
            {
                Underline = true,
                UnderlineThickness = underlineThickness_Actuated
            };

            globalTextLinkStyle = new TextLinkStyle
            {
                TextNormal = new TextRenderData
                {
                    FontColor = textLinkUnclickedColor,

                    Decoration = textDecoration_Unactuated
                },

                TextHovered = new TextRenderData
                {
                    FontColor = textLinkHoveredColor,

                    Decoration = textDecoration_Actuated
                },

                TextPressed = new TextRenderData
                {
                    FontColor = textLinkPressedColor,

                    Decoration = textDecoration_Actuated
                },

                TextDisabled = new TextRenderData
                {
                    FontColor = textLinkDisabledColor,

                    Decoration = textDecoration_Unactuated
                },

                HasBeenClickedText = new TextRenderData
                {
                    FontColor = textLinkClickedColor
                },

                HandOnHover = true
            };
        }

        private static void InitActionPools()
        {
            int defaultCapacity = 100;
            int growthAmount = 5;

            DefaultPool<MoveByAction> moveByActionPool = new DefaultPool<MoveByAction>(defaultCapacity: defaultCapacity, growthAmount: growthAmount);
            DefaultPool<MoveToAction> moveToActionPool = new DefaultPool<MoveToAction>(defaultCapacity: defaultCapacity, growthAmount: growthAmount);
            DefaultPool<SizeByAction> sizeByActionPool = new DefaultPool<SizeByAction>(defaultCapacity: defaultCapacity, growthAmount: defaultCapacity);
            DefaultPool<ScaleToAction> scaleToActionPool = new DefaultPool<ScaleToAction>(defaultCapacity: defaultCapacity, growthAmount: growthAmount);
            DefaultPool<ScaleToAbsoluteAction> scaleToAbsoluteActionPool = new DefaultPool<ScaleToAbsoluteAction>(defaultCapacity: defaultCapacity, growthAmount: growthAmount);

            Pools.AddPool(moveByActionPool);
            Pools.AddPool(moveToActionPool);
            Pools.AddPool(sizeByActionPool);
            Pools.AddPool(scaleToActionPool);
            Pools.AddPool(scaleToAbsoluteActionPool);
        }

        public static void SetCharacterSpacingOfGroup(GroupWidget group, float spacing)
        {
            group.ForEachOfType<ITextWidget>(textWidget => textWidget.CharacterSpacing = spacing);
        }

        public static void SetTextPadding(TextField.TextFieldStyle style, Vec2fValue padding)
        {
            if (style.TextNormal is not null)
            {
                style.TextNormal.Padding = padding;
            }

            if (style.TextHovered is not null)
            {
                style.TextHovered.Padding = padding;
            }

            if (style.TextInactive is not null)
            {
                style.TextInactive.Padding = padding;
            }

            if (style.TextDisabled is not null)
            {
                style.TextDisabled.Padding = padding;
            }
        }

        public static void SetTextPadding(Label.LabelStyle style, Vec2fValue padding)
        {
            if (style.TextNormal is not null)
            {
                style.TextNormal.Padding = padding;
            }

            if (style.TextDisabled is not null)
            {
                style.TextDisabled.Padding = padding;
            }

            if (style.TextHovered is not null)
            {
                style.TextHovered.Padding = padding;
            }
        }

        public static void InitNumberTextField(TextField textField,
                                               TextNumberFilterData<double> filterData, double? defaultValue,
                                               Action<string> onTextInput)
        {
            textField.TextFilter = TextFilter.Numeric(filterData);

            if (defaultValue.HasValue)
            {
                textField.Text = defaultValue.Value.ToString();
            }

            textField.OnTextInput += onTextInput;
        }

        public static T GetNewAction<T>() where T : ActorAction, new()
        {
            T action = GetPoolAs<DefaultPool<T>, T>().Get();

            AddAction_OnRemoved_Return(action);

            return action;
        }

        public static void ShiftDrawer_ChildAdded(Drawer drawer, Widget child)
        {
            // Attempts to find spacing between added child and closest child that was after the added child (in terms of position).

            PreciseGroupLayoutAdapter drawerLayoutAdapter = drawer.Adapters.FindFirstOfType<PreciseGroupLayoutAdapter>();
            ReadOnlySpan<PreciseGroupLayoutAdapter.WidgetState> drawerLayoutStates = drawerLayoutAdapter.GetWidgetStates();

            Vec2f shiftDelta = Vec2f.Zero;
            float minDistance = float.MaxValue;
            bool neighborFound = false;

            Vec2f childPosition = child.Position;
            Vec2f childMax = childPosition + child.Size;

            for (int i = UI.Drawer.DRAWER_CONTENT_BEGIN_INDEX; i < drawerLayoutStates.Length; i++)
            {
                PreciseGroupLayoutAdapter.WidgetState state = drawerLayoutStates[i];

                if (state.widget == child || state.widget == drawer.CoverButton)
                {
                    continue;
                }

                Vec2f currentStatePosition = state.widget.Position;

                if (drawer.Direction == LayoutOrientation.Horizontal && GeoMath.GT_Precise(currentStatePosition.X, childPosition.X))
                {
                    float dist = currentStatePosition.X - childMax.X;

                    if (dist < minDistance)
                    {
                        minDistance = dist;

                        neighborFound = true;
                    }
                }
                else if (drawer.Direction == LayoutOrientation.Vertical && GeoMath.GT_Precise(currentStatePosition.Y, childPosition.Y))
                {
                    float dist = currentStatePosition.Y - childMax.Y;

                    if (dist < minDistance)
                    {
                        minDistance = dist;

                        neighborFound = true;
                    }
                }
            }

            if (neighborFound)
            {
                if (drawer.Direction == LayoutOrientation.Horizontal)
                {
                    shiftDelta.X = child.Size.X + minDistance;
                }
                else if (drawer.Direction == LayoutOrientation.Vertical)
                {
                    shiftDelta.Y = child.Size.Y + minDistance;
                }

                for (int i = UI.Drawer.DRAWER_CONTENT_BEGIN_INDEX; i < drawerLayoutStates.Length; i++)
                {
                    PreciseGroupLayoutAdapter.WidgetState state = drawerLayoutStates[i];

                    if (state.widget == child || state.widget == drawer.CoverButton)
                    {
                        continue;
                    }

                    Vec2f currentStatePosition = state.widget.Position;

                    bool needsShifted = (drawer.Direction == LayoutOrientation.Horizontal && GeoMath.GTE_Precise(currentStatePosition.X, childPosition.X)) ||
                                       (drawer.Direction == LayoutOrientation.Vertical && GeoMath.GTE_Precise(currentStatePosition.Y, childPosition.Y));

                    if (needsShifted)
                    {
                        drawerLayoutAdapter.TrySetNormalizedBounds(state.widget,
                                                                   PreciseGroupLayoutAdapter.GetNormalizedBounds(drawer, new AABB
                                                                   {
                                                                       Position = currentStatePosition + shiftDelta,
                                                                       Size = state.widget.Size
                                                                   }));
                    }
                }
            }
        }

        // TODO: Implement finding child closest to child, rather than first child.
        public static void ShiftDrawer_ChildRemoved(Drawer drawer, Widget child)
        {
            // TODO: May need adjusted for horizontal layouts.

            PreciseGroupLayoutAdapter drawerLayoutAdapter = drawer.Adapters.FindFirstOfType<PreciseGroupLayoutAdapter>();

            ReadOnlySpan<PreciseGroupLayoutAdapter.WidgetState> drawerLayoutStates = drawerLayoutAdapter.GetWidgetStates();

            float detectedSpacing = 0f;

            bool spacingFound = false;

            // Attempts to find spacing between removed child and first child that was after the removed child (in terms of position).

            for (int i = UI.Drawer.DRAWER_CONTENT_BEGIN_INDEX; i < drawerLayoutStates.Length; i++)
            {
                Widget otherChild = drawerLayoutStates[i].widget;

                if (otherChild == drawer.CoverButton || otherChild == child)
                {
                    continue;
                }

                if (drawer.Direction == LayoutOrientation.Horizontal && otherChild.Position.X >= child.Position.X)
                {
                    detectedSpacing = otherChild.Position.X - child.GetMaxX();

                    spacingFound = true;

                    break;
                }
                else if (drawer.Direction == LayoutOrientation.Vertical && otherChild.Position.Y >= child.Position.Y)
                {
                    detectedSpacing = otherChild.Position.Y - child.GetMaxY();

                    spacingFound = true;

                    break;
                }
            }

            if (spacingFound)
            {
                Vec2f shiftDelta = Vec2f.Zero;

                if (drawer.Direction == LayoutOrientation.Horizontal)
                {
                    shiftDelta.X = child.Size.X + detectedSpacing;
                }
                else if (drawer.Direction == LayoutOrientation.Vertical)
                {
                    shiftDelta.Y = child.Size.Y + detectedSpacing;
                }

                for (int i = UI.Drawer.DRAWER_CONTENT_BEGIN_INDEX; i < drawerLayoutStates.Length; i++)
                {
                    PreciseGroupLayoutAdapter.WidgetState state = drawerLayoutStates[i];

                    if (state.widget == drawer.CoverButton || state.widget == child)
                    {
                        continue;
                    }

                    bool isAfter = (drawer.Direction == LayoutOrientation.Horizontal && state.widget.Position.X > child.Position.X) ||
                                   (drawer.Direction == LayoutOrientation.Vertical && state.widget.Position.Y > child.Position.Y);

                    if (isAfter)
                    {
                        drawerLayoutAdapter.TrySetNormalizedBounds(state.widget,
                                                                   PreciseGroupLayoutAdapter.GetNormalizedBounds(drawer, new AABB
                                                                   {
                                                                       Position = state.widget.Position - shiftDelta,
                                                                       Size = state.widget.Size
                                                                   }));
                    }
                }
            }
        }

        private static void AddAction_OnRemoved_Return<T>(T action) where T : ActorAction
        {
            action.OnRemoved += delegate
            {
                Action_OnRemoved_Return<T>(action);
            };
        }

        private static void Action_OnRemoved_Return<T>(T action) where T : ActorAction
        {
            Pools.ReturnObject<T>(action);
        }

        private static T GetPoolAs<T, E>() where T : IPool<E>, new() where E : IPoolable
        {
            return Pools.GetPoolAs<T, E>();
        }
    }
}
