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
    public class GlobalVoiceSemitonePitchShiftControlGroup : GroupWidget
    {
        private Frontend frontend;

        private SliderDisplayWidget shiftKeySlider;
        private SliderDisplayWidget controlKeySlider;

        private bool settingShiftAmount;
        private bool settingControlAmount;

        public GlobalVoiceSemitonePitchShiftControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   style: uiManager.GetDefaultGroupStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            frontend = uiManager.Game.SynthesizerFrontend;

            settingShiftAmount = false;
            settingControlAmount = false;

            Style.RenderData.SetColor(uiManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            UIXmlParser uiXmlParser = new UIXmlParser(uiManager);
            uiXmlParser.AddTypeFactory(new Frontend.SliderDisplayWidgetFactory());

            string uiXml = GetUIXml();

            uiXmlParser.Parse(uiXml, rootParent: this);

            InitWidgets();
        }

        private void InitWidgets()
        {
            shiftKeySlider = FindAsByNameDeepSearch<SliderDisplayWidget>(SHIFT_SLIDER_NAME);
            controlKeySlider = FindAsByNameDeepSearch<SliderDisplayWidget>(CONTROL_SLIDER_NAME);

            shiftKeySlider.OnWidgetValueChanged += ShiftSlider_OnValueChanged;
            controlKeySlider.OnWidgetValueChanged += ControlSlider_OnValueChanged;
        }

        private void ShiftSlider_OnValueChanged(double newValue)
        {
            SetShiftRaw(newValue);
        }

        private void ControlSlider_OnValueChanged(double newValue)
        {
            SetControlRaw(newValue);
        }

        private void SetShiftRaw(double value)
        {
            settingShiftAmount = true;

            frontend.ShiftSemitoneAmount = value;
        }

        private void SetControlRaw(double value)
        {
            settingControlAmount = true;

            frontend.ControlSemitoneAmount = value;
        }

        private string GetUIXml()
        {
            return
            $@"<Layout>

<!--Shift-->
                <SliderDisplayWidget
                 Position=""(0%, 0%)""
                 Size=""(100%, 50%)""
                 NumberMinValue=""{Frontend.ShiftAndControlSemitoneRange.Min}""
                 NumberMaxValue=""{Frontend.ShiftAndControlSemitoneRange.Max}""
                 NumberDefaultValue=""{Frontend.DEFAULT_SHIFT_SEMITONE_AMOUNT}""
                 DragIncrement=""1.0""
                 PropertyName=""Shift Key Amount""
                 LabelPosition=""(5%, 5%)""
                 SliderPosition=""(5%, 55%)""
                 SliderSize=""(65%, 40%)""
                 TextFieldPosition=""(75%, 55%)""
                 TextFieldSize=""(20%, 40%)""
                 ResetButtonPosition=""(70%, 5%)""
                 ResetButtonSize=""(25%, 25%)""
                 Name=""{SHIFT_SLIDER_NAME}""/>
                 

<!--Control-->

                <SliderDisplayWidget
                 Position=""(0%, 50%)""
                 Size=""(100%, 50%)""
                 NumberMinValue=""{Frontend.ShiftAndControlSemitoneRange.Min}""
                 NumberMaxValue=""{Frontend.ShiftAndControlSemitoneRange.Max}""
                 NumberDefaultValue=""{Frontend.DEFAULT_CONTROL_SEMITONE_AMOUNT}""
                 DragIncrement=""1.0""
                 PropertyName=""Control Key Amount""
                 LabelPosition=""(5%, 5%)""
                 SliderPosition=""(5%, 55%)""
                 SliderSize=""(65%, 40%)""
                 TextFieldPosition=""(75%, 55%)""
                 TextFieldSize=""(20%, 40%)""
                 ResetButtonPosition=""(70%, 5%)""
                 ResetButtonSize=""(25%, 25%)""
                 Name=""{CONTROL_SLIDER_NAME}""/>

            </Layout>";
        }

        private const string SHIFT_SLIDER_NAME = "ShiftSlider";
        private const string SHIFT_SLIDER_DISPLAY_TEXTFIELD_NAME = "ShiftDisplayTextField";
        private const string SHIFT_RESET_BUTTON_NAME = "ShiftResetButton";

        private const string CONTROL_SLIDER_NAME = "ControlSlider";
        private const string CONTROL_SLIDER_DISPLAY_TEXTFIELD_NAME = "ControlDisplayTextField";
        private const string CONTROL_RESET_BUTTON_NAME = "ControlResetButton";
    }
}