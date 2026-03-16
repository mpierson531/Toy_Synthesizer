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

using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.UI;
using Toy_Synthesizer.Game.DigitalSignalProcessing;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    public class GlobalPanControlGroup : GroupWidget
    {
        private DSP dsp;

        private Slider panSlider;
        private TextField panSliderDisplayTextField;
        private TextButton resetButton;

        private bool settingValueFromSlider = false;
        private bool settingValueFromDisplayTextField = false;
        private bool settingValue = false;

        public GlobalPanControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   style: uiManager.GetDefaultGroupStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            dsp = uiManager.Game.DSP;

            Style.RenderData.SetColor(uiManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            string uiXml = GetUIXml();

            new UIXmlParser(uiManager).Parse(uiXml, rootParent: this);

            InitWidgets();

            dsp.OnGlobalPanChanged += Synthesizer_OnGlobalPanChanged;
        }

        private void InitWidgets()
        {
            panSlider = FindAsByNameDeepSearch<Slider>(PAN_SLIDER_NAME);
            panSliderDisplayTextField = FindAsByNameDeepSearch<TextField>(PAN_SLIDER_DISPLAY_TEXTFIELD_NAME);
            resetButton = FindAsByNameDeepSearch<TextButton>(RESET_BUTTON_NAME);

            panSlider.OnValueChange += Slider_OnValueChanged;
            panSliderDisplayTextField.OnTextInput += SliderDisplayTextField_OnTextInput;

            SetSlider();
            SetDisplayTextField();

            resetButton.OnClick += ResetButton_OnClick;
        }

        private void Slider_OnValueChanged(Slider slide, float previousValue, float newValue)
        {
            if (settingValueFromSlider)
            {
                settingValueFromSlider = false;

                return;
            }

            SetPan(newValue, setSlider: false, setDisplay: true);
        }

        private void SliderDisplayTextField_OnTextInput(string text)
        {
            if (settingValueFromDisplayTextField)
            {
                settingValueFromDisplayTextField = false;

                return;
            }

            double newValue = GeoMath.ParseOrDefault<double>(text);

            SetPan(newValue, setSlider: true, setDisplay: false);
        }

        private void Synthesizer_OnGlobalPanChanged(double previousValue, double newValue)
        {
            if (settingValue)
            {
                return;
            }

            SetSlider();
            SetDisplayTextField();
        }

        private void ResetButton_OnClick()
        {
            dsp.GlobalPan = DSP.DEFAULT_GLOBAL_PAN;
        }

        private void SetPan(double newValue_Percentage, bool setSlider, bool setDisplay)
        {
            if (settingValue)
            {
                return;
            }

            settingValue = true;

            double newValue_Scalar = GeoMath.PercentToScalar(newValue_Percentage);

            dsp.GlobalPan = newValue_Scalar;

            if (setSlider)
            {
                SetSlider();
            }

            if (setDisplay)
            {
                SetDisplayTextField();
            }

            settingValue = false;
        }

        private void SetSlider()
        {
            settingValueFromSlider = true;

            panSlider.CurrentValue = (float)GeoMath.ScalarToPercent(dsp.GlobalPan);
        }

        private void SetDisplayTextField()
        {
            panSliderDisplayTextField.Text = ((float)GeoMath.ScalarToPercent(dsp.GlobalPan)).ToString();
        }

        private string GetUIXml()
        {
            NumberRange<double> globalPanPercentageRange = NumberRangeUtils.ScalarToPercent(DSP.GlobalPanRange);

            return
            $@"<Layout>

                <PlainLabel
                 Position=""(5%, 5%)""
                 Size=""(90%, 25%)""
                 Text=""Global Pan""
                 FitText=""false""
                 GrowWithText=""true""/>

                <TextButton
                 Position=""(70%, 5%)""
                 Size=""(25%, 25%)""
                 Text=""Reset""
                 Alignment=""Center""
                 Name=""{RESET_BUTTON_NAME}""/>
                 
                <Slider
                 Position=""(5%, 55%)""
                 Size=""(65%, 40%)""
                 NumberMinValue=""{globalPanPercentageRange.Min}""
                 NumberMaxValue=""{globalPanPercentageRange.Max}""
                 NumberDefaultValue=""{DSP.DEFAULT_GLOBAL_PAN}""
                 DragIncrement=""1.0""
                 Name=""{PAN_SLIDER_NAME}""/>

                <TextField
                 Position=""(75%, 55%)""
                 Size=""(20%, 40%)""
                 MaxCharacters=""6""
                 NumberMinValue=""{globalPanPercentageRange.Min}""
                 NumberMaxValue=""{globalPanPercentageRange.Max}""
                 NumberAllowedSign=""1""
                 Name=""{PAN_SLIDER_DISPLAY_TEXTFIELD_NAME}""/>

            </Layout>";
        }


        private const string PAN_SLIDER_NAME = "PanSlider";
        private const string PAN_SLIDER_DISPLAY_TEXTFIELD_NAME = "PanSliderDisplayTextField";
        private const string RESET_BUTTON_NAME = "ResetButton";
    }
}
