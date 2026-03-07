using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.Slicing;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Toy_Synthesizer.Game.Synthesizer;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    public class VoiceGroup : ScrollPane
    {
        private Voice voice;

        public Voice Voice
        {
            get => voice;

            set
            {
                Voice previous = voice;

                voice = value;

                if (voice is not null)
                {
                    SetName();

                    string frequencyString = voice.CenterFrequency.ToString();

                    SetFrequency(frequencyString);

                    FrequencyTextField.Text = frequencyString;

                    string attackString = voice.AdsrEnvelope?.AttackSeconds.ToString();
                    string decayString = voice.AdsrEnvelope?.DecaySeconds.ToString();
                    string sustainString = voice.AdsrEnvelope?.SustainLevel.ToString();
                    string releaseString = voice.AdsrEnvelope?.ReleaseSeconds.ToString();

                    SetAdsr(attackString, 
                            decayString, 
                            sustainString,
                            releaseString);

                    AttackTextField.Text = attackString;
                    DecayTextField.Text = decayString;
                    SustainTextField.Text = sustainString;
                    ReleaseTextField.Text = releaseString;
                }

                OnVoiceChanged?.Invoke(this, previous, voice);
            }
        }

        public PlainLabel NameLabel;

        public PlainLabel FrequencyDisplayLabel;
        public TextField FrequencyTextField;

        public PlainLabel AttackDisplayLabel;
        public PlainLabel DecayDisplayLabel;
        public PlainLabel SustainDisplayLabel;
        public PlainLabel ReleaseDisplayLabel;
        public TextField AttackTextField;
        public TextField DecayTextField;
        public TextField SustainTextField;
        public TextField ReleaseTextField;

        public event Action<VoiceGroup, Voice, Voice> OnVoiceChanged;

        /*private float minWidgetEdgeSpacingScalar;
        private float nameLabelFrequencyTextFieldVerticalSpacingScalar;*/

        public VoiceGroup(Vec2f position, Vec2f size, Voice voice, UIManager uiManager)
            : base(position, size,
                   scrollBarWidth: uiManager.GetScrollBarTrackSize(), 
                   style: uiManager.ScrollPaneStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            uiManager.InitScrollPane(this);

            Style.RenderData.SetColor(uiManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            CreateWidgets(uiManager);

            InitNameWidgets(uiManager);
            InitCenterFrequencyWidgets(uiManager);
            InitAdsrWidgets(uiManager);
            InitOscillatorWidgets();

            this.Voice = voice;

            FindAs<Drawer>(widget => widget is not null).CollapseInternal(false);
        }

        private void CreateWidgets(UIManager uiManager)
        {
            UIXmlParser xmlParser = new UIXmlParser(uiManager);

            xmlParser.AddTypeFactory(new VoiceMixControlGroupFactory());

            string uiXml = GetUIXml();

            xmlParser.Parse(uiXml, rootParent: this);
        }

        private void InitNameWidgets(UIManager uiManager)
        {
            SetAndInitPlainLabel(ref NameLabel, NameLabelName);
        }

        private void InitCenterFrequencyWidgets(UIManager uiManager)
        {
            NumberRange<double> frequencyRange = NumberRange<double>.From(min: 0.0, max: 32_000.0);
            TextNumberFilterData<double> numberFilterData = new TextNumberFilterData<double>(frequencyRange, 
                                                                                             allowedSign: 1,
                                                                                             allowFractional: true);

            SetAndInitPlainLabel(ref FrequencyDisplayLabel, FrequencyDisplayLabelName);
            SetAndInitNumberTextField(ref FrequencyTextField, FrequencyTextFieldName, numberFilterData, Voice?.CenterFrequency, SetFrequency);
        }

        private void InitAdsrWidgets(UIManager uiManager)
        {
            NumberRange<double> attackRange = NumberRange<double>.From(min: 0.0, max: double.MaxValue * 0.5);
            TextNumberFilterData<double> attackNumberFilterData = new TextNumberFilterData<double>(attackRange,
                                                                                                   allowedSign: 1,
                                                                                                   allowFractional: true);

            TextNumberFilterData<double> decayNumberFilterData = attackNumberFilterData;

            NumberRange<double> sustainRange = NumberRange<double>.From(min: 0.0, max: 1.0);
            TextNumberFilterData<double> sustainNumberFilterData = new TextNumberFilterData<double>(sustainRange,
                                                                                                    allowedSign: 1,
                                                                                                    allowFractional: true);

            TextNumberFilterData<double> releaseNumberFilterData = attackNumberFilterData;

            SetAndInitPlainLabel(ref AttackDisplayLabel, AttackDisplayLabelName);
            SetAndInitNumberTextField(ref AttackTextField, AttackTextFieldName, attackNumberFilterData, Voice?.AdsrEnvelope?.AttackSeconds, SetAttack);

            SetAndInitPlainLabel(ref DecayDisplayLabel, DecayDisplayLabelName);
            SetAndInitNumberTextField(ref DecayTextField, DecayTextFieldName, decayNumberFilterData, Voice?.AdsrEnvelope.DecaySeconds, SetDecay);

            SetAndInitPlainLabel(ref SustainDisplayLabel, SustainDisplayLabelName);
            SetAndInitNumberTextField(ref SustainTextField, SustainTextFieldName, sustainNumberFilterData, Voice?.AdsrEnvelope.SustainLevel, SetSustain);

            SetAndInitPlainLabel(ref ReleaseDisplayLabel, ReleaseDisplayLabelName);
            SetAndInitNumberTextField(ref ReleaseTextField, ReleaseTextFieldName, releaseNumberFilterData, Voice?.AdsrEnvelope?.ReleaseSeconds, SetRelease);
        }

        private void InitOscillatorWidgets()
        {
            
        }

        private void SetName()
        {
            if (Voice is null)
            {
                return;
            }

            Name = $"{Voice.Name}_{Voice.CenterFrequency}Hz_VoiceGroup";

            NameLabel.Text = GetNameLabelText();
        }

        private void SetFrequency(string text)
        {
            if (TextUtils.IsNullEmptyOrWhitespace(text))
            {
                Voice.CenterFrequency = 0.0;
            }
            else
            {
                Voice.CenterFrequency = GeoMath.ParseOrDefault<double>(text);
            }
        }

        private void SetAdsr(string attackText, string decayText, string sustainText, string releaseText)
        {
            SetAttack(attackText);
            SetDecay(decayText);
            SetSustain(sustainText);
            SetRelease(releaseText);
        }

        private void SetAttack(string text)
        {
            if (text is null || text.Length == 0)
            {
                return;
            }

            voice.AdsrEnvelope.AttackSeconds = GeoMath.ParseOrDefault<double>(text);
        }

        private void SetDecay(string text)
        {
            if (text is null || text.Length == 0)
            {
                return;
            }

            voice.AdsrEnvelope.DecaySeconds = GeoMath.ParseOrDefault<double>(text);
        }

        private void SetSustain(string text)
        {
            if (text is null || text.Length == 0)
            {
                return;
            }

            voice.AdsrEnvelope.SustainLevel = GeoMath.ParseOrDefault<double>(text);
        }

        private void SetRelease(string text)
        {
            if (text is null || text.Length == 0)
            {
                return;
            }

            voice.AdsrEnvelope.ReleaseSeconds = GeoMath.ParseOrDefault<double>(text);
        }

        private string GetNameLabelText()
        {
            if (Voice is null)
            {
                return "Name: ";
            }

            return "Name: " + Voice.Name;
        }

        /*private AABB GetNameLabelBounds()
        {
            return new AABB
            {
                Position = Position + GetNameLabelPositionValue().Compute(Size),
                Size = GetNameLabelSizeValue().Compute(Size)
            };
        }

        private AABB GetFrequencyTextFieldBounds()
        {
            return new AABB
            {
                Position = Position + GetFrequencyTextFieldPositionValue().Compute(Size),
                Size = GetFrequencyTextFieldSizeValue().Compute(Size)
            };
        }

        private Vec2fValue GetNameLabelPositionValue()
        {
            return Vec2fValue.Normalized(minWidgetEdgeSpacingScalar);
        }

        private Vec2fValue GetNameLabelSizeValue()
        {
            return Vec2fValue.Normalized(0.4f, 0.4f);
        }

        private Vec2fValue GetFrequencyTextFieldPositionValue()
        {
            Vec2fValue nameLabelPosition = GetNameLabelPositionValue();

            Vec2fValue size = GetFrequencyTextFieldSizeValue();

            return Vec2fValue.Normalized(nameLabelPosition.Value.X, 1f - size.Value.Y - minWidgetEdgeSpacingScalar);
        }

        private Vec2fValue GetFrequencyTextFieldSizeValue()
        {
            return Vec2fValue.Normalized(0.25f, 0.4f);
        }*/

        /*private float ComputeMinWidgetEdgeSpacing()
        {
            return Size.Min() * minWidgetEdgeSpacingScalar;
        }*/

        private string GetUIXml()
        {
            return $@"<Layout>

    <PlainLabel Position=""(5%, 5%)""
                Size=""(40%, 40%)"" 
                Text=""{GetNameLabelText()}"" 
                FitText=""false"" 
                GrowWithText=""true"" 
                Name=""{NameLabelName}""/>

    <PlainLabel Position=""(30%, 5%)"" 
                Size=""(25%, 12.5%)"" 
                Text=""Frequency:"" 
                FitText=""false"" 
                GrowWithText=""true"" 
                Name=""{FrequencyDisplayLabelName}""/>

    <TextField Position=""(57.5%, 5%)"" 
               Size=""(25%, 12.5%)"" 
               MaxCharacters=""20""
               Name=""{FrequencyTextFieldName}""/>   

<!--ADSR-->

    <Drawer Position=""(5%, 21.25%)"" 
            Size=""(25%, 12.5%)"" 
            CoverText=""ADSR""
            Name=""{AdsrDrawerName}"">

        <PlainLabel Position=""(20%, 120%)"" 
                    Size=""(100%, 100%)""
                    Text=""Attack:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{AttackDisplayLabelName}""/>

        <PlainLabel Position=""(20%, 260%)"" 
                    Size=""(100%, 100%)""
                    Text=""Decay:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{DecayDisplayLabelName}""/>

        <PlainLabel Position=""(20%, 400%)"" 
                    Size=""(100%, 100%)""
                    Text=""Sustain:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{SustainDisplayLabelName}""/>

        <PlainLabel Position=""(20%, 540%)"" 
                    Size=""(100%, 100%)""
                    Text=""Release:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{ReleaseDisplayLabelName}""/>

        <TextField Position=""(130%, 120%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20""
                   Name=""{AttackTextFieldName}""/>

        <TextField Position=""(130%, 260%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20""
                   Name=""{DecayTextFieldName}""/>

        <TextField Position=""(130%, 400%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20""
                   Name=""{SustainTextFieldName}""/>

        <TextField Position=""(130%, 540%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20""
                   Name=""{ReleaseTextFieldName}""/>

    </Drawer>

    <VoiceMixControlGroup Position=""(5%, 38.75%)""
                          Size=""(90%, 20%)""/>

</Layout>";
        }

        /*private string GetUIXml()
        {
            float minWidgetEdgeSpacing_Percent = GeoMath.ScalarToPercent(minWidgetEdgeSpacingScalar);

            float widgetVerticalSpacing_Percent = GeoMath.ScalarToPercent(0.1f);

            float widgetBeginX_Scalar = GeoMath.PercentToScalar(minWidgetEdgeSpacing_Percent);
            float widgetBeginX_Percent = GeoMath.ScalarToPercent(widgetBeginX_Scalar);
            float widgetBeginY_Percent = widgetBeginX_Percent;

            float displayLabelTextFieldHorizontalSpacing_Percent = GeoMath.ScalarToPercent(0.275f);

            float targetWidgetHeight_Percent = 12.5f;

            Vec2f adsrDrawerSize_Percent = new Vec2f(25f, targetWidgetHeight_Percent);
            Vec2f adsrDrawerPosition_Percent = new Vec2f(widgetBeginX_Percent, widgetBeginY_Percent + (targetWidgetHeight_Percent * 0.5f) + widgetVerticalSpacing_Percent);

            Vec2f adsrDrawerToGroupSize_Scalar = Size / ((adsrDrawerSize_Percent * 0.01f) * Size);

            float adsrDrawerVerticalSpacing_Percent = widgetVerticalSpacing_Percent * 0.5f * adsrDrawerToGroupSize_Scalar.Y; 

            Vec2f adsrDrawerWidgetBeginPosition_Percent = new Vec2f(widgetBeginX_Percent, widgetBeginY_Percent * 2f) * adsrDrawerToGroupSize_Scalar;

            float adsrTextFieldPositionX_Percent = (widgetBeginX_Percent + displayLabelTextFieldHorizontalSpacing_Percent) * adsrDrawerToGroupSize_Scalar.X;

            // This doesn't matter for the plain labels. They will be sized to the text.
            Vec2f widgetSizes_Percent = new Vec2f(25f, targetWidgetHeight_Percent);

            Vec2f adsrDrawerWidgetSizes_Percent = new Vec2f(25f, targetWidgetHeight_Percent) * adsrDrawerToGroupSize_Scalar;

            float frequencyDisplayLabelX_Percent = widgetBeginX_Percent + GeoMath.ScalarToPercent(0.25f);

            float frequencyTextFieldX_Percent = frequencyDisplayLabelX_Percent + displayLabelTextFieldHorizontalSpacing_Percent;
            float frequencyTextFieldY_Percent = widgetBeginY_Percent;

            float frequencyDisplayLabelY_Percent = widgetBeginY_Percent;

            Vec2f attackDisplayLabelPosition_Percent = adsrDrawerWidgetBeginPosition_Percent + new Vec2f(0f, adsrDrawerVerticalSpacing_Percent);
            Vec2f decayDisplayLabelPosition_Percent = new Vec2f(adsrDrawerWidgetBeginPosition_Percent.X, attackDisplayLabelPosition_Percent.Y + adsrDrawerWidgetSizes_Percent.Y + adsrDrawerVerticalSpacing_Percent);
            Vec2f sustainDisplayLabelPosition_Percent = new Vec2f(adsrDrawerWidgetBeginPosition_Percent.X, decayDisplayLabelPosition_Percent.Y + adsrDrawerWidgetSizes_Percent.Y + adsrDrawerVerticalSpacing_Percent);
            Vec2f releaseDisplayLabelPosition_Percent = new Vec2f(adsrDrawerWidgetBeginPosition_Percent.X, sustainDisplayLabelPosition_Percent.Y + adsrDrawerWidgetSizes_Percent.Y + adsrDrawerVerticalSpacing_Percent);

            Vec2f attackTextFieldPosition_Percent = new Vec2f(adsrTextFieldPositionX_Percent, attackDisplayLabelPosition_Percent.Y);
            Vec2f decayTextFieldPosition_Percent = new Vec2f(adsrTextFieldPositionX_Percent, decayDisplayLabelPosition_Percent.Y);
            Vec2f sustainTextFieldPosition_Percent = new Vec2f(adsrTextFieldPositionX_Percent, sustainDisplayLabelPosition_Percent.Y);
            Vec2f releaseTextFieldPosition_Percent = new Vec2f(adsrTextFieldPositionX_Percent, releaseDisplayLabelPosition_Percent.Y);

            return
            $@"
            <Layout>

                <PlainLabel Position=""({widgetBeginX_Percent}%, {widgetBeginY_Percent}%)"" 
                            Size=""(40%, 40%)"" 
                            Text=""{GetNameLabelText()}"" 
                            FitText=""false"" 
                            GrowWithText=""true"" 
                            Name=""{NameLabelName}""/>

                <PlainLabel Position=""({frequencyDisplayLabelX_Percent}%, {frequencyDisplayLabelY_Percent}%)"" 
                            Size=""({widgetSizes_Percent.X}%, {widgetSizes_Percent.Y}%)"" 
                            Text=""Frequency:"" 
                            FitText=""false"" 
                            GrowWithText=""true"" 
                            Name=""{FrequencyDisplayLabelName}""/>

                <TextField Position=""({frequencyTextFieldX_Percent}%, {frequencyTextFieldY_Percent}%)"" 
                           Size=""({widgetSizes_Percent.X}%, {widgetSizes_Percent.Y}%)"" 
                           MaxCharacters=""20""
                           Name=""{FrequencyTextFieldName}""/>   



                <Drawer Position=""({adsrDrawerPosition_Percent.X}%, {adsrDrawerPosition_Percent.Y}%)"" 
                        Size=""({adsrDrawerSize_Percent.X}%, {adsrDrawerSize_Percent.Y}%)"" 
                        CoverText=""ADSR""
                        Name=""{AdsrDrawerName}"">



                <PlainLabel Position=""({attackDisplayLabelPosition_Percent.X}%, {attackDisplayLabelPosition_Percent.Y}%)"" 
                           Size=""({adsrDrawerWidgetSizes_Percent.X}%, {adsrDrawerWidgetSizes_Percent.Y}%)""
                           Text=""Attack:"" 
                           FitText=""false"" 
                           GrowWithText=""true"" 
                           Name=""{AttackDisplayLabelName}""/>

                <PlainLabel Position=""({decayDisplayLabelPosition_Percent.X}%, {decayDisplayLabelPosition_Percent.Y}%)"" 
                           Size=""({adsrDrawerWidgetSizes_Percent.X}%, {adsrDrawerWidgetSizes_Percent.Y}%)""
                           Text=""Decay:"" 
                           FitText=""false"" 
                           GrowWithText=""true"" 
                           Name=""{DecayDisplayLabelName}""/>

                <PlainLabel Position=""({sustainDisplayLabelPosition_Percent.X}%, {sustainDisplayLabelPosition_Percent.Y}%)"" 
                           Size=""({adsrDrawerWidgetSizes_Percent.X}%, {adsrDrawerWidgetSizes_Percent.Y}%)""
                           Text=""Sustain:"" 
                           FitText=""false"" 
                           GrowWithText=""true"" 
                           Name=""{SustainDisplayLabelName}""/>

                <PlainLabel Position=""({releaseDisplayLabelPosition_Percent.X}%, {releaseDisplayLabelPosition_Percent.Y}%)"" 
                           Size=""({adsrDrawerWidgetSizes_Percent.X}%, {adsrDrawerWidgetSizes_Percent.Y}%)""
                           Text=""Release:"" 
                           FitText=""false"" 
                           GrowWithText=""true"" 
                           Name=""{ReleaseDisplayLabelName}""/>

                <TextField Position=""({attackTextFieldPosition_Percent.X}%, {attackTextFieldPosition_Percent.Y}%)"" 
                           Size=""({adsrDrawerWidgetSizes_Percent.X}%, {adsrDrawerWidgetSizes_Percent.Y}%)"" 
                           MaxCharacters=""20""
                           Name=""{AttackTextFieldName}""/>

                <TextField Position=""({decayTextFieldPosition_Percent.X}%, {decayTextFieldPosition_Percent.Y}%)"" 
                           Size=""({adsrDrawerWidgetSizes_Percent.X}%, {adsrDrawerWidgetSizes_Percent.Y}%)"" 
                           MaxCharacters=""20""
                           Name=""{DecayTextFieldName}""/>

                <TextField Position=""({sustainTextFieldPosition_Percent.X}%, {sustainTextFieldPosition_Percent.Y}%)"" 
                           Size=""({adsrDrawerWidgetSizes_Percent.X}%, {adsrDrawerWidgetSizes_Percent.Y}%)"" 
                           MaxCharacters=""20""
                           Name=""{SustainTextFieldName}""/>

                <TextField Position=""({releaseTextFieldPosition_Percent.X}%, {releaseTextFieldPosition_Percent.Y}%)"" 
                           Size=""({adsrDrawerWidgetSizes_Percent.X}%, {adsrDrawerWidgetSizes_Percent.Y}%)"" 
                           MaxCharacters=""20""
                           Name=""{ReleaseTextFieldName}""/>

                </Drawer>

            </Layout>";
        }*/

        private void SetAndInitPlainLabel(ref PlainLabel label, string name)
        {
            label = FindAsByNameDeepSearch<PlainLabel>(name);
        }

        private void SetAndInitNumberTextField(ref TextField textField, string name,
                                               TextNumberFilterData<double> filterData, double? defaultValue,
                                               Action<string> onTextInput)
        {
            textField = FindAsByNameDeepSearch<TextField>(name);

            textField.TextFilter = TextFilter.Numeric(filterData);

            if (defaultValue.HasValue)
            {
                textField.Text = defaultValue.Value.ToString();
            }

            textField.OnTextInput += onTextInput;
        }

        private const string NameLabelName = "NameLabel";

        private const string FrequencyDisplayLabelName = "FrequencyDisplayLabel";
        private const string FrequencyTextFieldName = "FrequencyTextField";

        private const string AdsrDrawerName = "AdsrDrawer";

        private const string AttackDisplayLabelName = "AttackDisplayLabel";
        private const string DecayDisplayLabelName = "DecayDisplayLabel";
        private const string SustainDisplayLabelName = "SustainDisplayLabel";
        private const string ReleaseDisplayLabelName = "ReleaseDisplayLabel";

        private const string AttackTextFieldName = "AttackTextField";
        private const string DecayTextFieldName = "DecayTextField";
        private const string SustainTextFieldName = "SustainTextField";
        private const string ReleaseTextFieldName = "ReleaseTextField";

        private const string OscillatorsDrawerName = "OscillatorsDrawer";

        private class VoiceMixControlGroupFactory : UIXmlParser.TypeFactory
        {
            public VoiceMixControlGroupFactory() : base("VoiceMixControlGroup")
            {

            }

            public override Widget Create(UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new VoiceMixControlGroup(position, size,
                                                uiManager: uiManager);
            }
        }
    }
}
