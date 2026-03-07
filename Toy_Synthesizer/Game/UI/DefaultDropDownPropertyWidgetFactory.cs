using System;

using GeoLib.GeoMaths;

using Toy_Synthesizer.Game.Data;
using Toy_Synthesizer.Game.Data.Generic;

namespace Toy_Synthesizer.Game.UI
{
    public sealed class DefaultDropDownPropertyWidgetFactory : DropDownPropertyWidgetFactory
    {
        public sealed override DropDownPropertyWidget<Source, ValueType> Create<Source, ValueType>(Property<Source> property,
                                                                         UIManager uiManager,
                                                                         ref Vec2f labelPosition,
                                                                         float labelWidth,
                                                                         Vec2f groupSize,
                                                                         float horizontalSpacing,
                                                                         string name,
                                                                         ValueType[] values,
                                                                         ValueType defaultValue,
                                                                         bool shouldSetImmediately,
                                                                         Func<Source> sourceGetter)
        {
            return new DropDownPropertyWidget<Source, ValueType>((Property<Source, ValueType>)property,
                                                                 uiManager,
                                                                 ref labelPosition,
                                                                 labelWidth,
                                                                 groupSize,
                                                                 horizontalSpacing,
                                                                 name,
                                                                 values, defaultValue,
                                                                 shouldSetImmediately,
                                                                 sourceGetter);
        }
    }
}
