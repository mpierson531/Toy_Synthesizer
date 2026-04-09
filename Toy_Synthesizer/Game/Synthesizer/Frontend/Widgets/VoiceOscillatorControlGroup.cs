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
    // TODO: Implement usage of range in PolyphonicSynthesizer and implement command usage.
    public class VoiceOscillatorControlGroup : GroupWidget
    {
        private VoiceGroup parentVoiceGroup;

        internal Oscillator oscillator;

        private PropertyBindable<double> amplitudeProperty;
        private PropertyBindable<WaveformType> waveformTypeProperty;
        private PropertyBindable<double> detuneCentsProperty;

        private ConvertingPropertyBinding<double, string> amplitudeBinding;
        private ConvertingPropertyBinding<WaveformType, object> waveformTypeBinding;
        private ConvertingPropertyBinding<double, string> detuneCentsBinding;

        private PlainLabel amplitudeLabel;
        private TextField amplitudeTextField;

        private PlainLabel waveformTypeLabel;
        private DropDownListView waveformTypeDropDown;

        private PlainLabel detuneCentsLabel;
        private TextField detuneCentsTextField;

        private Button removeButton;
        private LabelTooltip removeButtonTooltip;

        public VoiceOscillatorControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   positionChildren: false,
                   sizeChildren: false)
        {
            Adapters.Add(new PreciseGroupLayoutAdapter());

            InitPropertyBindables();

            string uiXml = GetUIXml();

            UIXmlParser uiXmlParser = new UIXmlParser(uiManager);

            uiXmlParser.CacheEnumType<WaveformType>();

            uiXmlParser.Parse(uiXml, rootParent: this);

            InitWidgets(uiManager);
        }

        protected override void ParentChanged(GroupWidget previousParent, GroupWidget newParent)
        {
            base.ParentChanged(previousParent, newParent);

            parentVoiceGroup = FindParentVoiceGroup();

            if (newParent is not null && parentVoiceGroup is null)
            {
                throw new InvalidOperationException("No parent voice group found in hierarchy.");
            }
        }

        private VoiceGroup FindParentVoiceGroup()
        {
            GroupWidget voiceGroup = FindFirstAscendant(ascendant => ascendant is VoiceGroup);

            return (VoiceGroup)voiceGroup;
        }

        internal void UpdateFromVoice()
        {
            if (parentVoiceGroup is null || parentVoiceGroup.Voice is null)
            {
                return;
            }

            amplitudeProperty.SetValueRaw(oscillator.Amplitude);
            waveformTypeProperty.SetValueRaw(oscillator.WaveformType);
            detuneCentsProperty.SetValueRaw(oscillator.DetuneCents);

            amplitudeTextField.SetTextWithoutProperty(oscillator.Amplitude.ToString());
            waveformTypeDropDown.SetValueWithoutProperty(oscillator.WaveformType);
            detuneCentsTextField.SetTextWithoutProperty(oscillator.DetuneCents.ToString());
        }

        private void InitPropertyBindables()
        {
            amplitudeProperty = new PropertyBindable<double>("Amplitude");
            waveformTypeProperty = new PropertyBindable<WaveformType>("Waveform Type");
            detuneCentsProperty = new PropertyBindable<double>("Detune Cents");

            amplitudeProperty.OnValueChangedTyped += SetAmplitude;
            waveformTypeProperty.OnValueChangedTyped += SetWaveformType;
            detuneCentsProperty.OnValueChangedTyped += SetDetuneCents;
        }

        private void SetAmplitude(double amplitude)
        {
            DSP dsp = parentVoiceGroup.game.DSP;
            PolyphonicSynthesizer synthesizer = parentVoiceGroup.game.Synthesizer;

            dsp.SendAudioSourceCommand(synthesizer, SynthesizerCommands.SetVoiceOscillatorAmplitude(oscillator, amplitude));
        }

        private void SetWaveformType(WaveformType waveformType)
        {
            DSP dsp = parentVoiceGroup.game.DSP;
            PolyphonicSynthesizer synthesizer = parentVoiceGroup.game.Synthesizer;

            dsp.SendAudioSourceCommand(synthesizer, SynthesizerCommands.SetVoiceOscillatorWaveformType(oscillator, waveformType));
        }

        private void SetDetuneCents(double detuneCents)
        {
            DSP dsp = parentVoiceGroup.game.DSP;
            PolyphonicSynthesizer synthesizer = parentVoiceGroup.game.Synthesizer;

            dsp.SendAudioSourceCommand(synthesizer, SynthesizerCommands.SetVoiceOscillatorDetuneCents(oscillator, detuneCents));
        }

        private void InitWidgets(UIManager uiManager)
        {
            amplitudeLabel = FindAsByNameDeepSearch<PlainLabel>(AmplitudeLabelName);
            amplitudeTextField = FindAsByNameDeepSearch<TextField>(AmplitudeTextFieldName);

            waveformTypeLabel = FindAsByNameDeepSearch<PlainLabel>(WaveformTypeLabelName);
            waveformTypeDropDown = FindAsByNameDeepSearch<DropDownListView>(WaveformTypeDropDownName);

            detuneCentsLabel = FindAsByNameDeepSearch<PlainLabel>(DetuneCentsLabelName);
            detuneCentsTextField = FindAsByNameDeepSearch<TextField>(DetuneCentsTextFieldName);

            removeButton = FindAsByNameDeepSearch<Button>(RemoveButtonName);

            removeButton.OnClick += RemoveButton_OnClick;

            removeButtonTooltip = uiManager.AddTextTooltip(removeButton, "Click to remove this oscillator");

            amplitudeBinding = amplitudeTextField.BindProperty_Number(amplitudeProperty);
            waveformTypeBinding = waveformTypeDropDown.BindProperty(waveformTypeProperty);
            detuneCentsBinding = detuneCentsTextField.BindProperty_Number(detuneCentsProperty);
        }

        private void RemoveButton_OnClick()
        {
            parentVoiceGroup.RemoveOscillatorAndGroup(this);

            if (removeButtonTooltip?.IsShowing ?? false)
            {
                removeButtonTooltip.Hide();
            }
        }

        private string GetUIXml()
        {
            NumberRange<double> amplitudePercentageRange = NumberRangeUtils.ScalarToPercent(PolyphonicSynthesizer.OscillatorAmplitudeRange);
            NumberRange<double> detuneCentsRange = PolyphonicSynthesizer.OscillatorDetuneCentsRange;

            return
            $@"<Layout>

                <TextButton
                 Position=""(5%, 5%)""
                 Size=""(20%, 15%)""
                 Text=""-""
                 Alignment=""Center""
                 SizeMode=""Min""
                 FitText=""false""
                 Name=""{RemoveButtonName}""/>

                <PlainLabel
                 Position=""(5%, 25%)""
                 Size=""(20%, 100%)""
                 Text=""Amplitude:""
                 FitText=""false""
                 GrowWithText=""true""
                 Name=""{AmplitudeLabelName}""/>

        `       <PlainLabel Position=""(5%, 50%)""
                    Size=""(100%, 100%)""
                    Text=""Waveform:""
                    FitText=""false""
                    GrowWithText=""true"" 
                    Name=""{WaveformTypeLabelName}""/>

                <PlainLabel Position=""(5%, 75%)"" 
                    Size=""(100%, 100%)""
                    Text=""Detune Cents:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{DetuneCentsLabelName}""/>

                <TextField Position=""(45%, 25%)"" 
                   Size=""(50%, 20%)"" 
                   MaxCharacters=""20"" 
                   NumberMinValue=""{amplitudePercentageRange.Min}""
                   NumberMaxValue=""{amplitudePercentageRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{AmplitudeTextFieldName}""/>

                <DropDownListView Position=""(45%, 50%)"" 
                   Size=""(50%, 20%)"" 
                   MaxCharacters=""20""
                   DefaultIndex=""0""
                   TypeName=""WaveformType""
                   Name=""{WaveformTypeDropDownName}""/>

                <TextField Position=""(45%, 75%)"" 
                   Size=""(50%, 20%)"" 
                   MaxCharacters=""20"" 
                   NumberMinValue=""{detuneCentsRange.Min}""
                   NumberMaxValue=""{detuneCentsRange.Max}""
                   NumberDefaultValue=""{-1}""
                   TreatAsScalarPercentage=""false""
                   Name=""{DetuneCentsTextFieldName}""/>

            </Layout>";
        }

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            parentVoiceGroup = null;

            oscillator = null;

            amplitudeProperty = null;
            waveformTypeProperty = null;
            detuneCentsProperty = null;

            amplitudeBinding.Dispose();
            waveformTypeBinding.Dispose();
            detuneCentsBinding.Dispose();

            amplitudeBinding = null;
            waveformTypeBinding = null;
            detuneCentsBinding = null;
        }

        private const string AmplitudeLabelName = "AmplitudeLabel";
        private const string AmplitudeTextFieldName = "AmplitudeTextField";

        private const string WaveformTypeLabelName = "WaveformTypeLabel";
        private const string WaveformTypeDropDownName = "WaveformTypeTextField";

        private const string DetuneCentsLabelName = "DetuneCentsLabel";
        private const string DetuneCentsTextFieldName = "DetuneCentsTextField";

        private const string RemoveButtonName = "RemoveButton";
    }
}
