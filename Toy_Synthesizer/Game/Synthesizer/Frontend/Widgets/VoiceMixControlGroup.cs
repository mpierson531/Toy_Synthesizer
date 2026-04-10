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
    public class VoiceMixControlGroup : GroupWidget
    {
        private Game game;

        private VoiceGroup parentVoiceGroup;

        private SliderDisplayWidget sliderDisplayWidget;

        private PropertyBindable<double> propertyBindable;

        public VoiceMixControlGroup(Vec2f position, Vec2f size, Game game)
            : base(position, size,
                   style: game.UIManager.GetDefaultGroupStyle(),
                   positionChildren: false,
                   sizeChildren: false)
        {
            this.game = game;

            propertyBindable = new PropertyBindable<double>("Voice Mix");

            propertyBindable.OnValueChangedTyped += SetVoiceMixRaw;

            Style.RenderData.SetColor(game.UIManager.BackgroundedLabelTint);

            Adapters.Add(new PreciseGroupLayoutAdapter());

            string uiXml = GetUIXml();

            UIXmlParser xmlParser = new UIXmlParser(game);
            xmlParser.AddTypeFactory(new Frontend.SliderDisplayWidgetFactory());

            xmlParser.Parse(uiXml, rootParent: this);

            InitWidgets();
        }

        protected override void ParentChanged(GroupWidget previousParent, GroupWidget newParent)
        {
            base.ParentChanged(previousParent, newParent);

            if (parentVoiceGroup is not null)
            {
                parentVoiceGroup.OnVoiceChanged -= ParentVoiceGroup_OnVoiceChanged;
            }

            Utils.Assert(newParent is VoiceGroup || newParent is null, "A VoiceMixControlGroup's parent must be a VoiceGroup.");

            parentVoiceGroup = (VoiceGroup)newParent;

            if (parentVoiceGroup is not null)
            {
                parentVoiceGroup.OnVoiceChanged += ParentVoiceGroup_OnVoiceChanged;
            }
        }

        private void InitWidgets()
        {
            sliderDisplayWidget = FindAsByNameDeepSearch<SliderDisplayWidget>(SLIDER_DISPLAY_WIDGET_NAME);

            sliderDisplayWidget.BindProperty(propertyBindable);
        }

        private void ParentVoiceGroup_OnVoiceChanged(VoiceGroup _, Voice previousVoice, Voice newVoice)
        {
            sliderDisplayWidget.SetWidgetValues(GetCurrentVoiceMix(), updateProperty: false);
        }

        private double GetCurrentVoiceMix()
        {
            Voice voice = GetCurrentVoice();

            if (voice is null)
            {
                return 0.0;
            }

            return voice.Mix;
        }

        private void SetVoiceMixRaw(double value)
        {
            Voice voice = GetCurrentVoice();

            if (voice is null)
            {
                return;
            }

            game.DSP.SendAudioSourceCommand(game.Synthesizer, SynthesizerCommands.SetVoiceMix(voice, value));
        }

        private Voice GetCurrentVoice()
        {
            return parentVoiceGroup?.Voice;
        }

        private string GetUIXml()
        {
            return
            $@"<Layout> 
                <SliderDisplayWidget 
                Position=""(0%, 0%)"" 
                Size=""(100%, 100%)"" 
                NumberMinValue=""{PolyphonicSynthesizer.MixRange.Min}""
                NumberMaxValue=""{PolyphonicSynthesizer.MixRange.Max}""
                NumberDefaultValue=""{PolyphonicSynthesizer.DEFAULT_MIX}""
                TreatAsScalarPercentage=""true"" 
                PropertyName=""Mix""
                Name=""{SLIDER_DISPLAY_WIDGET_NAME}""/> 
               </Layout>";
        }

        private const string SLIDER_DISPLAY_WIDGET_NAME = "SliderDisplayWidget";
    }
}