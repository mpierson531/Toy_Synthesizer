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

using Toy_Synthesizer.Game.DigitalSignalProcessing;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    public class MasterVolumeControlGroup : GroupWidget
    {
        private DSP dsp;

        private Slider volumeSlider;
        private TextField volumeSliderDisplayTextField;
        private TextButton resetButton;

        private PropertyBindable<double> volumePropertyBindable;
        private ConvertingPropertyBinding<double, float> sliderBinding;
        private ConvertingPropertyBinding<double, string> textFieldBinding;

        public MasterVolumeControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   style: uiManager.GetDefaultGroupStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            dsp = uiManager.Game.DSP;

            dsp.OnMasterVolumeChanged += DSP_OnMasterVolumeChanged;

            volumePropertyBindable = new PropertyBindable<double>("Master Volume", GeoMath.ScalarToPercent(dsp.MasterVolume));

            volumePropertyBindable.OnValueChangedTyped += SetVolume;

            Style.RenderData.SetColor(uiManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            string uiXml = GetUIXml();

            new UIXmlParser(uiManager.Game).Parse(uiXml, rootParent: this);

            InitWidgets();
        }

        private void DSP_OnMasterVolumeChanged(double newValue)
        {
            volumePropertyBindable.Value = GeoMath.ScalarToPercent(newValue);
        }

        private void InitWidgets()
        {
            volumeSlider = FindAsByNameDeepSearch<Slider>(VOLUME_SLIDER_NAME);
            volumeSliderDisplayTextField = FindAsByNameDeepSearch<TextField>(VOLUME_SLIDER_DISPLAY_TEXTFIELD_NAME);
            resetButton = FindAsByNameDeepSearch<TextButton>(RESET_BUTTON_NAME);

            sliderBinding = volumeSlider.BindPropertyConverting(volumePropertyBindable);

            textFieldBinding = volumeSliderDisplayTextField.BindProperty_Number(volumePropertyBindable);

            resetButton.OnClick += ResetButton_OnClick;
        }

        private void ResetButton_OnClick()
        {
            volumePropertyBindable.Value = GeoMath.ScalarToPercent(DSP.DEFAULT_MASTER_VOLUME);
        }

        private void SetVolume(double newValue)
        {
            dsp.MasterVolume = GeoMath.PercentToScalar(newValue);
        }

        private string GetUIXml()
        {
            NumberRange<double> masterVolumePercentageRange = NumberRangeUtils.ScalarToPercent(DSP.MasterVolumeRange);

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
                 NumberDefaultValue=""{GeoMath.ScalarToPercent(DSP.DEFAULT_MASTER_VOLUME)}""
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

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            volumePropertyBindable = null;

            sliderBinding.Unbind();
            textFieldBinding.Unbind();

            sliderBinding = null;
            textFieldBinding = null;
        }


        private const string VOLUME_SLIDER_NAME = "VolumeSlider";
        private const string VOLUME_SLIDER_DISPLAY_TEXTFIELD_NAME = "VolumeSliderDisplayTextField";
        private const string RESET_BUTTON_NAME = "ResetButton";
    }
}
