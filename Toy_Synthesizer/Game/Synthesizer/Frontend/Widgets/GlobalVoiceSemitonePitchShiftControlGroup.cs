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
            frontend = uiManager.Game.Synthesizer.Frontend;

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

        private void PolySynth_ShiftSemitoneAmountChanged(double newValue)
        {
            if (settingShiftAmount)
            {
                settingShiftAmount = false;

                return;
            }

            shiftKeySlider.SetWidgetValues(newValue);
        }

        private void PolySynth_ControlSemitoneAmountChanged(double newValue)
        {
            if (settingControlAmount)
            {
                settingControlAmount = false;

                return;
            }

            controlKeySlider.SetWidgetValues(newValue);
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



/*using System;
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
    public class GlobalSemitoneShiftControlGroup : GroupWidget
    {
        private Frontend frontend;

        private Slider shiftKeySlider;
        private TextField shiftKeySliderDisplayTextField;
        private Slider controlKeySlider;
        private TextField controlKeySliderDisplayTextField;
        private TextButton shiftResetButton;
        private TextButton controlResetButton;

        private bool settingShiftFromSlider = false;
        private bool settingShiftFromDisplayTextField = false;
        private bool settingShift = false;

        private bool settingControlFromSlider = false;
        private bool settingControlFromDisplayTextField = false;
        private bool settingControl = false;

        public GlobalSemitoneShiftControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   style: uiManager.GetDefaultGroupStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            frontend = uiManager.Game.Synthesizer.Frontend;

            Style.RenderData.SetColor(uiManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            string uiXml = GetUIXml();

            new UIXmlParser(uiManager).Parse(uiXml, rootParent: this);

            InitWidgets();
        }

        private void InitWidgets()
        {
            shiftKeySlider = FindAsByNameDeepSearch<Slider>(SHIFT_SLIDER_NAME);
            shiftKeySliderDisplayTextField = FindAsByNameDeepSearch<TextField>(SHIFT_SLIDER_DISPLAY_TEXTFIELD_NAME);
            shiftResetButton = FindAsByNameDeepSearch<TextButton>(SHIFT_RESET_BUTTON_NAME);

            shiftKeySlider.OnValueChange += ShiftSlider_OnValueChanged;
            shiftKeySliderDisplayTextField.OnTextInput += ShiftSliderDisplayTextField_OnTextInput;

            shiftResetButton.OnClick += ShiftResetButton_OnClick;

            SetShiftSlider();
            SetShiftDisplayTextField();
        }

        private void ShiftSlider_OnValueChanged(Slider slide, float previousValue, float newValue)
        {
            if (settingShiftFromSlider)
            {
                settingShiftFromSlider = false;

                return;
            }

            SetShiftAmount(newValue, setSlider: false, setDisplay: true);
        }

        private void ShiftSliderDisplayTextField_OnTextInput(string text)
        {
            if (settingShiftFromDisplayTextField)
            {
                settingShiftFromDisplayTextField = false;

                return;
            }

            double newValue = GeoMath.ParseOrDefault<double>(text);

            SetShiftAmount(newValue, setSlider: true, setDisplay: false);
        }

        private void ShiftResetButton_OnClick()
        {
            SetShiftAmount(Frontend.DEFAULT_SHIFT_SEMITONE_AMOUNT, setSlider: true, setDisplay: true);
        }

        private void SetShiftAmount(double newValue, bool setSlider, bool setDisplay)
        {
            if (settingShift)
            {
                return;
            }

            settingShift = true;

            SetShiftRaw(newValue);

            if (setSlider)
            {
                SetShiftSlider();
            }

            if (setDisplay)
            {
                SetShiftDisplayTextField();
            }

            settingShift = false;
        }

        private void SetShiftSlider()
        {
            settingShiftFromSlider = true;

            shiftKeySlider.CurrentValue = (float)frontend.ShiftSemitoneAmount;
        }

        private void SetShiftDisplayTextField()
        {
            shiftKeySliderDisplayTextField.Text = ((float)frontend.ShiftSemitoneAmount).ToString();
        }

        private void SetShiftRaw(double value)
        {
            frontend.ShiftSemitoneAmount = value;
        }

        private string GetUIXml()
        {
            return
            $@"<Layout>

<!--Shift-->

                <PlainLabel
                 Position=""(5%, 2%)""
                 Size=""(90%, 10%)""
                 Text=""Shift Key Amount""
                 FitText=""false""
                 GrowWithText=""true""/>

                <TextButton
                 Position=""(70%, 2%)""
                 Size=""(25%, 10%)""
                 Text=""Reset""
                 Alignment=""Center""
                 Name=""{SHIFT_RESET_BUTTON_NAME}""/>
                 
                <Slider
                 Position=""(5%, 22%)""
                 Size=""(65%, 16%)""
                 NumberMinValue=""{Frontend.ShiftAndControlSemitoneRange.Min}""
                 NumberMaxValue=""{Frontend.ShiftAndControlSemitoneRange.Max}""
                 NumberDefaultValue=""{Frontend.DEFAULT_SHIFT_SEMITONE_AMOUNT}""
                 DragIncrement=""1.0""
                 Name=""{SHIFT_SLIDER_NAME}""/>

                <TextField
                 Position=""(75%, 22%)""
                 Size=""(20%, 16%)""
                 MaxCharacters=""6""
                 NumberMinValue=""{Frontend.ShiftAndControlSemitoneRange.Min}""
                 NumberMaxValue=""{Frontend.ShiftAndControlSemitoneRange.Max}""
                 NumberAllowedSign=""1""
                 Name=""{SHIFT_SLIDER_DISPLAY_TEXTFIELD_NAME}""/>

<!--Control-->

                <PlainLabel
                 Position=""(5%, 62%)""
                 Size=""(90%, 10%)""
                 Text=""Control Key Amount""
                 FitText=""false""
                 GrowWithText=""true""/>

                <TextButton
                 Position=""(70%, 62%)""
                 Size=""(25%, 10%)""
                 Text=""Reset""
                 Alignment=""Center""
                 Name=""{CONTROL_RESET_BUTTON_NAME}""/>
                 
                <Slider
                 Position=""(5%, 82%)""
                 Size=""(65%, 16%)""
                 NumberMinValue=""{Frontend.ShiftAndControlSemitoneRange.Min}""
                 NumberMaxValue=""{Frontend.ShiftAndControlSemitoneRange.Max}""
                 NumberDefaultValue=""{Frontend.DEFAULT_CONTROL_SEMITONE_AMOUNT}""
                 DragIncrement=""1.0""
                 Name=""{CONTROL_SLIDER_NAME}""/>

                <TextField
                 Position=""(75%, 82%)""
                 Size=""(20%, 16%)""
                 MaxCharacters=""6""
                 NumberMinValue=""{Frontend.ShiftAndControlSemitoneRange.Min}""
                 NumberMaxValue=""{Frontend.ShiftAndControlSemitoneRange.Max}""
                 NumberAllowedSign=""1""
                 Name=""{CONTROL_SLIDER_DISPLAY_TEXTFIELD_NAME}""/>

            </Layout>";
        }

        private const string SHIFT_SLIDER_NAME = "ShiftSlider";
        private const string SHIFT_SLIDER_DISPLAY_TEXTFIELD_NAME = "ShiftDisplayTextField";
        private const string SHIFT_RESET_BUTTON_NAME = "ShiftResetButton";

        private const string CONTROL_SLIDER_NAME = "ControlSlider";
        private const string CONTROL_SLIDER_DISPLAY_TEXTFIELD_NAME = "ControlDisplayTextField";
        private const string CONTROL_RESET_BUTTON_NAME = "ControlResetButton";
    }
}*/