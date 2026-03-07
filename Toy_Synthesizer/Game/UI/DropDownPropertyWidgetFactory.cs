using System;

using GeoLib.GeoMaths;

using Toy_Synthesizer.Game.Data;

namespace Toy_Synthesizer.Game.UI
{
    public abstract class DropDownPropertyWidgetFactory
    {
        public abstract DropDownPropertyWidget<Source, ValueType> Create<Source, ValueType>(Property<Source> property,
                                                                         UIManager uiManager,
                                                                         ref Vec2f labelPosition,
                                                                         float labelWidth,
                                                                         Vec2f groupSize,
                                                                         float horizontalSpacing,
                                                                         string name,
                                                                         ValueType[] values,
                                                                         ValueType defaultValue,
                                                                         bool shouldSetImmediately,
                                                                         Func<Source> sourceGetter);
    }
}
