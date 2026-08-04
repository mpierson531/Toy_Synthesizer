using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Data;
using GeoLib.GeoGraphics.UI.WidgetAdapters;
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
    public class OscillatorControlGroup : GroupWidget
    {
        private DSP dsp;
        private PolyphonicSynthesizer synthesizer;

        private Oscillator oscillator;

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

        public Oscillator Oscillator
        {
            get => oscillator;
        }

        public event Action<OscillatorControlGroup> RemoveButton_OnClick;

        public OscillatorControlGroup(DSP dsp, PolyphonicSynthesizer synthesizer, Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   positionChildren: false,
                   sizeChildren: false)
        {
            InitDSPAndSynthesizer(dsp, synthesizer);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            InitPropertyBindables();

            string uiXml = GetUIXml();

            UIXmlParser uiXmlParser = new UIXmlParser(uiManager.Game);

            uiXmlParser.CacheEnumType<WaveformType>();

            uiXmlParser.Parse(uiXml, rootParent: this);

            InitWidgets(uiManager);
        }

        public void InitDSPAndSynthesizer(DSP dsp, PolyphonicSynthesizer synthesizer)
        {
            this.dsp = dsp;
            this.synthesizer = synthesizer;
        }

        // This sets the oscillator and updates the UI from it.
        public void SetOscillator(Oscillator oscillator)
        {
            this.oscillator = oscillator;

            UpdateFromOscillator();
        }

        private void UpdateFromOscillator()
        {
            amplitudeProperty.SetValueRaw(Oscillator.Amplitude);
            waveformTypeProperty.SetValueRaw(Oscillator.WaveformType);
            detuneCentsProperty.SetValueRaw(Oscillator.DetuneCents);

            amplitudeTextField.SetTextWithoutProperty(Oscillator.Amplitude.ToString());
            waveformTypeDropDown.SetValueWithoutProperty(Oscillator.WaveformType);
            detuneCentsTextField.SetTextWithoutProperty(Oscillator.DetuneCents.ToString());
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
            dsp.SendAudioSourceCommand(synthesizer, SynthesizerCommands.SetVoiceOscillatorAmplitude(Oscillator, amplitude));
        }

        private void SetWaveformType(WaveformType waveformType)
        {
            dsp.SendAudioSourceCommand(synthesizer, SynthesizerCommands.SetVoiceOscillatorWaveformType(Oscillator, waveformType));
        }

        private void SetDetuneCents(double detuneCents)
        {
            dsp.SendAudioSourceCommand(synthesizer, SynthesizerCommands.SetVoiceOscillatorDetuneCents(Oscillator, detuneCents));
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

            removeButton.OnClick += RemoveButton_OnClick_Internal;

            removeButtonTooltip = uiManager.AddTextTooltip(removeButton, "Click to remove this oscillator");

            amplitudeBinding = amplitudeTextField.BindProperty_Number(amplitudeProperty);
            waveformTypeBinding = waveformTypeDropDown.BindProperty(waveformTypeProperty);
            detuneCentsBinding = detuneCentsTextField.BindProperty_Number(detuneCentsProperty);
        }

        private void RemoveButton_OnClick_Internal()
        {
            RemoveButton_OnClick?.Invoke(this);

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

            RemoveButton_OnClick = null;
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
