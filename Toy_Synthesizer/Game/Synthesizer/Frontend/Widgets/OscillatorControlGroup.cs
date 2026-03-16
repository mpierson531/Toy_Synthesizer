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

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    // TODO: Implement usage of range in PolyphonicSynthesizer and implement command usage.
    public class OscillatorControlGroup : GroupWidget
    {
        private VoiceGroup parentVoiceGroup;

        private Slider mixSlider;
        private TextField mixSliderDisplayTextField;
        private TextButton resetButton;

        private bool settingValueFromSlider = false;
        private bool settingValueFromDisplayTextField = false;
        private bool settingValue = false;

        public OscillatorControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   style: uiManager.GetDefaultGroupStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            Style.RenderData.SetColor(uiManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            string uiXml = GetUIXml();

            new UIXmlParser(uiManager).Parse(uiXml, rootParent: this);

            InitWidgets();
        }

        protected override void ParentChanged(GroupWidget previousParent, GroupWidget newParent)
        {
            base.ParentChanged(previousParent, newParent);

            if (parentVoiceGroup is not null)
            {
                parentVoiceGroup.OnVoiceChanged -= ParentVoiceGroup_OnVoiceChanged;
            }

            Utils.Assert(newParent is VoiceGroup || newParent is null, "A VoiceMixControlGroup's parent must be a VoiceGroup.");

            parentVoiceGroup = (VoiceGroup)newParent;

            if (parentVoiceGroup is not null)
            {
                parentVoiceGroup.OnVoiceChanged += ParentVoiceGroup_OnVoiceChanged;
            }

            SetSlider();
            SetDisplayTextField();
        }

        private void InitWidgets()
        {
            mixSlider = FindAsByNameDeepSearch<Slider>(MIX_SLIDER_NAME);
            mixSliderDisplayTextField = FindAsByNameDeepSearch<TextField>(MIX_SLIDER_DISPLAY_TEXTFIELD_NAME);
            resetButton = FindAsByNameDeepSearch<TextButton>(RESET_BUTTON_NAME);

            mixSlider.OnValueChange += Slider_OnValueChanged;
            mixSliderDisplayTextField.OnTextInput += SliderDisplayTextField_OnTextInput;

            SetSlider();
            SetDisplayTextField();

            resetButton.OnClick += ResetButton_OnClick;
        }

        private void ParentVoiceGroup_OnVoiceChanged(VoiceGroup _, Voice previousVoice, Voice newVoice)
        {
            SetSlider();
            SetDisplayTextField();
        }

        private void Slider_OnValueChanged(Slider slide, float previousValue, float newValue)
        {
            if (settingValueFromSlider)
            {
                settingValueFromSlider = false;

                return;
            }

            SetMix(newValue, setSlider: false, setDisplay: true);
        }

        private void SliderDisplayTextField_OnTextInput(string text)
        {
            if (settingValueFromDisplayTextField)
            {
                settingValueFromDisplayTextField = false;

                return;
            }

            double newValue = GeoMath.ParseOrDefault<double>(text);

            SetMix(newValue, setSlider: true, setDisplay: false);
        }

        private void ResetButton_OnClick()
        {
            SetMix(GeoMath.ScalarToPercent(PolyphonicSynthesizer.DEFAULT_MIX), setSlider: true, setDisplay: true);
        }

        private void SetMix(double newValue_Percentage, bool setSlider, bool setDisplay)
        {
            if (settingValue)
            {
                return;
            }

            settingValue = true;

            double newValue_Scalar = GeoMath.PercentToScalar(newValue_Percentage);

            SetVoiceMixRaw(newValue_Scalar);

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

            mixSlider.CurrentValue = (float)GeoMath.ScalarToPercent(GetCurrentVoiceMix());
        }

        private void SetDisplayTextField()
        {
            mixSliderDisplayTextField.Text = ((float)GeoMath.ScalarToPercent(GetCurrentVoiceMix())).ToString();
        }

        private double GetCurrentVoiceMix()
        {
            Voice voice = GetCurrentVoice();

            if (voice is null)
            {
                return 0.0;
            }

            return voice.Mix;
        }

        private void SetVoiceMixRaw(double value)
        {
            Voice voice = GetCurrentVoice();

            if (voice is null)
            {
                return;
            }

            voice.Mix = value;
        }

        private Voice GetCurrentVoice()
        {
            return parentVoiceGroup?.Voice;
        }

        private string GetUIXml()
        {
            NumberRange<double> mixPercentageRange = NumberRangeUtils.ScalarToPercent(PolyphonicSynthesizer.MixRange);

            return
            $@"<Layout>

                <PlainLabel
                 Position=""(0%, 25%)""
                 Size=""(20%, 100%)""
                 Text=""Mix""
                 FitText=""false""
                 GrowWithText=""true""/>

                <TextButton
                 Position=""(12.5%, 12.5%)""
                 Size=""(17.5%, 75%)""
                 Text=""Reset""
                 Alignment=""Center""
                 Name=""{RESET_BUTTON_NAME}""/>
                 
                <Slider
                 Position=""(32.5%, 0%)""
                 Size=""(42.5%, 100%)""
                 NumberMinValue=""{mixPercentageRange.Min}""
                 NumberMaxValue=""{mixPercentageRange.Max}""
                 NumberDefaultValue=""{PolyphonicSynthesizer.DEFAULT_MIX}""
                 DragIncrement=""1.0""
                 Name=""{MIX_SLIDER_NAME}""/>

                <TextField
                 Position=""(77.5%, 0%)""
                 Size=""(17.5%, 100%)""
                 MaxCharacters=""6""
                 NumberMinValue=""{mixPercentageRange.Min}""
                 NumberMaxValue=""{mixPercentageRange.Max}""
                 NumberAllowedSign=""1""
                 Name=""{MIX_SLIDER_DISPLAY_TEXTFIELD_NAME}""/>

            </Layout>";
        }

        private const string MIX_SLIDER_NAME = "MixSlider";
        private const string MIX_SLIDER_DISPLAY_TEXTFIELD_NAME = "MixDisplayTextField";
        private const string RESET_BUTTON_NAME = "ResetButton";
    }
}
