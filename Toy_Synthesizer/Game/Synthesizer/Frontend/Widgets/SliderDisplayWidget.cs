using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    // TODO: Add options/support for not having label and slightly different layouts.
    public class SliderDisplayWidget : GroupWidget
    {
        private Slider slider;
        private TextField sliderDisplayTextField;
        private TextButton resetButton;

        private bool settingValueFromSlider = false;
        private bool settingValueFromDisplayTextField = false;
        private bool settingValue = false;

        public NumberRange<double> Range { get; set; }
        public double DefaultValue { get; set; }
        public bool TreatAsScalarPercentage { get; set; }

        public string PropertyName { get; }

        public float DragIncrement
        {
            get => slider.DragIncrement;
            set => slider.DragIncrement = value;
        }

        public event Action<double> OnWidgetValueChanged;

        // label and resetButton layout values should be percentages.
        public SliderDisplayWidget(Vec2f position, Vec2f size, GroupStyle style, UIManager uiManager,
                                   NumberRange<double> range, double defaultValue,
                                   bool treatAsScalarPercentage = false,
                                   string propertyName = null,
                                   Vec2f? labelPosition = null,
                                   Vec2f? sliderPosition = null,
                                   Vec2f? sliderSize = null,
                                   Vec2f? textFieldPosition = null,
                                   Vec2f? textFieldSize = null,
                                   Vec2f? resetButtonPosition = null,
                                   Vec2f? resetButtonSize = null)
            : base(position, size,
                   style: style,
                   positionChildren: false,
                   sizeChildren: false)
        {
            Adapters.Add(new PreciseGroupLayoutAdapter());

            Range = range;

            DefaultValue = defaultValue;

            TreatAsScalarPercentage = treatAsScalarPercentage;

            PropertyName = propertyName;

            string uiXml = GetUIXml(labelPosition: labelPosition,
                                    sliderPosition: sliderPosition,
                                    sliderSize: sliderSize,
                                    textFieldPosition: textFieldPosition,
                                    textFieldSize: textFieldSize,
                                    resetButtonPosition: resetButtonPosition, 
                                    resetButtonSize:  resetButtonSize);

            new UIXmlParser(uiManager).Parse(uiXml, rootParent: this);

            InitWidgets();
        }

        private void InitWidgets()
        {
            slider = FindAsByNameDeepSearch<Slider>(SLIDER_NAME);
            sliderDisplayTextField = FindAsByNameDeepSearch<TextField>(SLIDER_DISPLAY_TEXTFIELD_NAME);
            resetButton = FindAsByNameDeepSearch<TextButton>(RESET_BUTTON_NAME);

            slider.OnValueChange += Slider_OnValueChanged;
            sliderDisplayTextField.OnTextInput += SliderDisplayTextField_OnTextInput;

            SetWidgetValues(DefaultValue);

            resetButton.OnClick += ResetButton_OnClick;
        }

        public void SetWidgetValues(double value)
        {
            value = GetProcessedValueForWidgets(value);

            SetWidgetValuesInternal(value);
        }

        private void SetWidgetValuesInternal(double value)
        {
            SetSlider(value);
            SetDisplayTextField(value);
        }

        private void Slider_OnValueChanged(Slider slide, float previousValue, float newValue)
        {
            if (settingValueFromSlider)
            {
                settingValueFromSlider = false;

                return;
            }

            WidgetValueChanged(newValue, setSlider: false, setDisplay: true);
        }

        private void SliderDisplayTextField_OnTextInput(string text)
        {
            if (settingValueFromDisplayTextField)
            {
                settingValueFromDisplayTextField = false;

                return;
            }

            double newValue = GeoMath.ParseOrDefault<double>(text);

            WidgetValueChanged(newValue, setSlider: true, setDisplay: false);
        }

        private void ResetButton_OnClick()
        {
            SetWidgetValues(DefaultValue);

            OnWidgetValueChanged?.Invoke(DefaultValue);
        }

        private void WidgetValueChanged(double newValue, bool setSlider, bool setDisplay)
        {
            if (settingValue)
            {
                return;
            }

            settingValue = true;

            double externalValue = GetProcessedValueForExternal(newValue);

            if (setSlider)
            {
                SetSlider(newValue);
            }

            if (setDisplay)
            {
                SetDisplayTextField(newValue);
            }

            OnWidgetValueChanged?.Invoke(externalValue);

            settingValue = false;
        }

        private void SetSlider(double value)
        {
            settingValueFromSlider = true;

            slider.CurrentValue = (float)value;
        }

        private void SetDisplayTextField(double value)
        {
            sliderDisplayTextField.Text = ((float)value).ToString();
        }

        private double GetProcessedValueForWidgets(double value)
        {
            if (TreatAsScalarPercentage)
            {
                return GeoMath.ScalarToPercent(value);
            }

            return value;
        }

        private double GetProcessedValueForExternal(double value)
        {
            if (TreatAsScalarPercentage)
            {
                return GeoMath.PercentToScalar(value);
            }

            return value;
        }

        private string GetUIXml(Vec2f? labelPosition = null,
                                Vec2f? sliderPosition = null,
                                Vec2f? sliderSize = null,
                                Vec2f? textFieldPosition = null,
                                Vec2f? textFieldSize = null,
                                Vec2f? resetButtonPosition = null,
                                Vec2f? resetButtonSize = null)
        {
            bool hasPropertyName = !TextUtils.IsNullEmptyOrWhitespace(PropertyName);

            NumberRange<double> processedRange = TreatAsScalarPercentage ? NumberRangeUtils.ScalarToPercent(Range) : Range;

            if (!labelPosition.HasValue)
            {
                labelPosition = new Vec2f(0f, 25f);
            }

            if (!sliderPosition.HasValue)
            {
                sliderPosition = !hasPropertyName ? new Vec2f(26.25f, 0f) : new Vec2f(32.5f, 0f);
            }

            if (!sliderSize.HasValue)
            {
                sliderSize = !hasPropertyName ? new Vec2f(48.75f, 100f) : new Vec2f(42.5f, 100f);
            }

            if (!textFieldPosition.HasValue)
            {
                textFieldPosition = new Vec2f(77.5f, 0f);
            }

            if (!textFieldSize.HasValue)
            {
                textFieldSize = new Vec2f(17.5f, 100f);
            }

            if (!resetButtonPosition.HasValue)
            {
                resetButtonPosition = !hasPropertyName ? new Vec2f(0f, 12.5f) : new Vec2f(12.5f, 12.5f);
            }

            if (!resetButtonSize.HasValue)
            {
                resetButtonSize = new Vec2f(17.5f, 75f);
            }

            /*if (!labelPosition.HasValue)
            {
                labelPosition = new Vec2f(0f, 0.25f);
            }

            if (!sliderPosition.HasValue)
            {
                sliderPosition = !hasPropertyName ? new Vec2f(0.2625f, 0f) : new Vec2f(0.325f, 0f);
            }

            if (!sliderSize.HasValue)
            {
                sliderSize = !hasPropertyName ? new Vec2f(0.4875f, 1f) : new Vec2f(0.425f, 1f);
            }

            if (!textFieldPosition.HasValue)
            {
                textFieldPosition = new Vec2f(0.775f, 0f);
            }

            if (!textFieldSize.HasValue)
            {
                textFieldSize = new Vec2f(0.175f, 1f);
            }

            if (!resetButtonPosition.HasValue)
            {
                resetButtonPosition = !hasPropertyName ? new Vec2f(0f, 0.125f) : new Vec2f(0.125f, 0.125f);
            }

            if (!resetButtonSize.HasValue)
            {
                resetButtonSize = new Vec2f(0.175f, 0.75f);
            }

            labelPosition *= 100f;
            sliderPosition *= 100f;
            sliderSize *= 100f;
            textFieldPosition *= 100f;
            textFieldSize *= 100f;
            resetButtonPosition *= 100f;
            resetButtonSize *= 100f;*/

            double processedDefaultValue = GetProcessedValueForWidgets(DefaultValue);

            if (PropertyName is null)
            {
                return
            $@"<Layout>

                <TextButton
                 Position=""({resetButtonPosition.Value.X}%, {resetButtonPosition.Value.Y}%)""
                 Size=""({resetButtonSize.Value.X}%, {resetButtonSize.Value.Y}%)""
                 Text=""Reset""
                 Alignment=""Center""
                 Name=""{RESET_BUTTON_NAME}""/>
                 
                <Slider
                 Position=""({sliderPosition.Value.X}%, {sliderPosition.Value.Y}%)""
                 Size=""({sliderSize.Value.X}%, {sliderSize.Value.Y}%)""
                 NumberMinValue=""{processedRange.Min}""
                 NumberMaxValue=""{processedRange.Max}""
                 NumberDefaultValue=""{processedDefaultValue}""
                 DragIncrement=""1.0""
                 Name=""{SLIDER_NAME}""/>

                <TextField
                 Position=""({textFieldPosition.Value.X}%, {textFieldPosition.Value.Y}%)""
                 Size=""({textFieldSize.Value.X}%, {textFieldSize.Value.Y}%)""
                 MaxCharacters=""6""
                 NumberMinValue=""{processedRange.Min}""
                 NumberMaxValue=""{processedRange.Max}""
                 NumberAllowedSign=""1""
                 Name=""{SLIDER_DISPLAY_TEXTFIELD_NAME}""/>

            </Layout>";
            }

            return
            $@"<Layout>

                <PlainLabel
                 Position=""({labelPosition.Value.X}%, {labelPosition.Value.Y}%)""
                 Size=""(20%, 100%)""
                 Text=""{PropertyName}""
                 FitText=""false""
                 GrowWithText=""true""/>

                <TextButton
                 Position=""({resetButtonPosition.Value.X}%, {resetButtonPosition.Value.Y}%)""
                 Size=""({resetButtonSize.Value.X}%, {resetButtonSize.Value.Y}%)""
                 Text=""Reset""
                 Alignment=""Center""
                 Name=""{RESET_BUTTON_NAME}""/>
                 
                <Slider
                 Position=""({sliderPosition.Value.X}%, {sliderPosition.Value.Y}%)""
                 Size=""({sliderSize.Value.X}%, {sliderSize.Value.Y}%)""
                 NumberMinValue=""{processedRange.Min}""
                 NumberMaxValue=""{processedRange.Max}""
                 NumberDefaultValue=""{processedDefaultValue}""
                 DragIncrement=""1.0""
                 Name=""{SLIDER_NAME}""/>

                <TextField
                 Position=""({textFieldPosition.Value.X}%, {textFieldPosition.Value.Y}%)""
                 Size=""({textFieldSize.Value.X}%, {textFieldSize.Value.Y}%)""
                 MaxCharacters=""6""
                 NumberMinValue=""{processedRange.Min}""
                 NumberMaxValue=""{processedRange.Max}""
                 NumberAllowedSign=""1""
                 Name=""{SLIDER_DISPLAY_TEXTFIELD_NAME}""/>

            </Layout>";
        }

        private const string SLIDER_NAME = "Slider";
        private const string SLIDER_DISPLAY_TEXTFIELD_NAME = "DisplayTextField";
        private const string RESET_BUTTON_NAME = "ResetButton";
    }
}
