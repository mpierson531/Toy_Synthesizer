using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using GeoLib;
using GeoLib.GeoInput;
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
using Microsoft.Xna.Framework.Input;

using Toy_Synthesizer.Game.DigitalSignalProcessing;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    // TODO: Improve keybinding stuff.
    // TODO: Implement more flexible keybindings (like multiple possible key combos; I'm thinking a drawer where you can add or remove keybindings and change AND/OR for the keybinding)
    public class VoiceGroup : ScrollPane
    {
        internal Game game;

        private VoiceFrontend voiceFrontend;

        private Voice voice;

        private UIXmlParser uiXmlParser;

        public Voice Voice
        {
            get => voice;

            set
            {
                Voice previous = voice;

                voice = value;

                if (voice is not null)
                {
                    UpdateFromVoice();
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

        //private ViewableList<VoiceOscillatorControlGroup> currentOscillatorControlGroups;

        private PlainLabel NameDisplayLabel;
        private TextField NameTextField;

        private PlainLabel FrequencyDisplayLabel;
        private TextField FrequencyTextField;

        private PlainLabel KeybindingDisplayLabel;
        private TextButton KeybindingButton;

        private PlainLabel AttackDisplayLabel;
        private PlainLabel DecayDisplayLabel;
        private PlainLabel SustainDisplayLabel;
        private PlainLabel ReleaseDisplayLabel;
        private TextField AttackTextField;
        private TextField DecayTextField;
        private TextField SustainTextField;
        private TextField ReleaseTextField;

        private Drawer oscillatorsDrawer;

        private Button addOscillatorButton;
        private IListener addOscillatorsButtonTooltip;

        private bool isSettingKeybinding;
        private readonly ViewableList<Keys> pendingKeybindingKeys;
        private InputListener uiKeybindingInputSetterListener;
        private readonly ImmutableArray<Keys> invalidKeybindingKeys;
        private bool activatedBindingIsEmpty;
        private string activatedKeybindingPreviousText;

        private float drawerBeginX_Percent = 20f;
        private float drawerBeginY_Percent = 160f;
        private readonly float oscillatorsVerticalDrawerSpacing_Percent = 50f;
        private readonly Vec2f oscillatorsDrawerChildSize_Percent = new Vec2f(240f, 500f);

        public event Action<VoiceGroup, Voice, Voice> OnVoiceChanged;

        public VoiceGroup(VoiceFrontend voiceFrontend, Vec2f position, Vec2f size, Voice voice, Game game)
            : base(position, size,
                   scrollBarWidth: game.UIManager.GetScrollBarTrackSize(), 
                   style: game.UIManager.ScrollPaneStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            this.game = game;

            this.voiceFrontend = voiceFrontend;

            invalidKeybindingKeys = new ImmutableArray<Keys>(new Keys[]
            {
                Keys.Enter,
                Keys.Escape
            });

            pendingKeybindingKeys = new ViewableList<Keys>();

            InitPropertyBindables();

            InitUIKeybindingInputSetterListener();

            //currentOscillatorControlGroups = new ViewableList<VoiceOscillatorControlGroup>(100);

            game.UIManager.InitScrollPane(this);

            Style.RenderData.SetColor(game.UIManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            CreateWidgets(game.UIManager);

            Voice = voice;
        }

        private void InitPropertyBindables()
        {
            namePropertyBindable = new PropertyBindable<string>("Name");

            frequencyPropertyBindable = new PropertyBindable<double>("Frequency");

            attackPropertyBindable = new PropertyBindable<double>("Attack");
            decayPropertyBindable = new PropertyBindable<double>("Decay");
            sustainPropertyBindable = new PropertyBindable<double>("Sustain");
            releasePropertyBindable = new PropertyBindable<double>("Release");

            namePropertyBindable.OnValueChangedTyped += SetVoiceNameInternal;

            frequencyPropertyBindable.OnValueChangedTyped += SetFrequencyInternal;

            attackPropertyBindable.OnValueChangedTyped += SetAttackInternal;
            decayPropertyBindable.OnValueChangedTyped += SetDecayInternal;
            sustainPropertyBindable.OnValueChangedTyped += SetSustainInternal;
            releasePropertyBindable.OnValueChangedTyped += SetReleaseInternal;
        }

        private void CreateWidgets(UIManager uiManager)
        {
            uiXmlParser = new UIXmlParser(uiManager.Game);

            uiXmlParser.AddTypeFactory(new VoiceMixControlGroupFactory());
            uiXmlParser.AddTypeFactory(new VoiceOscillatorControlGroupFactory());

            string uiXml = GetUIXml();

            uiXmlParser.Parse(uiXml, rootParent: this);

            NameDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(NameLabelName);
            NameTextField = FindAsByNameDeepSearch<TextField>(NameTextFieldName);

            nameBinding = NameTextField.BindProperty(namePropertyBindable);

            FrequencyDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(FrequencyDisplayLabelName);
            FrequencyTextField = FindAsByNameDeepSearch<TextField>(FrequencyTextFieldName);

            KeybindingDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(KeybindingLabelName);
            KeybindingButton = FindAsByNameDeepSearch<TextButton>(KeybindingButtonName);

            KeybindingButton.OnClick += ActivateKeybindingSetting;

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

            oscillatorsDrawer = FindAsByNameDeepSearch<Drawer>(OscillatorsDrawerName);

            addOscillatorButton = FindAsByNameDeepSearch<Button>(AddOscillatorButtonName);

            addOscillatorButton.OnClick += AddOscillatorButton_OnClick;

            addOscillatorsButtonTooltip = uiManager.AddTextTooltip(addOscillatorButton, "Click to add an oscillator");

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

        private void AddOscillatorButton_OnClick()
        {
            // TODO: Add animation/velocity-based scrolling to the position of the new oscillator widget

            Oscillator oscillator = PolyphonicSynthesizer.CreateDefaultOscillator(Voice.CenterFrequency);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.AddVoiceOscillator(Voice, oscillator));

            string uiXml = GetOscillatorUIXml(drawerBeginY_Percent);

            VoiceOscillatorControlGroup newOscillatorWidget = (VoiceOscillatorControlGroup)uiXmlParser.Parse(uiXml,
                                                                                                             rootParentBaseBounds: new AABB(oscillatorsDrawer.Position, oscillatorsDrawer.PreExpansionSize))[0];

            UIManager.ShiftDrawer_ChildAdded(oscillatorsDrawer, newOscillatorWidget);

            newOscillatorWidget.oscillator = oscillator;

            oscillatorsDrawer.AddChild(newOscillatorWidget);

            newOscillatorWidget.UpdateFromVoice();
        }

        internal void RemoveOscillatorAndGroup(VoiceOscillatorControlGroup oscillatorControlGroup)
        {
            if (!oscillatorsDrawer.Contains(oscillatorControlGroup))
            {
                throw new InvalidOperationException("Oscillator does not exist.");
            }

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.RemoveVoiceOscillator(Voice, oscillatorControlGroup.oscillator));

            UIManager.ShiftDrawer_ChildRemoved(oscillatorsDrawer, oscillatorControlGroup);

            oscillatorsDrawer.RemoveChild(oscillatorControlGroup);
        }

        private void UpdateFromVoice()
        {
            Utils.Assert(!isSettingKeybinding);

            namePropertyBindable.SetValueRaw(Voice.Name);

            frequencyPropertyBindable.SetValueRaw(Voice.CenterFrequency);

            voiceFrontend.TryFindVoiceKeybinding(Voice, out KeyBinding key);

            KeybindingButton.Text = GetKeybindingUIButtonTextFromKey(key);

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

            for (int index = 0; index < oscillatorsDrawer.Count; index++)
            {
                if (oscillatorsDrawer[index] is VoiceOscillatorControlGroup)
                {
                    oscillatorsDrawer.RemoveChildAt(index);

                    index--;
                }
            }

            string oscillatorsUIXml = GetOscillatorsUIXml();

            uiXmlParser.Parse(oscillatorsUIXml, rootParent: oscillatorsDrawer);

            ForEachOfTypeWithIndex<VoiceOscillatorControlGroup>((index, oscillatorControlGroup) =>
            {
                oscillatorControlGroup.oscillator = Voice.Oscillators[index];

                oscillatorControlGroup.UpdateFromVoice();
            }, start: 0, end: Count);
        }

        public void SetVoiceName(string name)
        {
            SetVoiceNameInternal(name);

            NameTextField.SetTextWithoutProperty(name);
        }

        public void SetFrequency(double frequency)
        {
            SetFrequencyInternal(frequency);

            FrequencyTextField.SetTextWithoutProperty(frequency.ToString());
        }

        private void SetVoiceNameInternal(string name)
        {
            if (Voice is null)
            {
                return;
            }

            Name = $"{Voice.Name}_{Voice.CenterFrequency}Hz_VoiceGroup";

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceName(Voice, name));
        }

        private void SetFrequencyInternal(double frequency)
        {
            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceCenterFrequency(Voice, frequency));
        }

        private void SetAttackInternal(double attack)
        {
            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceAttack(voice, attack));
        }

        private void SetDecayInternal(double decay)
        {
            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceDecay(voice, decay));
        }

        private void SetSustainInternal(double sustain)
        {
            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceSustain(voice, sustain));
        }

        private void SetReleaseInternal(double release)
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

<!--Keybinding-->

    <PlainLabel Position=""(5%, 21.25%)""
                Size=""(25%, 12.5%)"" 
                Text=""Keybinding:"" 
                FitText=""false"" 
                GrowWithText=""true"" 
                Name=""{KeybindingLabelName}""/>
    <TextButton Position=""(28%, 21.25%)"" 
                Size=""(30%, 12.5%)""
                Alignment=""Center""
                Name=""{KeybindingButtonName}""/>

<!--ADSR-->

    <Drawer Position=""(5%, 38.75%)"" 
            Size=""(30%, 12.5%)"" 
            CoverText=""ADSR""
            Name=""{AdsrDrawerName}"">

        <PlainLabel Position=""({drawerBeginX_Percent}%, {drawerBeginY_Percent}%)"" 
                    Size=""(100%, 100%)""
                    Text=""Attack:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{AttackDisplayLabelName}""/>

        <PlainLabel Position=""({drawerBeginX_Percent}%, 300%)"" 
                    Size=""(100%, 100%)""
                    Text=""Decay:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{DecayDisplayLabelName}""/>

        <PlainLabel Position=""({drawerBeginX_Percent}%, 440%)"" 
                    Size=""(100%, 100%)""
                    Text=""Sustain:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{SustainDisplayLabelName}""/>

        <PlainLabel Position=""({drawerBeginX_Percent}%, 580%)"" 
                    Size=""(100%, 100%)""
                    Text=""Release:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{ReleaseDisplayLabelName}""/>

        <TextField Position=""(130%, {drawerBeginY_Percent}%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20"" 
                   NumberMinValue=""{PolyphonicSynthesizer.AttackRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.AttackRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{AttackTextFieldName}""/>

        <TextField Position=""(130%, 300%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20"" 
                   NumberMinValue=""{PolyphonicSynthesizer.DecayRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.DecayRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{DecayTextFieldName}""/>

        <TextField Position=""(130%, 440%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20"" 
                   NumberMinValue=""{PolyphonicSynthesizer.SustainRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.SustainRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{SustainTextFieldName}""/>

        <TextField Position=""(130%, 580%)"" 
                   Size=""(90%, 100%)"" 
                   MaxCharacters=""20"" 
                   NumberMinValue=""{PolyphonicSynthesizer.ReleaseRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.ReleaseRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{ReleaseTextFieldName}""/>

    </Drawer>

    <VoiceMixControlGroup Position=""(5%, 56%)""
                          Size=""(90%, 20%)""/>

    <Drawer Position=""(5%, 81.25%)"" 
            Size=""(30%, 12.5%)"" 
            CoverText=""Oscillators""
            Name=""{OscillatorsDrawerName}"">
            
            <TextButton
                 Position=""(100%, 25%)""
                 Size=""(100%, 75%)""
                 Text=""+""
                 Alignment=""Center""
                 SizeMode=""Min""
                 FitText=""false""
                 Name=""{AddOscillatorButtonName}""/>
    </Drawer>

</Layout>";
        }

        private string GetOscillatorsUIXml()
        {
            if (Voice is null)
            {
                return null;
            }

            float verticalSpacing_Percent = oscillatorsDrawerChildSize_Percent.Y + oscillatorsVerticalDrawerSpacing_Percent;
            float currentPositionY_Percent = drawerBeginY_Percent;

            StringBuilder stringBuilder = new StringBuilder();

            for (int index = 0; index < Voice.Oscillators.Count; index++)
            {
                string oscillatorXml = GetOscillatorUIXml(currentPositionY_Percent);

                stringBuilder.Append(oscillatorXml);
                stringBuilder.AppendLine();

                currentPositionY_Percent += verticalSpacing_Percent;
            }

            return $@"<Layout>

            {stringBuilder.ToString()}

            </Layout>";
        }

        private string GetOscillatorUIXml(float positionY_Percent)
        {
            string oscillatorXml = $@"<VoiceOscillatorControlGroup 
                                           Position=""({drawerBeginX_Percent}%, {positionY_Percent}%)"" 
                                           Size=""({oscillatorsDrawerChildSize_Percent.X}%, {oscillatorsDrawerChildSize_Percent.Y}%)""
                                           Style=""DrawerStyle""/>";

            return oscillatorXml;
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

            //currentOscillatorControlGroups = null;
        }

        private const string NameLabelName = "NameLabel";
        private const string NameTextFieldName = "NameTextField";

        private const string FrequencyDisplayLabelName = "FrequencyDisplayLabel";
        private const string FrequencyTextFieldName = "FrequencyTextField";

        private const string KeybindingLabelName = "KeybindingLabel";
        private const string KeybindingButtonName = "KeybindingButton";

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

        private const string AddOscillatorButtonName = "AddOscillatorButton";

        private const string EMPTY_KEYBINDING_DISPLAY_STRING = "Empty";

        private class VoiceMixControlGroupFactory : UIXmlParser.TypeFactory
        {
            public VoiceMixControlGroupFactory() : base("VoiceMixControlGroup")
            {

            }

            public override Widget Create(Game game, UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new VoiceMixControlGroup(position, size,
                                                game: uiManager.Game);
            }
        }

        private class VoiceOscillatorControlGroupFactory : UIXmlParser.TypeFactory
        {
            public VoiceOscillatorControlGroupFactory() : base("VoiceOscillatorControlGroup")
            {

            }

            public override Widget Create(Game game, UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new VoiceOscillatorControlGroup(position, size,
                                                       uiManager: uiManager);
            }
        }

        private void ActivateKeybindingSetting()
        {
            isSettingKeybinding = true;

            KeybindingButton.AddCaptureListener(uiKeybindingInputSetterListener);

            activatedKeybindingPreviousText = KeybindingButton.Text;

            string newButtonText = "Press a key";

            KeybindingButton.Text = newButtonText;
        }

        private void DeactivateAndSetKeybinding()
        {
            if (pendingKeybindingKeys.IsEmpty)
            {
                SetKeybindingInternal(null);

                KeybindingButton.Text = EMPTY_KEYBINDING_DISPLAY_STRING;

                ResetKeybindingActivation(setButtonPreviousText: false);

                return;
            }

            // For now, making all keys required (KeyComboMode.AND), but I want to implement more options in the future.

            KeyBinding.Key[] keys = pendingKeybindingKeys.ProcessToArray(key => new KeyBinding.Key(key, PressMode.Down, KeyBinding.KeyComboMode.AND));

            KeyBinding keybinding = new KeyBinding(modifiers: null, keys: keys, holdDelay: 0, repeatDelay: 0, respectRepeatDelay: false);

            SetKeybindingInternal(keybinding);

            KeybindingButton.Text = GetKeybindingUIButtonTextFromKey(keybinding);

            ResetKeybindingActivation(setButtonPreviousText: false);
        }

        private void ResetKeybindingActivation(bool setButtonPreviousText)
        {
            activatedBindingIsEmpty = false;

            if (setButtonPreviousText)
            {
                KeybindingButton.Text = activatedKeybindingPreviousText;
            }

            activatedKeybindingPreviousText = null;

            KeybindingButton.RemoveCaptureListener(uiKeybindingInputSetterListener);

            pendingKeybindingKeys.Clear();

            isSettingKeybinding = false;
        }

        private void SetKeybindingInternal(KeyBinding keybinding)
        {
            voiceFrontend.SetVoiceKeybinding(keybinding, Voice);
        }

        private void InitUIKeybindingInputSetterListener()
        {
            uiKeybindingInputSetterListener = new InputListener
            {
                KeyEnter = delegate (InputEvent e, Keys key)
                {
                    e.HandleAndStop();

                    DeactivateAndSetKeybinding();
                },

                KeyDown = delegate (InputEvent e, Keys key)
                {
                    e.HandleAndStop();

                    if (e.Keyboard.IsRepeat)
                    {
                        return;
                    }

                    if (key == Keys.Escape)
                    {
                        ResetKeybindingActivation(setButtonPreviousText: true);

                        return;
                    }

                    if (key == Keys.Back)
                    {
                        pendingKeybindingKeys.Clear();

                        DeactivateAndSetKeybinding();

                        return;
                    }

                    if (invalidKeybindingKeys.Contains(key))
                    {
                        return;
                    }

                    pendingKeybindingKeys.Add(key);
                },

                KeyUp = delegate(InputEvent e, Keys key)
                {
                    e.HandleAndStop();
                },

                MouseDown = delegate (InputEvent e, float x, float y, MouseStates.Button button)
                {
                    e.HandleAndStop();
                },

                MouseUp = delegate (InputEvent e, float x, float y, MouseStates.Button button)
                {
                    e.HandleAndStop();
                },

                MouseDragged = delegate (InputEvent e, float previousX, float previousY, float x, float y, MouseStates.Button button)
                {
                    e.HandleAndStop();
                }
            };
        }

        private static string GetKeybindingUIButtonTextFromKey(KeyBinding keybinding)
        {
            string bindingText;

            if (keybinding is not null)
            {
                bindingText = "Keys: [";

                for (int index = 0; index < keybinding.keys.Length; index++)
                {
                    KeyBinding.Key currentKey = keybinding.keys[index];

                    bindingText += currentKey.key.ToString();

                    if (index + 1 < keybinding.keys.Length)
                    {
                        bindingText += " + ";
                    }
                }

                bindingText += "]";
            }
            else
            {
                bindingText = EMPTY_KEYBINDING_DISPLAY_STRING;
            }

            return bindingText;
        }
    }
}
