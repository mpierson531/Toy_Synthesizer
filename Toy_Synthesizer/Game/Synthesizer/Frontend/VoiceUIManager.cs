using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.DigitalSignalProcessing;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend
{
    public class VoiceUIManager
    {
        private readonly Game game;

        private readonly PolyphonicSynthesizer synthesizer;

        private readonly UIXmlParser uiXmlParser;

        private GroupWidget voicesGroup;

        public VoiceUIManager(Game game, UIXmlParser uiXmlParser)
        {
            this.game = game;

            this.synthesizer = game.Synthesizer;

            this.uiXmlParser = uiXmlParser;
        }

        public void InitUI()
        {
            string xml = @"
<Layout>

    <Window Title=""Voices"" X=""50"" Y=""50"" W=""400"" H=""600"">

        <ScrollPane X=""0%"" Y=""0%"" W=""100%"" H=""95%""/>

    </Window>

</Layout>";


            ViewableList<Widget> widgets = uiXmlParser.Parse(xml);

            Window window = (Window)widgets[0];

            voicesGroup = (GroupWidget)window[window.ComputeEffectiveChildBeginOffset()];

            float currentY = voicesGroup.Position.Y;

            AudioSourceCommand forEachVoiceCommand = SynthesizerCommands.ForEachVoiceAction(delegate (Voice voice)
            {
                InitVoiceGroup(voice, ref currentY, offsetYFirst: false);
            });

            synthesizer.SendCommand(ref forEachVoiceCommand);

            game.AddUIWidgets(widgets);

            synthesizer.OnVoiceAdded += delegate (PolyphonicSynthesizer polyphonic, Voice voice)
            {
                float currentY = voicesGroup[voicesGroup.Count - 1].Position.Y;

                InitVoiceGroup(voice, ref currentY, offsetYFirst: true);
            };
        }

        private void InitVoiceGroup(Voice voice, ref float currentY, bool offsetYFirst)
        {
            float scrollPaneGroupSpacing = voicesGroup.Size.Min() * 0.02f;

            float groupX = voicesGroup.Position.X + scrollPaneGroupSpacing;
            float groupY = currentY + scrollPaneGroupSpacing;
            float groupW = voicesGroup.Size.X * 0.925f;
            float groupH = voicesGroup.Size.Y * 0.35f;

            if (offsetYFirst)
            {
                currentY += groupH + voicesGroup.Size.Y * 0.1f;
            }

            string xml = "<Layout>" + Environment.NewLine;

            xml +=
            $@"
                    <VoiceGroup X=""{groupX}"" Y=""{groupY}"" W=""{groupW}"" H=""{groupH}""/>
            ";

            currentY += groupH + voicesGroup.Size.Y * 0.05f;

            xml += Environment.NewLine + "</Layout>";

            ViewableList<Widget> voiceGroups = uiXmlParser.Parse(xml);

            voiceGroups.ForEach(voiceGroup =>
            {
                ((VoiceGroup)voiceGroup).Voice = voice;
            });

            voicesGroup.AddChildRange(voiceGroups);
        }
    }
}
