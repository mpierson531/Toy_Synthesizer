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
using Toy_Synthesizer.Game.Synthesizer.Frontend;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    public class GlobalGainControlGroup : GroupWidget
    {
        private Backend.PolyphonicSynthesizer synthesizer;

        private Slider gainSlider;
        private TextField gainSliderDisplayTextField;
        private TextButton resetButton;

        private bool settingValueFromSlider = false;
        private bool settingValueFromDisplayTextField = false;
        private bool settingValue = false;

        public GlobalGainControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   style: uiManager.GetDefaultGroupStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            this.synthesizer = uiManager.Game.Synthesizer.Backend.PolyphonicSynthesizer;

            Style.RenderData.SetColor(uiManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            string uiXml = GetUIXml();

            new UIXmlParser(uiManager).Parse(uiXml, rootParent: this);

            InitWidgets();

            synthesizer.OnGlobalGainChanged += Synthesizer_OnGlobalGainChanged;
        }

        private void InitWidgets()
        {
            gainSlider = FindAsByNameDeepSearch<Slider>(GLOBAL_GAIN_SLIDER_NAME);
            gainSliderDisplayTextField = FindAsByNameDeepSearch<TextField>(GLOBAL_GAIN_SLIDER_DISPLAY_TEXTFIELD_NAME);
            resetButton = FindAsByNameDeepSearch<TextButton>(RESET_BUTTON_NAME);

            gainSlider.OnValueChange += Slider_OnValueChanged;
            gainSliderDisplayTextField.OnTextInput += SliderDisplayTextField_OnTextInput;

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

            SetGlobalGain(newValue, setSlider: false, setDisplay: true);
        }

        private void SliderDisplayTextField_OnTextInput(string text)
        {
            if (settingValueFromDisplayTextField)
            {
                settingValueFromDisplayTextField = false;

                return;
            }

            double newValue = GeoMath.ParseOrDefault<double>(text);

            SetGlobalGain(newValue, setSlider: true, setDisplay: false);
        }

        private void Synthesizer_OnGlobalGainChanged(double previousValue, double newValue)
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
            // The slider and display textfield will be set through Synthesizer_OnGlobalGainChanged.

            synthesizer.GlobalGain = PolyphonicSynthesizer.DEFAULT_GLOBAL_GAIN;
        }

        private void SetGlobalGain(double newValue_Percentage, bool setSlider, bool setDisplay)
        {
            if (settingValue)
            {
                return;
            }

            settingValue = true;

            double newValue_Scalar = GeoMath.PercentToScalar(newValue_Percentage);

            synthesizer.GlobalGain = newValue_Scalar;

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

            gainSlider.CurrentValue = (float)GeoMath.ScalarToPercent(synthesizer.GlobalGain);
        }

        private void SetDisplayTextField()
        {
            gainSliderDisplayTextField.Text = ((float)GeoMath.ScalarToPercent(synthesizer.GlobalGain)).ToString();
        }

        private string GetUIXml()
        {
            NumberRange<double> globalGainPercentageRange = NumberRangeUtils.ScalarToPercent(PolyphonicSynthesizer.GlobalGainRange);

            return
            $@"<Layout>

                <PlainLabel
                 Position=""(5%, 5%)""
                 Size=""(90%, 25%)""
                 Text=""Global Gain""
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
                 NumberMinValue=""{globalGainPercentageRange.Min}""
                 NumberMaxValue=""{globalGainPercentageRange.Max}""
                 NumberDefaultValue=""{PolyphonicSynthesizer.DEFAULT_GLOBAL_GAIN}""
                 DragIncrement=""1.0""
                 Name=""{GLOBAL_GAIN_SLIDER_NAME}""/>

                <TextField
                 Position=""(75%, 55%)""
                 Size=""(20%, 40%)""
                 MaxCharacters=""6""
                 NumberMinValue=""{globalGainPercentageRange.Min}""
                 NumberMaxValue=""{globalGainPercentageRange.Max}""
                 NumberAllowedSign=""1""
                 Name=""{GLOBAL_GAIN_SLIDER_DISPLAY_TEXTFIELD_NAME}""/>

            </Layout>";
        }


        private const string GLOBAL_GAIN_SLIDER_NAME = "GlobalGainSlider";
        private const string GLOBAL_GAIN_SLIDER_DISPLAY_TEXTFIELD_NAME = "GlobalGainDisplayTextField";
        private const string RESET_BUTTON_NAME = "ResetButton";
    }
}
