using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using GeoLib.GeoGraphics.UI;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.WidgetFactories
{
    public sealed class OscillatorControlGroupFactory : UIXmlParser.TypeFactory
    {
        public OscillatorControlGroupFactory() : base("VoiceOscillatorControlGroup")
        {

        }

        public override Widget Create(Game game, UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            return new OscillatorControlGroup(game.DSP, game.Synthesizer,
                                              position, size,
                                              uiManager: uiManager);
        }
    }
}
