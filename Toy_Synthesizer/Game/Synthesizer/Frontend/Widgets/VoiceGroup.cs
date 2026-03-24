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
using GeoLib.GeoGraphics.UI.Data;
using GeoLib.GeoGraphics.UI.Data.Generic;
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
                    UpdatePropertiesFromVoice();
                }

                OnVoiceChanged?.Invoke(this, previous, voice);
            }
        }

        private PropertyBindable<string> namePropertyBindable;
        private SameTypePropertyBinding<string> nameBinding;

        private PropertyBindable<double> frequencyPropertyBindable;
        private ConvertingPropertyBinding<double, string> frequencyBinding;

        private PropertyBindable<double> attackPropertyBindable;
        private ConvertingPropertyBinding<double, string> attackBinding;

        private PropertyBindable<double> decayPropertyBindable;
        private ConvertingPropertyBinding<double, string> decayBinding;

        private PropertyBindable<double> sustainPropertyBindable;
        private ConvertingPropertyBinding<double, string> sustainBinding;

        private PropertyBindable<double> releasePropertyBindable;
        private ConvertingPropertyBinding<double, string> releaseBinding;

        private PlainLabel NameDisplayLabel;
        private TextField NameTextField;

        private PlainLabel FrequencyDisplayLabel;
        private TextField FrequencyTextField;

        private PlainLabel AttackDisplayLabel;
        private PlainLabel DecayDisplayLabel;
        private PlainLabel SustainDisplayLabel;
        private PlainLabel ReleaseDisplayLabel;
        private TextField AttackTextField;
        private TextField DecayTextField;
        private TextField SustainTextField;
        private TextField ReleaseTextField;

        public event Action<VoiceGroup, Voice, Voice> OnVoiceChanged;

        public VoiceGroup(Vec2f position, Vec2f size, Voice voice, Game game)
            : base(position, size,
                   scrollBarWidth: game.UIManager.GetScrollBarTrackSize(), 
                   style: game.UIManager.ScrollPaneStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            this.game = game;

            InitPropertyBindables();

            game.UIManager.InitScrollPane(this);

            Style.RenderData.SetColor(game.UIManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            CreateWidgets(game.UIManager);

            Voice = voice;

            FindAs<Drawer>(widget => widget is not null).CollapseInternal(false);
        }

        private void InitPropertyBindables()
        {
            namePropertyBindable = new PropertyBindable<string>("Name");
            frequencyPropertyBindable = new PropertyBindable<double>("Frequency");
            attackPropertyBindable = new PropertyBindable<double>("Attack");
            decayPropertyBindable = new PropertyBindable<double>("Decay");
            sustainPropertyBindable = new PropertyBindable<double>("Sustain");
            releasePropertyBindable = new PropertyBindable<double>("Release");

            namePropertyBindable.OnValueChangedTyped += SetVoiceName;
            frequencyPropertyBindable.OnValueChangedTyped += SetFrequency;
            attackPropertyBindable.OnValueChangedTyped += SetAttack;
            decayPropertyBindable.OnValueChangedTyped += SetDecay;
            sustainPropertyBindable.OnValueChangedTyped += SetSustain;
            releasePropertyBindable.OnValueChangedTyped += SetRelease;
        }

        private void CreateWidgets(UIManager uiManager)
        {
            UIXmlParser xmlParser = new UIXmlParser(uiManager);

            xmlParser.AddTypeFactory(new VoiceMixControlGroupFactory());

            string uiXml = GetUIXml();

            xmlParser.Parse(uiXml, rootParent: this);

            NameDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(NameLabelName);
            NameTextField = FindAsByNameDeepSearch<TextField>(NameTextFieldName);

            nameBinding = NameTextField.BindProperty(namePropertyBindable);

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

            nameBinding = NameTextField.BindProperty(namePropertyBindable);

            frequencyBinding = FrequencyTextField.BindProperty_Number(frequencyPropertyBindable);

            attackBinding = AttackTextField.BindProperty_Number(attackPropertyBindable);
            decayBinding = DecayTextField.BindProperty_Number(decayPropertyBindable);
            sustainBinding = SustainTextField.BindProperty_Number(sustainPropertyBindable);
            releaseBinding = ReleaseTextField.BindProperty_Number(releasePropertyBindable);

            InitTooltips(uiManager);
        }

        private void InitTooltips(UIManager uiManager)
        {
            string nameDescription = "The name of this voice";

            string centerFrequencyDescription = $"The center frequency of this voice, around which the oscillators oscillate."
                                                + $"\nMin: {PolyphonicSynthesizer.MIN_CENTER_FREQUENCY}\nMax: {PolyphonicSynthesizer.MAX_CENTER_FREQUENCY}";

            string attackDescription = $"How long it takes to reach peak volume, in seconds." 
                                       + $"\nMin: {PolyphonicSynthesizer.MIN_ATTACK}\nMax: {PolyphonicSynthesizer.MAX_ATTACK}";

            string decayDescription = $"How long it takes to decrease to the sustain level volume, in seconds." 
                                      + $"\nMin: {PolyphonicSynthesizer.MIN_DECAY}\nMax: {PolyphonicSynthesizer.MAX_DECAY}";

            string sustainDescription = $"A scalar representing a percentage of the peak attack volume. The volume decreases over time to this after the decay time has passed." 
                                        + $"\nMin: {PolyphonicSynthesizer.MIN_SUSTAIN}\nMax: {PolyphonicSynthesizer.MAX_SUSTAIN}";

            string releaseDescription = $"How long it takes the volume to fade to zero, in seconds." +
                                        $"\nMin: {PolyphonicSynthesizer.MIN_RELEASE}\nMax: {PolyphonicSynthesizer.MAX_RELEASE}";

            uiManager.AddTextTooltip(NameDisplayLabel, nameDescription);
            uiManager.AddTextTooltip(NameTextField, nameDescription);

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

        private void SetVoiceName(string name)
        {
            if (Voice is null)
            {
                return;
            }

            Name = $"{Voice.Name}_{Voice.CenterFrequency}Hz_VoiceGroup";

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceName(Voice, name));
        }

        private void UpdatePropertiesFromVoice()
        {
            namePropertyBindable.SetValueRaw(Voice.Name);

            frequencyPropertyBindable.SetValueRaw(Voice.CenterFrequency);

            attackPropertyBindable.SetValueRaw(Voice.Adsr.AttackSeconds);
            decayPropertyBindable.SetValueRaw(Voice.Adsr.DecaySeconds);
            sustainPropertyBindable.SetValueRaw(Voice.Adsr.SustainLevel);
            releasePropertyBindable.SetValueRaw(Voice.Adsr.ReleaseSeconds);

            NameTextField.SetTextWithoutProperty(Voice.Name);

            FrequencyTextField.SetTextWithoutProperty(Voice.CenterFrequency.ToString());

            AttackTextField.SetTextWithoutProperty(Voice.Adsr.AttackSeconds.ToString());
            DecayTextField.SetTextWithoutProperty(Voice.Adsr.DecaySeconds.ToString());
            SustainTextField.SetTextWithoutProperty(Voice.Adsr.SustainLevel.ToString());
            ReleaseTextField.SetTextWithoutProperty(Voice.Adsr.ReleaseSeconds.ToString());
        }

        private void SetFrequency(double frequency)
        {
            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceCenterFrequency(Voice, frequency));
        }

        private void SetAdsr(double attack, double decay, double sustain, double release)
        {
            SetAttack(attack);
            SetDecay(decay);
            SetSustain(sustain);
            SetRelease(release);
        }

        private void SetAttack(double attack)
        {
            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceAttack(voice, attack));
        }

        private void SetDecay(double decay)
        {
            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceDecay(voice, decay));
        }

        private void SetSustain(double sustain)
        {
            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceSustain(voice, sustain));
        }

        private void SetRelease(double release)
        {
            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceRelease(voice, release));
        }

        private string GetUIXml()
        {
            return $@"<Layout>

    <PlainLabel Position=""(5%, 5%)""
                Size=""(25%, 12.5%)"" 
                Text=""Name:"" 
                FitText=""false"" 
                GrowWithText=""true"" 
                Name=""{NameLabelName}""/>

    <TextField Position=""(21%, 5%)"" 
               Size=""(22.5%, 12.5%)"" 
               MaxCharacters=""20"" 
               Name=""{NameTextFieldName}""/>

    <PlainLabel Position=""(46%, 5%)"" 
                Size=""(20%, 12.5%)"" 
                Text=""Frequency:"" 
                FitText=""false"" 
                GrowWithText=""true"" 
                Name=""{FrequencyDisplayLabelName}""/>

    <TextField Position=""(72.5%, 5%)"" 
               Size=""(22.5%, 12.5%)"" 
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

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            namePropertyBindable = null;
            frequencyPropertyBindable = null;
            attackPropertyBindable = null;
            decayPropertyBindable = null;
            sustainPropertyBindable = null;
            releasePropertyBindable = null;

            nameBinding.Dispose();
            frequencyBinding.Dispose();
            attackBinding.Dispose(); 
            decayBinding.Dispose();
            sustainBinding.Dispose();
            releaseBinding.Dispose();

            nameBinding = null;
            frequencyBinding = null;
            attackBinding = null;
            decayBinding = null;
            sustainBinding = null;
            releaseBinding = null;
        }

        private const string NameLabelName = "NameLabel";
        private const string NameTextFieldName = "NameTextField";

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
