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
    public class MasterVolumeControlGroup : GroupWidget
    {
        private Backend.PolyphonicSynthesizer synthesizer;

        private Slider volumeSlider;
        private TextField volumeSliderDisplayTextField;
        private TextButton resetButton;

        private bool settingValueFromSlider = false;
        private bool settingValueFromDisplayTextField = false;
        private bool settingValue = false;

        public MasterVolumeControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
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

            synthesizer.OnMasterVolumeChanged += Synthesizer_OnMasterVolumeChanged;
        }

        private void InitWidgets()
        {
            volumeSlider = FindAsByNameDeepSearch<Slider>(VOLUME_SLIDER_NAME);
            volumeSliderDisplayTextField = FindAsByNameDeepSearch<TextField>(VOLUME_SLIDER_DISPLAY_TEXTFIELD_NAME);
            resetButton = FindAsByNameDeepSearch<TextButton>(RESET_BUTTON_NAME);

            volumeSlider.OnValueChange += Slider_OnValueChanged;
            volumeSliderDisplayTextField.OnTextInput += SliderDisplayTextField_OnTextInput;

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

            SetVolume(newValue, setSlider: false, setDisplay: true);
        }

        private void SliderDisplayTextField_OnTextInput(string text)
        {
            if (settingValueFromDisplayTextField)
            {
                settingValueFromDisplayTextField = false;

                return;
            }

            double newValue = GeoMath.ParseOrDefault<double>(text);

            SetVolume(newValue, setSlider: true, setDisplay: false);
        }

        private void Synthesizer_OnMasterVolumeChanged(double previousValue, double newValue)
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
            synthesizer.MasterVolume = PolyphonicSynthesizer.DEFAULT_MASTER_VOLUME;
        }

        private void SetVolume(double newValue_Percentage, bool setSlider, bool setDisplay)
        {
            if (settingValue)
            {
                return;
            }

            settingValue = true;

            double newValue_Scalar = GeoMath.PercentToScalar(newValue_Percentage);

            synthesizer.MasterVolume = newValue_Scalar;

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

            volumeSlider.CurrentValue = (float)GeoMath.ScalarToPercent(synthesizer.MasterVolume);
        }

        private void SetDisplayTextField()
        {
            volumeSliderDisplayTextField.Text = ((float)GeoMath.ScalarToPercent(synthesizer.MasterVolume)).ToString();
        }

        private string GetUIXml()
        {
            NumberRange<double> masterVolumePercentageRange = NumberRangeUtils.ScalarToPercent(PolyphonicSynthesizer.MasterVolumeRange);

            return
            $@"<Layout>

                <PlainLabel
                 Position=""(5%, 5%)""
                 Size=""(90%, 25%)""
                 Text=""Master Volume""
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
                 NumberMinValue=""{masterVolumePercentageRange.Min}""
                 NumberMaxValue=""{masterVolumePercentageRange.Max}""
                 NumberDefaultValue=""{PolyphonicSynthesizer.DEFAULT_MASTER_VOLUME}""
                 DragIncrement=""1.0""
                 Name=""{VOLUME_SLIDER_NAME}""/>

                <TextField
                 Position=""(75%, 55%)""
                 Size=""(20%, 40%)""
                 MaxCharacters=""6""
                 NumberMinValue=""{masterVolumePercentageRange.Min}""
                 NumberMaxValue=""{masterVolumePercentageRange.Max}""
                 NumberAllowedSign=""1""
                 Name=""{VOLUME_SLIDER_DISPLAY_TEXTFIELD_NAME}""/>

            </Layout>";
        }


        private const string VOLUME_SLIDER_NAME = "VolumeSlider";
        private const string VOLUME_SLIDER_DISPLAY_TEXTFIELD_NAME = "VolumeSliderDisplayTextField";
        private const string RESET_BUTTON_NAME = "ResetButton";
    }
}
