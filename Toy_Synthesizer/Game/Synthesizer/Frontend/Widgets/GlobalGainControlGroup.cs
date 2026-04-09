using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoGraphics.UI.Data;
using GeoLib.GeoGraphics.UI.Data.Generic;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.UI;
using Toy_Synthesizer.Game.DigitalSignalProcessing;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    public class GlobalGainControlGroup : GroupWidget
    {
        private DSP dsp;

        private Slider gainSlider;
        private TextField gainSliderDisplayTextField;
        private TextButton resetButton;

        private PropertyBindable<double> gainPropertyBindable;
        private ConvertingPropertyBinding<double, float> sliderBinding;
        private ConvertingPropertyBinding<double, string> textFieldBinding;

        public GlobalGainControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   style: uiManager.GetDefaultGroupStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            dsp = uiManager.Game.DSP;

            gainPropertyBindable = new PropertyBindable<double>("Global Gain", GeoMath.ScalarToPercent(dsp.GlobalGain));

            gainPropertyBindable.OnValueChangedTyped += SetGain;

            dsp.OnGlobalGainChanged += DSP_OnGlobalGainChanged;

            Style.RenderData.SetColor(uiManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            string uiXml = GetUIXml();

            UIXmlParser.Parse(uiManager, uiXml, rootParent: this);

            InitWidgets();
        }

        private void InitWidgets()
        {
            gainSlider = FindAsByNameDeepSearch<Slider>(GLOBAL_GAIN_SLIDER_NAME);
            gainSliderDisplayTextField = FindAsByNameDeepSearch<TextField>(GLOBAL_GAIN_SLIDER_DISPLAY_TEXTFIELD_NAME);
            resetButton = FindAsByNameDeepSearch<TextButton>(RESET_BUTTON_NAME);

            sliderBinding = gainSlider.BindPropertyConverting(gainPropertyBindable);

            textFieldBinding = gainSliderDisplayTextField.BindProperty_Number(gainPropertyBindable);

            resetButton.OnClick += ResetButton_OnClick;
        }

        private void DSP_OnGlobalGainChanged(double newValue)
        {
            gainPropertyBindable.Value = GeoMath.ScalarToPercent(newValue);
        }

        private void SetGain(double value)
        {
            dsp.GlobalGain = GeoMath.PercentToScalar(value);
        }

        private void ResetButton_OnClick()
        {
            gainPropertyBindable.Value = GeoMath.ScalarToPercent(DSP.DEFAULT_GLOBAL_GAIN);
        }

        private string GetUIXml()
        {
            NumberRange<double> globalGainPercentageRange = NumberRangeUtils.ScalarToPercent(DSP.GlobalGainRange);

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
                 NumberDefaultValue=""{GeoMath.ScalarToPercent(DSP.DEFAULT_GLOBAL_GAIN)}""
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

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            gainPropertyBindable = null;

            sliderBinding.Unbind();
            textFieldBinding.Unbind();

            sliderBinding = null;
            textFieldBinding = null;
        }

        private const string GLOBAL_GAIN_SLIDER_NAME = "GlobalGainSlider";
        private const string GLOBAL_GAIN_SLIDER_DISPLAY_TEXTFIELD_NAME = "GlobalGainDisplayTextField";
        private const string RESET_BUTTON_NAME = "ResetButton";
    }
}
