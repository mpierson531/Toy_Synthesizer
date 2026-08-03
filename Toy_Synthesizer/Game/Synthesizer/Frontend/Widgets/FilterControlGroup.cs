using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Data;
using GeoLib.GeoGraphics.UI.Data.Generic;
using GeoLib.GeoGraphics.UI.WidgetAdapters;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.DigitalSignalProcessing.Filters;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    // TODO: Finish implementation
    // TODO: Extend functionality to support setting global synth or DSP filters (not implemented yet)
    // NOTE: This class requires the filter source to not be null; for voices this is voice.Filter, for synth/DSP it is not implemented yet.
    public class FilterControlGroup : GroupWidget
    {
        private Game game;

        //private Voice voice;

        private ISVFProvider filterProvider;

        private SVFType filterType;
        private SVFType previousFilterType;

        private DropDownListView filterTypeDropDown;

        private PlainLabel cutoffDisplayLabel;
        private PlainLabel resonanceDisplayLabel;
        private PlainLabel gainDisplayLabel;
        private PlainLabel lowMixDisplayLabel;
        private PlainLabel highMixDisplayLabel;
        private PlainLabel bandMixDisplayLabel;

        private TextField cutoffTextField;
        private TextField resonanceTextField;
        private TextField gainTextField;
        private TextField lowMixTextField;
        private TextField highMixTextField;
        private TextField bandMixTextField;

        private PropertyBindable<SVFType> filterTypeProperty;
        private PropertyBindable<double> cutoffProperty;
        private PropertyBindable<double> resonanceProperty;
        private PropertyBindable<double> gainProperty;
        private PropertyBindable<double> lowMixProperty;
        private PropertyBindable<double> highMixProperty;
        private PropertyBindable<double> bandMixProperty;

        private ConvertingPropertyBinding<SVFType, object> filterTypeBinding;
        private ConvertingPropertyBinding<double, string> cutoffBinding;
        private ConvertingPropertyBinding<double, string> resonanceBinding;
        private ConvertingPropertyBinding<double, string> gainBinding;
        private ConvertingPropertyBinding<double, string> lowMixBinding;
        private ConvertingPropertyBinding<double, string> highMixBinding;
        private ConvertingPropertyBinding<double, string> bandMixBinding;

        private bool isFirstUIInit = true;

        /*public Voice Voice
        {
            get => voice;
        }*/

        public SVFType FilterType
        {
            get => filterType;
        }

        public FilterControlGroup(Vec2f position, Vec2f size, Game game, SVFType filterType = SVFType.LowPass)
            : base(position, size,
                   style: null,
                   positionChildren: false,
                   sizeChildren: false)
        {
            this.game = game;

            this.filterType = filterType;

            ValidateFilterType();

            InitFilterMixProperties();

            InitCommonProperties();

            Adapters.Add(new PreciseGroupLayoutAdapter());
        }

        public void Init(ISVFProvider filterProvider)
        {
            this.filterProvider = filterProvider;

            ValidateFilterProvider();

            InitUI(setProperties: true, filterMix: null);
        }

        private void SetFilterType(SVFType filterType)
        {
            SetFilterType(filterType, onlySetUI: false);
        }

        private void SetFilterType(SVFType filterType, bool onlySetUI)
        {
            GeoDebug.Assert(filterProvider is not null);

            SVF filter = filterProvider.GetFilter();

            Utils.Assert(filter is not null, "Filter was null.");

            this.previousFilterType = this.filterType;

            this.filterType = filterType;

            ValidateFilterType();

            GetFilterMixFromProviderAndFilterType(out SVFMix newFilterMix);

            if (!onlySetUI)
            {
                Utils.Assert(filter is not null, "Filter was null. This should never be reached!");

                filterProvider.MixProvider.SetMix(newFilterMix);
            }

            // Return early if the UI is already initialized and the UI doesn't need to be changed to accomadate the filter type.
            if (!DoesUINeedInitialized())
            {
                return;
            }

            InitFilterMixProperties();

            if (filter is null)
            {
                return;
            }

            InitUI(setProperties: true, filterMix: newFilterMix);
        }

        /*private void SetFilterType(SVFType filterType, bool onlySetUI)
        {
            this.previousFilterType = this.filterType;

            this.filterType = filterType;

            ValidateFilterType();

            GetFilterMixFromVoiceAndFilterType(out SVFMix newFilterMix);

            if (!onlySetUI)
            {
                Utils.Assert(Voice is not null, "Voice was null. This should never be reached!");

                game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceFilterMix(Voice, newFilterMix));
            }

            // Return early if the UI is already initialized and the UI doesn't need to be changed to accomadate the filter type.
            if (!DoesUINeedInitialized())
            {
                return;
            }

            InitFilterMixProperties();

            if (Voice is null)
            {
                return;
            }

            InitUI(setProperties: true, filterMix: newFilterMix);
        }*/

        private void InitFilterMixProperties()
        {
            if (!DoesUINeedInitialized())
            {
                return;
            }

            if (FilterType == SVFType.Freeform)
            {
                InitLowMixProperty();
                InitHighMixProperty();
                InitBandMixProperty();
            }
            else
            {
                ClearFilterMixProperties();
            }
        }

        private void ClearFilterMixProperties()
        {
            lowMixProperty = null;
            highMixProperty = null;
            bandMixProperty = null;
        }

        private void GetFilterMixFromProviderAndFilterType(out SVFMix filterMix)
        {
            filterMix = FilterType switch
            {
                SVFType.LowPass => SVFMix.LowPass(),
                SVFType.HighPass => SVFMix.HighPass(),
                SVFType.Notch => SVFMix.Notch(),
                SVFType.BandPass => SVFMix.BandPass(),
                SVFType.Freeform => filterProvider.MixProvider.GetMix().GetValueOrDefault(SVFMix.Default),

                _ => throw new InvalidOperationException($"Invalid SVFType: \"{FilterType}\".")
            };
        }

        private bool DoesUINeedInitialized()
        {
            if (isFirstUIInit)
            {
                return true;
            }

            return (previousFilterType == SVFType.Freeform && FilterType != SVFType.Freeform)
                   || (previousFilterType != SVFType.Freeform && FilterType == SVFType.Freeform);
        }

        private void InitUI(bool setProperties, SVFMix? filterMix)
        {
            if (!isFirstUIInit)
            {
                Clear();
            }

            /*if (Voice is null)
            {
                return;
            }*/

            SVF filter = filterProvider?.GetFilter();

            if (filter is null)
            {
                return;
            }

            ValidateFilterType();

            string uiXml = GetUIXmlAndCheckSize();

            UIXmlParser xmlParser = new UIXmlParser(game);
            xmlParser.CacheEnumType<SVFType>();
            xmlParser.AddTypeFactory(new Frontend.SliderDisplayWidgetFactory());

            xmlParser.Parse(uiXml, rootParent: this);

            InitWidgets(setPropertiesAndWidgets: setProperties, bindProperties: true, svfMix: filterMix);

            if (Parent is Drawer drawer && drawer.IsExpanded)
            {
                drawer.RecheckExpansionSize();
            }

            if (Parent is not null)
            {
                Parent.Layout();
            }

            if (isFirstUIInit)
            {
                isFirstUIInit = false;
            }
        }

        private void InitWidgets(bool setPropertiesAndWidgets, bool bindProperties, SVFMix? svfMix)
        {
            filterTypeDropDown = FindAsByNameDeepSearch<DropDownListView>(FilterTypeDropDownName);

            cutoffDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(CutoffDisplayLabelName);
            resonanceDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(ResonanceDisplayLabelName);
            gainDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(GainDisplayLabelName);

            cutoffTextField = FindAsByNameDeepSearch<TextField>(CutoffTextFieldName);
            resonanceTextField = FindAsByNameDeepSearch<TextField>(ResonanceTextFieldName);
            gainTextField = FindAsByNameDeepSearch<TextField>(GainTextFieldName);

            if (bindProperties)
            {
                filterTypeBinding = filterTypeDropDown.BindProperty(filterTypeProperty);

                cutoffBinding = cutoffTextField.BindProperty_Number(cutoffProperty);
                resonanceBinding = resonanceTextField.BindProperty_Number(resonanceProperty);
                gainBinding = gainTextField.BindProperty_Number(gainProperty);
            }

            if (setPropertiesAndWidgets)
            {
                SVF filter = filterProvider.GetFilter();
                double filterBaseCutoff = filterProvider.GetFilterBaseCutoff();

                filterTypeProperty.SetValueRaw(FilterType);

                cutoffProperty.SetValueRaw(filterBaseCutoff);
                resonanceProperty.SetValueRaw(filter.Resonance);
                gainProperty.SetValueRaw(filter.Gain);

                filterTypeDropDown.SetValueWithoutProperty(FilterType);

                cutoffTextField.SetTextWithoutProperty(filterBaseCutoff.ToString());
                resonanceTextField.SetTextWithoutProperty(filter.Resonance.ToString());
                gainTextField.SetTextWithoutProperty(filter.Gain.ToString());
            }

            if (FilterType == SVFType.Freeform)
            {
                lowMixDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(LowMixDisplayLabelName);
                highMixDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(HighMixDisplayLabelName);
                bandMixDisplayLabel = FindAsByNameDeepSearch<PlainLabel>(BandMixTextFieldName);

                lowMixTextField = FindAsByNameDeepSearch<TextField>(LowMixTextFieldName);
                highMixTextField = FindAsByNameDeepSearch<TextField>(HighMixTextFieldName);
                bandMixTextField = FindAsByNameDeepSearch<TextField>(BandMixTextFieldName);

                if (previousFilterType != SVFType.Freeform)
                {
                    (double currentLowMix,
                    double currentHighMix,
                    double currentBandMix) = SVFMix.Default;

                    lowMixBinding = lowMixTextField.BindProperty_Number(lowMixProperty);
                    highMixBinding = highMixTextField.BindProperty_Number(highMixProperty);
                    bandMixBinding = bandMixTextField.BindProperty_Number(bandMixProperty);

                    lowMixProperty.SetValueRaw(currentLowMix);
                    highMixProperty.SetValueRaw(currentHighMix);
                    bandMixProperty.SetValueRaw(currentBandMix);

                    lowMixTextField.SetTextWithoutProperty(currentLowMix.ToString());
                    highMixTextField.SetTextWithoutProperty(currentHighMix.ToString());
                    bandMixTextField.SetTextWithoutProperty(currentBandMix.ToString());
                }
            }
            else
            {
                lowMixDisplayLabel = null;
                lowMixTextField = null;

                highMixDisplayLabel = null;
                highMixTextField = null;

                bandMixDisplayLabel = null;
                bandMixTextField = null;

                lowMixBinding = null;
                highMixBinding = null;
                bandMixBinding = null;
            }
        }

        private void InitCommonProperties()
        {
            filterTypeProperty = new PropertyBindable<SVFType>("Filter Type");

            cutoffProperty = new PropertyBindable<double>("Cutoff Frequency");
            resonanceProperty = new PropertyBindable<double>("Resonance");
            gainProperty = new PropertyBindable<double>("Gain");

            filterTypeProperty.OnValueChangedTyped += SetFilterType;

            cutoffProperty.OnValueChangedTyped += SetCutoff;
            resonanceProperty.OnValueChangedTyped += SetResonance;
            gainProperty.OnValueChangedTyped += SetGain;
        }

        private void InitLowMixProperty()
        {
            if (lowMixProperty is not null)
            {
                return;
            }

            lowMixProperty = new PropertyBindable<double>("Low Pass Mix");

            lowMixProperty.OnValueChangedTyped += SetLowMix;
        }

        private void InitHighMixProperty()
        {
            if (highMixProperty is not null)
            {
                return;
            }

            highMixProperty = new PropertyBindable<double>("High Pass Mix");

            highMixProperty.OnValueChangedTyped += SetHighMix;
        }

        private void InitBandMixProperty()
        {
            if (bandMixProperty is not null)
            {
                return;
            }

            bandMixProperty = new PropertyBindable<double>("Band Pass Mix");

            bandMixProperty.OnValueChangedTyped += SetBandMix;
        }

        private void SetCutoff(double value)
        {
            GeoDebug.Assert(filterProvider is not null);

            filterProvider.SetFilterBaseCutoff(value);
        }

        private void SetResonance(double value)
        {
            GeoDebug.Assert(filterProvider is not null);

            filterProvider.SetFilterResonance(value);
        }

        private void SetGain(double value)
        {
            GeoDebug.Assert(filterProvider is not null);

            filterProvider.SetFilterGain(value);
        }

        private void SetLowMix(double value)
        {
            GeoDebug.Assert(filterProvider is not null && filterProvider.MixProvider is not null);

            if (FilterType != SVFType.Freeform)
            {
                throw new InvalidOperationException("Low mix UI accessed from a non-freeform filter! This shouldn't be reached!");
            }

            filterProvider.MixProvider.SetLowMix(value);
        }

        private void SetHighMix(double value)
        {
            GeoDebug.Assert(filterProvider is not null && filterProvider.MixProvider is not null);

            if (FilterType != SVFType.Freeform)
            {
                throw new InvalidOperationException("High mix UI accessed from a non-freeform filter! This shouldn't be reached!");
            }

            filterProvider.MixProvider.SetHighMix(value);
        }

        private void SetBandMix(double value)
        {
            GeoDebug.Assert(filterProvider is not null && filterProvider.MixProvider is not null);

            if (FilterType != SVFType.Freeform)
            {
                throw new InvalidOperationException("Band mix UI accessed from a non-freeform filter! This shouldn't be reached!");
            }

            filterProvider.MixProvider.SetBandMix(value);
        }

        private void ValidateFilterProvider()
        {
            Utils.Assert(filterProvider is not null, "Filter provider was null.");
            Utils.Assert(filterProvider.MixProvider is not null, "Filter provider's mix provider was null.");
        }

        /*private void SetCutoff(double value)
        {
            GeoDebug.Assert(Voice is not null);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceFilterBaseCutoff(Voice, value));
        }

        private void SetResonance(double value)
        {
            GeoDebug.Assert(Voice is not null);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceFilterResonance(Voice, value));
        }

        private void SetGain(double value)
        {
            GeoDebug.Assert(Voice is not null);

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceFilterGain(Voice, value));
        }

        private void SetLowMix(double value)
        {
            GeoDebug.Assert(Voice is not null);

            if (FilterType != SVFType.Freeform)
            {
                throw new InvalidOperationException("Low mix UI accessed from a non-freeform filter! This shouldn't be reached!");
            }

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceFilterMixLow(Voice, value));
        }

        private void SetHighMix(double value)
        {
            GeoDebug.Assert(Voice is not null);

            if (FilterType != SVFType.Freeform)
            {
                throw new InvalidOperationException("High mix UI accessed from a non-freeform filter! This shouldn't be reached!");
            }

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceFilterMixHigh(Voice, value));
        }

        private void SetBandMix(double value)
        {
            GeoDebug.Assert(Voice is not null);

            if (FilterType != SVFType.Freeform)
            {
                throw new InvalidOperationException("Band mix UI accessed from a non-freeform filter! This shouldn't be reached!");
            }

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceFilterMixBand(Voice, value));
        }

        private void ValidateVoice()
        {
            GeoDebug.Assert(Voice is not null);

            Utils.Assert(Voice.Filter is not null, "Voice filter was null.");
        }

        private void ValidateFilter()
        {
            GeoDebug.Assert(Filter is not null);

            Utils.Assert(Filter is not null, "Filter was null.");
        }*/

        private void ValidateFilterType()
        {
            switch (FilterType)
            {
                case SVFType.LowPass:
                case SVFType.HighPass:
                case SVFType.BandPass:
                case SVFType.Notch:
                case SVFType.Freeform:
                    break;

                default: throw new InvalidOperationException($"Invalid SVFType: \"{FilterType}\".");
            }
        }

        private string GetUIXmlAndCheckSize()
        {
            SVF filter = filterProvider.GetFilter();
            double filterBaseCutoff = filterProvider.GetFilterBaseCutoff();

            if (FilterType != SVFType.Freeform)
            {
                if (previousFilterType == SVFType.Freeform)
                {
                    PreciseGroupLayoutAdapter parentPreciseLayoutAdapter = Parent?.Adapters.FindFirstOfType<PreciseGroupLayoutAdapter>();

                    if (parentPreciseLayoutAdapter is not null)
                    {
                        parentPreciseLayoutAdapter.TryGetNormalizedBounds(this, out AABB newBounds);
                        newBounds.Size.Y *= 0.57142f;

                        parentPreciseLayoutAdapter.TrySetNormalizedBounds(this, newBounds);
                    }
                    else
                    {
                        DisableLayout();

                        Size *= new Vec2f(1f, 0.57142f);

                        EnableLayout();
                    }
                }

                return "<Layout>" +
               $@"
        <DropDownListView Position=""(0%, 0%)"" 
                   Size=""(50%, 20%)"" 
                   MaxCharacters=""20""
                   DefaultIndex=""0""
                   TypeName=""SVFType""
                   Name=""{FilterTypeDropDownName}""/>

        <PlainLabel Position=""(0%, 25%)"" 
                    Size=""(100%, 100%)""
                    Text=""Cutoff:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{CutoffDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 50%)"" 
                    Size=""(100%, 100%)""
                    Text=""Resonance:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{ResonanceDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 75%)"" 
                    Size=""(100%, 100%)""
                    Text=""Gain:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{GainDisplayLabelName}""/>

        <TextField Position=""(50%, 25%)""
                   Size=""(50%, 20%)"" 
                   MaxCharacters=""20"" 
                   NumberAllowedSign=""1""
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_BaseCutoffRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_BaseCutoffRange.Max}""
                   NumberDefaultValue=""{filterBaseCutoff}""
                   TreatAsScalarPercentage=""false""
                   Name=""{CutoffTextFieldName}""/>

        <TextField Position=""(50%, 50%)"" 
                   Size=""(50%, 20%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1"" 
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_ResonanceRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_ResonanceRange.Max}""
                   NumberDefaultValue=""{filter.Resonance}""
                   TreatAsScalarPercentage=""false""
                   Name=""{ResonanceTextFieldName}""/>

        <TextField Position=""(50%, 75%)"" 
                   Size=""(50%, 20%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1""  
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_GainRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_GainRange.Max}""
                   NumberDefaultValue=""{filter.Gain}""
                   TreatAsScalarPercentage=""false""
                   Name=""{GainTextFieldName}""/>
        </Layout>";
            }

            (double currentLowMix,
            double currentHighMix,
            double currentBandMix) = SVFMix.Default;

            if (previousFilterType != SVFType.Freeform)
            {
                PreciseGroupLayoutAdapter parentPreciseLayoutAdapter = Parent?.Adapters.FindFirstOfType<PreciseGroupLayoutAdapter>();

                if (parentPreciseLayoutAdapter is not null)
                {
                    parentPreciseLayoutAdapter.TryGetNormalizedBounds(this, out AABB newBounds);
                    newBounds.Size.Y *= 1.75f;

                    parentPreciseLayoutAdapter.TrySetNormalizedBounds(this, newBounds);
                }
                else
                {
                    DisableLayout();

                    Size *= new Vec2f(1f, 1.75f);

                    EnableLayout();
                }
            }

            return "<Layout>" +
           $@"

        <DropDownListView Position=""(0%, 0%)"" 
                   Size=""(50%, 11.4284%)"" 
                   MaxCharacters=""20""
                   DefaultIndex=""0""
                   TypeName=""SVFType""
                   Name=""{FilterTypeDropDownName}""/>

        <PlainLabel Position=""(0%, 14.2855%)"" 
                    Size=""(100%, 100%)""
                    Text=""Cutoff:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{CutoffDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 28.571%)"" 
                    Size=""(100%, 100%)""
                    Text=""Resonance:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{ResonanceDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 42.8565%)"" 
                    Size=""(100%, 100%)""
                    Text=""Gain:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{GainDisplayLabelName}""/>

        <TextField Position=""(50%, 14.2855%)""
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20"" 
                   NumberAllowedSign=""1""
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_BaseCutoffRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_BaseCutoffRange.Max}""
                   NumberDefaultValue=""{filterBaseCutoff}""
                   TreatAsScalarPercentage=""false""
                   Name=""{CutoffTextFieldName}""/>

        <TextField Position=""(50%, 28.571%)"" 
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1"" 
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_ResonanceRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_ResonanceRange.Max}""
                   NumberDefaultValue=""{filter.Resonance}""
                   TreatAsScalarPercentage=""false""
                   Name=""{ResonanceTextFieldName}""/>

        <TextField Position=""(50%, 42.8565%)"" 
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1""  
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_GainRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_GainRange.Max}""
                   NumberDefaultValue=""{filter.Gain}""
                   TreatAsScalarPercentage=""false""
                   Name=""{GainTextFieldName}""/>"

    + $@"<PlainLabel Position=""(0%, 57.142%)"" 
                    Size=""(100%, 100%)""
                    Text=""Low Pass Mix:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{LowMixDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 71.4275%)"" 
                    Size=""(100%, 100%)""
                    Text=""High Pass Mix:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{HighMixDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 85.713%)"" 
                    Size=""(100%, 100%)""
                    Text=""Band Pass Mix:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{BandMixDisplayLabelName}""/>

        <TextField Position=""(50%, 57.142%)""
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20"" 
                   NumberAllowedSign=""1""
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_MixRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_MixRange.Max}""
                   NumberDefaultValue=""{currentLowMix}""
                   TreatAsScalarPercentage=""false""
                   Name=""{LowMixTextFieldName}""/>

        <TextField Position=""(50%, 71.4275%)""
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1"" 
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_MixRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_MixRange.Max}""
                   NumberDefaultValue=""{currentHighMix}""
                   TreatAsScalarPercentage=""false""
                   Name=""{HighMixTextFieldName}""/>

        <TextField Position=""(50%, 85.713%)"" 
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1""  
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_MixRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_MixRange.Max}""
                   NumberDefaultValue=""{currentBandMix}""
                   TreatAsScalarPercentage=""false""
                   Name=""{BandMixTextFieldName}""/>" + "</Layout>";
        }

        /*private string GetUIXmlAndCheckSize()
        {
            if (FilterType != SVFType.Freeform)
            {
                if (previousFilterType == SVFType.Freeform)
                {
                    PreciseGroupLayoutAdapter parentPreciseLayoutAdapter = Parent?.Adapters.FindFirstOfType<PreciseGroupLayoutAdapter>();

                    if (parentPreciseLayoutAdapter is not null)
                    {                        
                        parentPreciseLayoutAdapter.TryGetNormalizedBounds(this, out AABB newBounds);
                        newBounds.Size.Y *= 0.57142f;

                        parentPreciseLayoutAdapter.TrySetNormalizedBounds(this, newBounds);
                    }
                    else
                    {
                        DisableLayout();

                        Size *= new Vec2f(1f, 0.57142f);

                        EnableLayout();
                    }
                }

                return "<Layout>" +
               $@"
        <DropDownListView Position=""(0%, 0%)"" 
                   Size=""(50%, 20%)"" 
                   MaxCharacters=""20""
                   DefaultIndex=""0""
                   TypeName=""SVFType""
                   Name=""{FilterTypeDropDownName}""/>

        <PlainLabel Position=""(0%, 25%)"" 
                    Size=""(100%, 100%)""
                    Text=""Cutoff:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{CutoffDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 50%)"" 
                    Size=""(100%, 100%)""
                    Text=""Resonance:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{ResonanceDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 75%)"" 
                    Size=""(100%, 100%)""
                    Text=""Gain:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{GainDisplayLabelName}""/>

        <TextField Position=""(50%, 25%)""
                   Size=""(50%, 20%)"" 
                   MaxCharacters=""20"" 
                   NumberAllowedSign=""1""
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_BaseCutoffRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_BaseCutoffRange.Max}""
                   NumberDefaultValue=""{Voice.Filter_BaseCutoff}""
                   TreatAsScalarPercentage=""false""
                   Name=""{CutoffTextFieldName}""/>

        <TextField Position=""(50%, 50%)"" 
                   Size=""(50%, 20%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1"" 
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_ResonanceRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_ResonanceRange.Max}""
                   NumberDefaultValue=""{Voice.Filter.Resonance}""
                   TreatAsScalarPercentage=""false""
                   Name=""{ResonanceTextFieldName}""/>

        <TextField Position=""(50%, 75%)"" 
                   Size=""(50%, 20%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1""  
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_GainRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_GainRange.Max}""
                   NumberDefaultValue=""{Voice.Filter.Gain}""
                   TreatAsScalarPercentage=""false""
                   Name=""{GainTextFieldName}""/>
        </Layout>";
            }

            SVFMix.DeconstructOrDefault(Voice.FilterMix, out double currentLowMix, out double currentHighMix, out double currentBandMix);

            if (previousFilterType != SVFType.Freeform)
            {
                PreciseGroupLayoutAdapter parentPreciseLayoutAdapter = Parent?.Adapters.FindFirstOfType<PreciseGroupLayoutAdapter>();

                if (parentPreciseLayoutAdapter is not null)
                {
                    parentPreciseLayoutAdapter.TryGetNormalizedBounds(this, out AABB newBounds);
                    newBounds.Size.Y *= 1.75f;

                    parentPreciseLayoutAdapter.TrySetNormalizedBounds(this, newBounds);
                }
                else
                {
                    DisableLayout();

                    Size *= new Vec2f(1f, 1.75f);

                    EnableLayout();
                }
            }

            return "<Layout>" +
           $@"

        <DropDownListView Position=""(0%, 0%)"" 
                   Size=""(50%, 11.4284%)"" 
                   MaxCharacters=""20""
                   DefaultIndex=""0""
                   TypeName=""SVFType""
                   Name=""{FilterTypeDropDownName}""/>

        <PlainLabel Position=""(0%, 14.2855%)"" 
                    Size=""(100%, 100%)""
                    Text=""Cutoff:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{CutoffDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 28.571%)"" 
                    Size=""(100%, 100%)""
                    Text=""Resonance:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{ResonanceDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 42.8565%)"" 
                    Size=""(100%, 100%)""
                    Text=""Gain:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{GainDisplayLabelName}""/>

        <TextField Position=""(50%, 14.2855%)""
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20"" 
                   NumberAllowedSign=""1""
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_BaseCutoffRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_BaseCutoffRange.Max}""
                   NumberDefaultValue=""{Voice.Filter_BaseCutoff}""
                   TreatAsScalarPercentage=""false""
                   Name=""{CutoffTextFieldName}""/>

        <TextField Position=""(50%, 28.571%)"" 
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1"" 
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_ResonanceRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_ResonanceRange.Max}""
                   NumberDefaultValue=""{Voice.Filter.Resonance}""
                   TreatAsScalarPercentage=""false""
                   Name=""{ResonanceTextFieldName}""/>

        <TextField Position=""(50%, 42.8565%)"" 
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1""  
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_GainRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_GainRange.Max}""
                   NumberDefaultValue=""{Voice.Filter.Gain}""
                   TreatAsScalarPercentage=""false""
                   Name=""{GainTextFieldName}""/>"

    + $@"<PlainLabel Position=""(0%, 57.142%)"" 
                    Size=""(100%, 100%)""
                    Text=""Low Pass Mix:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{LowMixDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 71.4275%)"" 
                    Size=""(100%, 100%)""
                    Text=""High Pass Mix:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{HighMixDisplayLabelName}""/>

        <PlainLabel Position=""(0%, 85.713%)"" 
                    Size=""(100%, 100%)""
                    Text=""Band Pass Mix:"" 
                    FitText=""false"" 
                    GrowWithText=""true"" 
                    Name=""{BandMixDisplayLabelName}""/>

        <TextField Position=""(50%, 57.142%)""
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20"" 
                   NumberAllowedSign=""1""
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_MixRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_MixRange.Max}""
                   NumberDefaultValue=""{currentLowMix}""
                   TreatAsScalarPercentage=""false""
                   Name=""{LowMixTextFieldName}""/>

        <TextField Position=""(50%, 71.4275%)""
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1"" 
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_MixRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_MixRange.Max}""
                   NumberDefaultValue=""{currentHighMix}""
                   TreatAsScalarPercentage=""false""
                   Name=""{HighMixTextFieldName}""/>

        <TextField Position=""(50%, 85.713%)"" 
                   Size=""(50%, 11.4284%)""
                   MaxCharacters=""20""
                   NumberAllowedSign=""1""  
                   NumberMinValue=""{PolyphonicSynthesizer.Filter_MixRange.Min}""
                   NumberMaxValue=""{PolyphonicSynthesizer.Filter_MixRange.Max}""
                   NumberDefaultValue=""{currentBandMix}""
                   TreatAsScalarPercentage=""false""
                   Name=""{BandMixTextFieldName}""/>" + "</Layout>";
        }*/

        private const string FilterTypeDropDownName = "FilterTypeDropDown";
        private const string CutoffDisplayLabelName = "CutoffDisplayLabel";
        private const string ResonanceDisplayLabelName = "ResonanceDisplayLabel";
        private const string GainDisplayLabelName = "GainDisplayLabel";
        private const string LowMixDisplayLabelName = "LowMixDisplayLabel";
        private const string HighMixDisplayLabelName = "HighMixDisplayLabel";
        private const string BandMixDisplayLabelName = "BandMixDisplayLabel";

        private const string CutoffTextFieldName = "CutoffTextField";
        private const string ResonanceTextFieldName = "ResonanceTextField";
        private const string GainTextFieldName = "GainTextField";
        private const string LowMixTextFieldName = "LowMixTextField";
        private const string HighMixTextFieldName = "HighMixTextField";
        private const string BandMixTextFieldName = "BandMixTextField";

        protected override void DisableInternal()
        {
            base.DisableInternal();

            lowMixProperty = null;
            highMixProperty = null;
            bandMixProperty = null;
            cutoffProperty = null;
            resonanceProperty = null;
            gainProperty = null;

            lowMixBinding.Dispose();
            highMixBinding.Dispose();
            bandMixBinding.Dispose();
            cutoffBinding.Dispose();
            resonanceBinding.Dispose();
            gainBinding.Dispose();

            lowMixBinding = null;
            highMixBinding = null;
            bandMixBinding = null;
            cutoffBinding = null;
            resonanceBinding = null;
            gainBinding = null;
        }

        public enum SVFType
        {
            LowPass,
            HighPass,
            BandPass,
            Notch,
            Freeform
        }

        public class FilterControlGroupFactory : UIXmlParser.TypeFactory
        {
            public FilterControlGroupFactory() : base("FilterControlGroup") { }

            public override Widget Create(Game game, UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                UIXmlParser.TryGetEnum(attributes, "filtertype", out SVFType filterType);

                return new FilterControlGroup(position, size, game, filterType);
            }
        }
    }
}