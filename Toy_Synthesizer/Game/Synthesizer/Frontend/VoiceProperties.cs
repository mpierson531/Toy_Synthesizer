using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.UI;
using Toy_Synthesizer.Game.Data;
using Toy_Synthesizer.Game.Data.Generic;
using Toy_Synthesizer.Game.Midi;
using Toy_Synthesizer.Game.Synthesizer.Backend;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend
{
    public class VoiceProperties : IIndexable<Property<Voice>>
    {
        private readonly Property<Voice>[] properties;

        public int Count
        {
            get => properties.Length;
        }

        public Property<Voice> this[int index]
        {
            get => properties[index];
        }

        public VoiceProperties()
        {
            properties = new Property<Voice>[3];

            InitProperties();
        }

        private void InitProperties()
        {
            properties[0] = new Property<Voice, double>
            (
                name: "Center Frequency",

                dataType: PropertyDataType.Float,

                uiData: new PropertyUIData(PropertyWidgetType.TextField),

                getter: (voice) => (float)voice.CenterFrequency,
                setter: (value, voice) => voice.CenterFrequency = value,

                defaultValue: 0,

                range: PropertyRange.NumberRange((float)MidiUtils.GetFrequency(MidiNote.C0), (float)MidiUtils.GetFrequency(MidiNote.G9), 1),

                description: "The center frequency",

                shouldSetImmediately: true,

                isUpdateable: true
            );
        }

        public IEnumerator<Property<Voice>> GetEnumerator()
        {
            return properties.Cast<Property<Voice>>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
