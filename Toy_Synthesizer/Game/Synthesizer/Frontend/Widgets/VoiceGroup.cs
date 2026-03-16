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

using Toy_Synthesizer.Game.DigitalSignalProcessing;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    // TODO: Implement usage of ranges in PolyphonicSynthesizer and implement command usage.
    // TODO: Implement oscillator control group.
    public class VoiceGroup : ScrollPane
    {
        private Game game;

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

                    string attackString = voice.Adsr?.AttackSeconds.ToString();
                    string decayString = voice.Adsr?.DecaySeconds.ToString();
                    string sustainString = voice.Adsr?.SustainLevel.ToString();
                    string releaseString = voice.Adsr?.ReleaseSeconds.ToString();

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

        public VoiceGroup(Vec2f position, Vec2f size, Voice voice, Game game)
            : base(position, size,
                   scrollBarWidth: game.UIManager.GetScrollBarTrackSize(), 
                   style: game.UIManager.ScrollPaneStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            this.game = game;

            game.UIManager.InitScrollPane(this);

            Style.RenderData.SetColor(game.UIManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            CreateWidgets(game.UIManager);

            Voice = voice;

            FindAs<Drawer>(widget => widget is not null).CollapseInternal(false);
        }

        private void CreateWidgets(UIManager uiManager)
        {
            UIXmlParser xmlParser = new UIXmlParser(uiManager);

            xmlParser.AddTypeFactory(new VoiceMixControlGroupFactory());

            string uiXml = GetUIXml();

            xmlParser.Parse(uiXml, rootParent: this);

            NameLabel = FindAsByNameDeepSearch<PlainLabel>(NameLabelName);

            FrequencyDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(FrequencyDisplayLabelName);
            FrequencyTextField = FindAsByNameDeepSearch<TextField>(FrequencyTextFieldName);

            AttackDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(AttackDisplayLabelName);
            DecayDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(DecayDisplayLabelName);
            SustainDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(SustainDisplayLabelName);
            ReleaseDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(ReleaseDisplayLabelName);

            AttackTextField = FindAsByNameDeepSearch<TextField>(AttackTextFieldName);
            DecayTextField = FindAsByNameDeepSearch<TextField>(DecayTextFieldName);
            SustainTextField = FindAsByNameDeepSearch<TextField>(SustainTextFieldName);
            ReleaseTextField = FindAsByNameDeepSearch<TextField>(ReleaseTextFieldName);

            AttackTextField.OnTextInput += SetAttack;
            DecayTextField.OnTextInput += SetDecay;
            SustainTextField.OnTextInput += SetSustain;
            ReleaseTextField.OnTextInput += SetRelease;

            InitTooltips(uiManager);
        }

        private void InitTooltips(UIManager uiManager)
        {
            string centerFrequencyDescription = $"The center frequency of this voice, around which the oscillators oscillate."
                                                + $"\nMin: {PolyphonicSynthesizer.MIN_CENTER_FREQUENCY}\nMax: {PolyphonicSynthesizer.MAX_CENTER_FREQUENCY}";

            string attackDescription = $"How long it takes to increase to peak volume, in seconds." 
                                       + $"\nMin: {PolyphonicSynthesizer.MIN_ATTACK}\nMax: {PolyphonicSynthesizer.MAX_ATTACK}";

            string decayDescription = $"How long it takes to decrease to the sustain level volume, in seconds." 
                                      + $"\nMin: {PolyphonicSynthesizer.MIN_DECAY}\nMax: {PolyphonicSynthesizer.MAX_DECAY}";

            string sustainDescription = $"A scalar representing a percentage of the peak attack volume. The volume decreases over time to this after the decay time has passed." 
                                        + $"\nMin: {PolyphonicSynthesizer.MIN_SUSTAIN}\nMax: {PolyphonicSynthesizer.MAX_SUSTAIN}";

            string releaseDescription = $"How long it takes the volume to fade to zero, in seconds." +
                                        $"\nMin: {PolyphonicSynthesizer.MIN_RELEASE}\nMax: {PolyphonicSynthesizer.MAX_RELEASE}";

            uiManager.AddTextTooltip(FrequencyDisplayLabel, centerFrequencyDescription);
            uiManager.AddTextTooltip(FrequencyTextField, centerFrequencyDescription);

            uiManager.AddTextTooltip(AttackDisplayLabel, attackDescription);
            uiManager.AddTextTooltip(AttackTextField, attackDescription);

            uiManager.AddTextTooltip(DecayDisplayLabel, decayDescription);
            uiManager.AddTextTooltip(DecayTextField, decayDescription);

            uiManager.AddTextTooltip(SustainDisplayLabel, sustainDescription);
            uiManager.AddTextTooltip(SustainTextField, sustainDescription);

            uiManager.AddTextTooltip(ReleaseDisplayLabel, releaseDescription);
            uiManager.AddTextTooltip(ReleaseTextField, releaseDescription);
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
            double value;

            if (TextUtils.IsNullEmptyOrWhitespace(text))
            {
                value = 0.0;
            }
            else
            {
                value = GeoMath.ParseOrDefault<double>(text);
            }

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceCenterFrequency(Voice, value));
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
            double value = GeoMath.ParseOrDefault<double>(text);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceAttack(voice, value));
        }

        private void SetDecay(string text)
        {
            double value = GeoMath.ParseOrDefault<double>(text);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceDecay(voice, value));
        }

        private void SetSustain(string text)
        {
            double value = GeoMath.ParseOrDefault<double>(text);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceSustain(voice, value));
        }

        private void SetRelease(string text)
        {
            double value = GeoMath.ParseOrDefault<double>(text);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceRelease(voice, value));
        }

        private string GetNameLabelText()
        {
            if (Voice is null)
            {
                return "Name: ";
            }

            return "Name: " + Voice.Name;
        }

        private string GetUIXml()
        {
            return $@"<Layout>

    <PlainLabel Position=""(5%, 5%)""
                Size=""(25%, 12.5%)"" 
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
               NumberMinValue=""{PolyphonicSynthesizer.CenterFrequencyRange.Min}""
               NumberMaxValue=""{PolyphonicSynthesizer.CenterFrequencyRange.Max}""
               TreatAsScalarPercentage=""false""
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
                   NumberMinValue=""{PolyphonicSynthesizer.AttackRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.AttackRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{AttackTextFieldName}""/>

        <TextField Position=""(130%, 260%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20"" 
                   NumberMinValue=""{PolyphonicSynthesizer.DecayRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.DecayRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{DecayTextFieldName}""/>

        <TextField Position=""(130%, 400%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20"" 
                   NumberMinValue=""{PolyphonicSynthesizer.SustainRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.SustainRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{SustainTextFieldName}""/>

        <TextField Position=""(130%, 540%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20"" 
                   NumberMinValue=""{PolyphonicSynthesizer.ReleaseRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.ReleaseRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{ReleaseTextFieldName}""/>

    </Drawer>

    <VoiceMixControlGroup Position=""(5%, 38.75%)""
                          Size=""(90%, 20%)""/>

</Layout>";
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
                                                game: uiManager.Game);
            }
        }
    }
}
