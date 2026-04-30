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
    public class GlobalPanControlGroup : GroupWidget
    {
        private DSP dsp;

        private Slider panSlider;
        private TextField panSliderDisplayTextField;
        private TextButton resetButton;

        private PropertyBindable<double> panPropertyBindable;
        private ConvertingPropertyBinding<double, float> sliderBinding;
        private ConvertingPropertyBinding<double, string> textFieldBinding;

        public GlobalPanControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   style: uiManager.GetDefaultGroupStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            dsp = uiManager.Game.DSP;

            dsp.OnGlobalPanChanged += DSP_OnGlobalPanChanged;

            panPropertyBindable = new PropertyBindable<double>("Global Pan", GeoMath.ScalarToPercent(dsp.GlobalPan));

            panPropertyBindable.OnValueChangedTyped += SetPan;

            Style.RenderData.SetColor(uiManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            string uiXml = GetUIXml();

            new UIXmlParser(uiManager.Game).Parse(uiXml, rootParent: this);

            InitWidgets();
        }

        private void InitWidgets()
        {
            panSlider = FindAsByNameDeepSearch<Slider>(PAN_SLIDER_NAME);
            panSliderDisplayTextField = FindAsByNameDeepSearch<TextField>(PAN_SLIDER_DISPLAY_TEXTFIELD_NAME);
            resetButton = FindAsByNameDeepSearch<TextButton>(RESET_BUTTON_NAME);

            sliderBinding = panSlider.BindPropertyConverting(panPropertyBindable);

            textFieldBinding = panSliderDisplayTextField.BindProperty_Number(panPropertyBindable);

            resetButton.OnClick += ResetButton_OnClick;
        }

        private void DSP_OnGlobalPanChanged(double newValue)
        {
            panPropertyBindable.Value = newValue;
        }

        private void ResetButton_OnClick()
        {
            panPropertyBindable.Value = GeoMath.ScalarToPercent(DSP.DEFAULT_GLOBAL_PAN);
        }

        private void SetPan(double newValue)
        {
            dsp.GlobalPan = GeoMath.PercentToScalar(newValue);
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
                 NumberDefaultValue=""{GeoMath.ScalarToPercent(DSP.DEFAULT_GLOBAL_PAN)}""
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

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            panPropertyBindable = null;

            sliderBinding.Unbind();
            textFieldBinding.Unbind();

            sliderBinding = null;
            textFieldBinding = null;
        }


        private const string PAN_SLIDER_NAME = "PanSlider";
        private const string PAN_SLIDER_DISPLAY_TEXTFIELD_NAME = "PanSliderDisplayTextField";
        private const string RESET_BUTTON_NAME = "ResetButton";
    }
}
