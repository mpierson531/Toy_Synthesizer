using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Data;
using GeoLib.GeoGraphics.UI.Data.Generic;
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
        /// <summary>
        /// 
        /// This is used to round end results.
        /// 
        /// <br></br>
        /// <br></br>
        /// 
        /// Set <c>-1</c> to disable.
        /// 
        /// </summary>
        public static int RoundingPlaces = 8;

        private Slider slider;
        private TextField sliderDisplayTextField;
        private TextButton resetButton;

        private readonly NumberRange<double> range;
        private readonly double defaultValue;
        private readonly bool treatAsScalarPercentage;

        private readonly string propertyName;

        private bool settingValue = false;
        private bool settingValueFromSlider = false;
        private bool settingValueFromTextField = false;

        private IPropertyBindable propertyBindable;
        private ConvertingPropertyBinding<double, float> sliderBinding;
        private ConvertingPropertyBinding<double, string> textFieldBinding;

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

            this.range = range;

            this.defaultValue = defaultValue;

            this.treatAsScalarPercentage = treatAsScalarPercentage;

            this.propertyName = propertyName;

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

        public void BindProperty(PropertyBindable<double> bindable)
        {
            this.propertyBindable = bindable;

            sliderBinding?.Dispose();
            textFieldBinding?.Dispose();

            sliderBinding = new ConvertingPropertyBinding<double, float>(bindable, SliderBinding_GetTarget, SliderBinding_SetTarget, SliderBinding_SourceToTarget, SliderBinding_TargetToSource);
            textFieldBinding = new ConvertingPropertyBinding<double, string>(bindable, TextFieldBinding_GetTarget, TextFieldBinding_SetTarget, TextFieldBinding_SourceToTarget, TextFieldBinding_TargetToSource);

            slider.BindProperty(sliderBinding);
            sliderDisplayTextField.BindProperty(textFieldBinding);
        }

        private float SliderBinding_GetTarget()
        {
            return slider.CurrentValue;
        }

        private void SliderBinding_SetTarget(float value)
        {
            slider.SetValueWithoutProperty((float)value);
        }

        private float SliderBinding_SourceToTarget(double value)
        {
            return (float)GetProcessedValueForWidgets(value);
        }

        private double SliderBinding_TargetToSource(float value)
        {
            return GetProcessedValueForExternal(value);
        }

        private string TextFieldBinding_GetTarget()
        {
            return sliderDisplayTextField.Text;
        }

        private void TextFieldBinding_SetTarget(string value)
        {
            sliderDisplayTextField.Text = value;
        }

        private string TextFieldBinding_SourceToTarget(double value)
        {
            return GetProcessedValueForWidgets(value).ToString();
        }

        private double TextFieldBinding_TargetToSource(string value)
        {
            return GetProcessedValueForExternal(GeoMath.ParseOrDefault<double>(value));
        }

        private void InitWidgets()
        {
            slider = FindAsByNameDeepSearch<Slider>(SLIDER_NAME);
            sliderDisplayTextField = FindAsByNameDeepSearch<TextField>(SLIDER_DISPLAY_TEXTFIELD_NAME);
            resetButton = FindAsByNameDeepSearch<TextButton>(RESET_BUTTON_NAME);

            slider.OnValueChange += Slider_OnValueChanged;
            sliderDisplayTextField.OnTextInput += SliderDisplayTextField_OnTextInput;

            SetWidgetValues(defaultValue, updateProperty: true);

            resetButton.OnClick += ResetButton_OnClick;
        }

        public void SetWidgetValues(double value, bool updateProperty)
        {
            value = GetProcessedValueForWidgets(value);

            SetWidgetValuesInternal(value, updateProperty);
        }

        private void SetWidgetValuesInternal(double value, bool updateProperty)
        {
            SetSlider(value, updateProperty: true);
            SetDisplayTextField(value, updateProperty: true);
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
            if (settingValueFromTextField)
            {
                settingValueFromTextField = false;

                return;
            }

            double newValue = GeoMath.ParseOrDefault<double>(text);

            WidgetValueChanged(newValue, setSlider: true, setDisplay: false);
        }

        private void ResetButton_OnClick()
        {
            SetWidgetValues(defaultValue, updateProperty: true);

            OnWidgetValueChanged?.Invoke(defaultValue);
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
                SetSlider(newValue, updateProperty: true);
            }

            if (setDisplay)
            {
                SetDisplayTextField(newValue, updateProperty: true);
            }

            OnWidgetValueChanged?.Invoke(externalValue);

            settingValue = false;
        }

        private void SetSlider(double value, bool updateProperty)
        {
            settingValueFromSlider = true;

            if (!updateProperty)
            {
                slider.SetValueWithoutProperty((float)value);
            }
            else
            {
                slider.CurrentValue = (float)value;
            }
        }

        private void SetDisplayTextField(double value, bool updateProperty)
        {
            string text = ((float)value).ToString();

            if (!updateProperty)
            {
                sliderDisplayTextField.SetTextWithoutProperty(text);
            }
            else
            {
                sliderDisplayTextField.Text = text;
            }
        }

        private double GetProcessedValueForWidgets(double value)
        {
            if (treatAsScalarPercentage)
            {
                value = GeoMath.ScalarToPercent(value);
            }

            return CheckIfNeedsRounding(value);
        }

        private double GetProcessedValueForExternal(double value)
        {
            if (treatAsScalarPercentage)
            {
                value = GeoMath.PercentToScalar(value);
            }

            return CheckIfNeedsRounding(value);
        }

        private double CheckIfNeedsRounding(double value)
        {
            if (RoundingPlaces < 0)
            {
                return value;
            }

            return GeoMath.RoundAwayFromZero(value, RoundingPlaces);
        }

        private string GetUIXml(Vec2f? labelPosition = null,
                                Vec2f? sliderPosition = null,
                                Vec2f? sliderSize = null,
                                Vec2f? textFieldPosition = null,
                                Vec2f? textFieldSize = null,
                                Vec2f? resetButtonPosition = null,
                                Vec2f? resetButtonSize = null)
        {
            bool hasPropertyName = !TextUtils.IsNullEmptyOrWhitespace(propertyName);

            NumberRange<double> processedRange = treatAsScalarPercentage ? NumberRangeUtils.ScalarToPercent(range) : range;

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

            double processedDefaultValue = GetProcessedValueForWidgets(defaultValue);

            if (propertyName is null)
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
                 Text=""{propertyName}""
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

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            propertyBindable = null;
        }

        private const string SLIDER_NAME = "Slider";
        private const string SLIDER_DISPLAY_TEXTFIELD_NAME = "DisplayTextField";
        private const string RESET_BUTTON_NAME = "ResetButton";
    }
}
