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
    // TODO: When adding/removing oscillators and keybindings, figure out why laying out doesn't work right unless I explicitly Layout (the drawer widgets should be making it work)
    public class VoiceGroup : ScrollPane
    {
        internal Game game;

        internal VoiceFrontend voiceFrontend;

        private KeyBinding keybinding;

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

        private Drawer oscillatorsDrawer;
        private Button addOscillatorButton;
        private LabelTooltip addOscillatorsButtonTooltip;

        private readonly ViewableList<KeybindingGroupWidgets> keybindingGroupWidgets;

        private Drawer keybindingsDrawer;
        private Button addKeybindingButton;
        private LabelTooltip addKeybindingButtonTooltip;

        private readonly float drawerBeginX_Percent = 20f;
        private readonly float drawerBeginY_Percent = 160f;
        private readonly float oscillatorsVerticalDrawerSpacing_Percent = 50f;
        private readonly float keybindingsVerticalDrawerSpacing_Percent = 50f;
        private readonly Vec2f oscillatorsDrawerChildSize_Percent = new Vec2f(240f, 500f);
        private readonly Vec2f keybindingsDrawerChildSize_Percent = new Vec2f(240f, 150f);

        // keybindingsDrawerChildSize_Percent.X * 0.15
        private readonly float keybindingsKeyComboModeWidth_Percent = 240f * 0.175f;

        // keybindingsDrawerChildSize_Percent.Y * 0.75
        private readonly float keybindingsKeyComboModeHeight_Percent = 112.5f;

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

            keybindingGroupWidgets = new ViewableList<KeybindingGroupWidgets>();

            InitPropertyBindables();

            //InitUIKeybindingInputSetterListener();

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
            uiXmlParser.AddTypeFactory(new VoiceKeybindingGroupFactory());

            string uiXml = GetUIXml();

            uiXmlParser.Parse(uiXml, rootParent: this);

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

            oscillatorsDrawer = FindAsByNameDeepSearch<Drawer>(OscillatorsDrawerName);

            addOscillatorButton = FindAsByNameDeepSearch<Button>(AddOscillatorButtonName);

            addOscillatorButton.OnClick += AddOscillatorButton_OnClick;

            addOscillatorsButtonTooltip = uiManager.AddTextTooltip(addOscillatorButton, "Click to add an oscillator");

            keybindingsDrawer = FindAsByNameDeepSearch<Drawer>(KeybindingsDrawerName);

            addKeybindingButton = FindAsByNameDeepSearch<Button>(AddKeybindingButtonName);

            addKeybindingButton.OnClick += AddKeybindingButton_OnClick;

            addKeybindingButtonTooltip = uiManager.AddTextTooltip(addKeybindingButton, "Click to add a keybinding");

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

            if (addOscillatorsButtonTooltip?.IsShowing ?? false)
            {
                addOscillatorsButtonTooltip.Hide();
            }

            Oscillator oscillator = PolyphonicSynthesizer.CreateDefaultOscillator(Voice.CenterFrequency);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.AddVoiceOscillator(Voice, oscillator));

            string uiXml = GetOscillatorUIXml(drawerBeginY_Percent);

            VoiceOscillatorControlGroup newOscillatorWidget = (VoiceOscillatorControlGroup)uiXmlParser.Parse(uiXml,
                                                                                                             rootParentBaseBounds: new AABB(oscillatorsDrawer.Position, oscillatorsDrawer.PreExpansionSize))[0];

            // May have to manually pass spacing for the drawer here, not sure yet.

            UIManager.ShiftDrawer_ChildAdded(oscillatorsDrawer, newOscillatorWidget);

            newOscillatorWidget.oscillator = oscillator;

            oscillatorsDrawer.AddChild(newOscillatorWidget);

            Layout();

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

            Layout();
        }

        private void AddKeybindingButton_OnClick()
        {
            // TODO: Add animation/velocity-based scrolling to the position of the new keybinding widget

            if (addKeybindingButtonTooltip?.IsShowing ?? false)
            {
                addKeybindingButtonTooltip.Hide();
            }

            string keybindingXml = GetKeybindingUIXml(drawerBeginY_Percent);

            VoiceKeybindingGroup newKeybindingWidget = (VoiceKeybindingGroup)uiXmlParser.Parse(keybindingXml,
                                                                                               rootParentBaseBounds: new AABB(keybindingsDrawer.Position, keybindingsDrawer.PreExpansionSize))[0];
            
            Vec2f drawerSpacing_Scalar = new Vec2f(GeoMath.PercentToScalar(keybindingsDrawerChildSize_Percent + keybindingsVerticalDrawerSpacing_Percent));
            Vec2f keyComboModeSpacing_Scalar = new Vec2f(drawerSpacing_Scalar.X, GeoMath.PercentToScalar(keybindingsKeyComboModeHeight_Percent + keybindingsVerticalDrawerSpacing_Percent));

            Vec2f drawerSpacing = keybindingsDrawer.PreExpansionSize * new Vec2f(0f, drawerSpacing_Scalar.Y);
            Vec2f keyComboModeSpacing = keybindingsDrawer.PreExpansionSize * new Vec2f(0f, keyComboModeSpacing_Scalar.Y);

            UIManager.ShiftDrawer_ChildAdded(keybindingsDrawer, newKeybindingWidget, shiftDelta: drawerSpacing);

            newKeybindingWidget.Key = new KeyBinding.Key(key: Keys.None, mode: PressMode.Down, KeyBinding.KeyComboMode.AND);
            newKeybindingWidget.keybindingIndex = 0;

            AddKeyAtBeginningRaw(newKeybindingWidget);

            KeybindingGroupWidgets keybindingGroupWidgets = new KeybindingGroupWidgets
            {
                Group = newKeybindingWidget,
                KeyComboModeButton = null,
            };

            keybindingsDrawer.AddChild(newKeybindingWidget);

            // Only add another key combo mode button if there would be more than 1 VoiceKeybindingGroup.
            if (!this.keybindingGroupWidgets.IsEmpty)
            {
                string keyComboModeXml = GetKeyComboModeButtonXml(drawerBeginY_Percent + keybindingsDrawerChildSize_Percent.Y + keybindingsVerticalDrawerSpacing_Percent);

                TextButton keyComboModeButton = (TextButton)uiXmlParser.Parse(keyComboModeXml,
                                                                              rootParentBaseBounds: new AABB(keybindingsDrawer.Position, keybindingsDrawer.PreExpansionSize))[0];

                UIManager.ShiftDrawer_ChildAdded(keybindingsDrawer, keyComboModeButton, shiftDelta: keyComboModeSpacing);

                keybindingGroupWidgets.KeyComboModeButton = keyComboModeButton;
                keybindingGroupWidgets.KeyComboMode = KeyBinding.KeyComboMode.AND;

                keyComboModeButton.OnClick += GetKeyComboModeButton_OnClick(keybindingGroupWidgets);
            }

            this.keybindingGroupWidgets.Insert(0, keybindingGroupWidgets);

            SyncKeybindingGroupsKeybindingIndex(startIndex: 1);

            if (keybindingGroupWidgets.KeyComboModeButton is not null)
            {
                keybindingsDrawer.AddChild(keybindingGroupWidgets.KeyComboModeButton);
            }

            Layout();

            newKeybindingWidget.UpdateFromVoice();
        }

        internal void RemoveKeyFromKeybinding(VoiceKeybindingGroup keybindingGroup)
        {
            if (!keybindingsDrawer.Contains(keybindingGroup))
            {
                throw new InvalidOperationException("Oscillator does not exist.");
            }

            TextButton keyComboModeButton = this.keybindingGroupWidgets[keybindingGroup.keybindingIndex].KeyComboModeButton;

            this.keybindingGroupWidgets.RemoveAt(keybindingGroup.keybindingIndex);

            SyncKeybindingGroupsKeybindingIndex(startIndex: 0);

            keybinding.RemoveKeyAt(keybindingGroup.keybindingIndex);

            UIManager.ShiftDrawer_ChildRemoved(keybindingsDrawer, keybindingGroup);

            keybindingsDrawer.RemoveChild(keybindingGroup);

            if (keyComboModeButton is not null)
            {
                UIManager.ShiftDrawer_ChildRemoved(keybindingsDrawer, keyComboModeButton);

                keybindingsDrawer.RemoveChild(keyComboModeButton);
            }

            // Remove the previous group's key combo mode button if applicable.
            if (keybindingGroup.keybindingIndex > 1)
            {
                KeybindingGroupWidgets previousKeybindingGroupWidgets = this.keybindingGroupWidgets[keybindingGroup.keybindingIndex - 1];

                GeoDebug.Assert(previousKeybindingGroupWidgets is not null && previousKeybindingGroupWidgets.KeyComboModeButton is not null);

                UIManager.ShiftDrawer_ChildRemoved(keybindingsDrawer, previousKeybindingGroupWidgets.KeyComboModeButton);

                keybindingsDrawer.RemoveChild(previousKeybindingGroupWidgets.KeyComboModeButton);
            }

            Layout();
        }

        internal void SetKey(VoiceKeybindingGroup group)
        {
            keybinding.SetKey(group.keybindingIndex, group.Key);
        }

        private void AddKeyAtBeginningRaw(VoiceKeybindingGroup group)
        {
            keybinding.AddKey(group.Key, atBeginning: true);
        }

        private void SyncKeybindingGroupsKeybindingIndex(int startIndex)
        {
            for (int index = startIndex; index < keybindingGroupWidgets.Count; index++)
            {
                keybindingGroupWidgets[index].Group.keybindingIndex = index;
            }
        }

        private Action GetKeyComboModeButton_OnClick(KeybindingGroupWidgets keybindingGroupWidgets)
        {
            return () => SwitchKeyComboMode(keybindingGroupWidgets);
        }

        private void SwitchKeyComboMode(KeybindingGroupWidgets keybindingGroupWidgets)
        {
            keybindingGroupWidgets.KeyComboMode = keybindingGroupWidgets.KeyComboMode switch
            {
                KeyBinding.KeyComboMode.AND => KeyBinding.KeyComboMode.OR,
                KeyBinding.KeyComboMode.OR => KeyBinding.KeyComboMode.AND,

                _ => throw new InvalidOperationException($"Invalid KeyComboMode: \"{keybindingGroupWidgets.KeyComboMode}\".")
            };

            SetKeyComboModeText(keybindingGroupWidgets);

            keybindingGroupWidgets.Group.Key = keybindingGroupWidgets.Group.Key.Copy(comboMode: keybindingGroupWidgets.KeyComboMode);

            SetKey(keybindingGroupWidgets.Group);
        }

        private static void SetKeyComboModeText(KeybindingGroupWidgets keybindingGroupWidgets)
        {
            keybindingGroupWidgets.KeyComboModeButton.Text = keybindingGroupWidgets.KeyComboMode.ToString();
        }

        private void UpdateFromVoice()
        {
            voiceFrontend.TryFindVoiceKeybinding(Voice, out keybinding);

            namePropertyBindable.SetValueRaw(Voice.Name);

            frequencyPropertyBindable.SetValueRaw(Voice.CenterFrequency);

            voiceFrontend.TryFindVoiceKeybinding(Voice, out KeyBinding key);

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

            InitOscillatorGroups();

            InitKeybindingGroups();
        }

        private void InitOscillatorGroups()
        {
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

        private void InitKeybindingGroups()
        {
            for (int index = 0; index < keybindingsDrawer.Count; index++)
            {
                if (keybindingsDrawer[index] is VoiceKeybindingGroup || (keybindingsDrawer[index].Name?.Equals(KeyComboModeButtonsNameTag) ?? false))
                {
                    keybindingsDrawer.RemoveChildAt(index);

                    index--;
                }
            }

            keybindingGroupWidgets.Clear();

            string keybindingsUIXml = GetKeybindingsUIXml();

            uiXmlParser.Parse(keybindingsUIXml, rootParent: keybindingsDrawer);

            ReadOnlySpan<KeyBinding.Key> keybindingKeysSpan = keybinding.KeysSpan;

            for (int index = Drawer.DRAWER_CONTENT_BEGIN_INDEX, keybindingIndex = 0; index < keybindingsDrawer.Count; index++)
            {
                if (keybindingsDrawer[index] is VoiceKeybindingGroup voiceKeybindingGroup)
                {
                    KeybindingGroupWidgets keybindingGroupWidgets = new KeybindingGroupWidgets
                    {
                        Group = voiceKeybindingGroup,
                        KeyComboModeButton = null,
                    };

                    voiceKeybindingGroup.Key = keybindingKeysSpan[keybindingIndex];
                    voiceKeybindingGroup.keybindingIndex = keybindingIndex;

                    voiceKeybindingGroup.UpdateFromVoice();

                    if (index + 1 < keybindingsDrawer.Count)
                    {
                        // Because of pattern matching and var declaration, doing this kind of weirdly.

                        Utils.Assert(keybindingsDrawer[index + 1] is TextButton keyComboModeButton && keyComboModeButton.Name.Equals(KeyComboModeButtonsNameTag));

                        keyComboModeButton = (TextButton)keybindingsDrawer[index + 1];

                        GeoDebug.Assert(keyComboModeButton is not null);

                        keybindingGroupWidgets.KeyComboModeButton = keyComboModeButton;

                        keybindingGroupWidgets.KeyComboMode = keybindingKeysSpan[keybindingIndex].KeyComboMode;

                        keyComboModeButton.Text = keybindingGroupWidgets.KeyComboMode.ToString();

                        keyComboModeButton.OnClick += GetKeyComboModeButton_OnClick(keybindingGroupWidgets);
                    }

                    this.keybindingGroupWidgets.Insert(keybindingIndex, keybindingGroupWidgets);

                    keybindingIndex++;
                }
            }
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

<!--ADSR-->

    <Drawer Position=""(5%, 21.25%)"" 
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

    <VoiceMixControlGroup Position=""(5%, 38.75%)""
                          Size=""(90%, 20%)""/>

    <Drawer Position=""(5%, 60%)"" 
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

    <Drawer Position=""(5%, 81.25%)"" 
            Size=""(30%, 12.5%)"" 
            CoverText=""Keybindings""
            RenderTextPosition=""(-2.5%, 0%)""
            Name=""{KeybindingsDrawerName}"">
            
            <TextButton
                 Position=""(105%, 25%)""
                 Size=""(100%, 75%)""
                 Text=""+""
                 Alignment=""Center""
                 SizeMode=""Min""
                 FitText=""false""
                 Name=""{AddKeybindingButtonName}""/>
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

        private string GetKeybindingsUIXml()
        {
            if (Voice is null)
            {
                return null;
            }

            float verticalSpacing_Percent = keybindingsDrawerChildSize_Percent.Y + keybindingsVerticalDrawerSpacing_Percent;
            float keyComboModeButtonVerticalSpacing_Percent = keybindingsKeyComboModeHeight_Percent + keybindingsVerticalDrawerSpacing_Percent;
            float currentPositionY_Percent = drawerBeginY_Percent;

            StringBuilder stringBuilder = new StringBuilder();

            ReadOnlySpan<KeyBinding.Key> keybindingKeysSpan = keybinding.KeysSpan;

            for (int index = 0; index < keybindingKeysSpan.Length; index++)
            {
                string keybindingXml = GetKeybindingUIXml(currentPositionY_Percent);

                stringBuilder.Append(keybindingXml);
                stringBuilder.AppendLine();

                currentPositionY_Percent += verticalSpacing_Percent;

                if (index + 1 < keybindingKeysSpan.Length)
                {
                    string keyComboModeButtonXml = GetKeyComboModeButtonXml(currentPositionY_Percent);

                    stringBuilder.Append(keyComboModeButtonXml);
                    stringBuilder.AppendLine();

                    currentPositionY_Percent += keyComboModeButtonVerticalSpacing_Percent;
                }
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

        private string GetKeybindingUIXml(float positionY_Percent)
        {
            string keybindingXml = $@"<VoiceKeybindingGroup 
                                           Position=""({drawerBeginX_Percent}%, {positionY_Percent}%)"" 
                                           Size=""({keybindingsDrawerChildSize_Percent.X}%, {keybindingsDrawerChildSize_Percent.Y}%)""
                                           Style=""DrawerStyle""/>";

            return keybindingXml;
        }

        private string GetKeyComboModeButtonXml(float positionY_Percent)
        {
            // Default KeyComboMode is AND

            float x_Percent = (drawerBeginX_Percent + keybindingsDrawerChildSize_Percent.X * 0.5f) - (keybindingsKeyComboModeWidth_Percent * 0.5f);

            string buttonXml = $@"<TextButton
                                   Position=""({x_Percent}%, {positionY_Percent}%)"" 
                                   Size=""({keybindingsKeyComboModeWidth_Percent}%, {keybindingsKeyComboModeHeight_Percent}%)""
                                   Text=""AND""
                                   Alignment=""Center""
                                   FitText=""false""
                                   Name=""{KeyComboModeButtonsNameTag}""/>";

            return buttonXml;
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
        private const string AddOscillatorButtonName = "AddOscillatorButton";

        private const string KeybindingsDrawerName = "KeybindingsDrawer";
        private const string AddKeybindingButtonName = "AddKeybindingButton";

        public const string EMPTY_KEYBINDING_DISPLAY_STRING = "Empty";

        private const string KeyComboModeButtonsNameTag = "KeyComboModeButton";

        private sealed class VoiceMixControlGroupFactory : UIXmlParser.TypeFactory
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

        private sealed class VoiceOscillatorControlGroupFactory : UIXmlParser.TypeFactory
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

        private sealed class VoiceKeybindingGroupFactory : UIXmlParser.TypeFactory
        {
            public VoiceKeybindingGroupFactory() : base("VoiceKeybindingGroup")
            {

            }

            public override Widget Create(Game game, UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                return new VoiceKeybindingGroup(position, size,
                                                uiManager: uiManager);
            }
        }

        public static VoiceGroup FindParentVoiceGroup(Widget widget)
        {
            GroupWidget voiceGroup = widget.FindFirstAscendant(ascendant => ascendant is VoiceGroup);

            return (VoiceGroup)voiceGroup;
        }

        private sealed class KeybindingGroupWidgets
        {
            public VoiceKeybindingGroup Group;
            public TextButton KeyComboModeButton;
            public KeyBinding.KeyComboMode KeyComboMode;
        }
    }
}
