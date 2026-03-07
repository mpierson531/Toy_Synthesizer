using System;

namespace Toy_Synthesizer.Game.Data
{
    // Use this for Property<Source>(s) that have an incompatible PropertyDataType and PropertyWidgetType
    public class WrongPropertyUIMappingException : Exception
    {
        public WrongPropertyUIMappingException(PropertyDataType dataType, PropertyWidgetType widgetType)
            : base($"PropertyDataType \"{dataType}\" is incompatible with PropertyWidgetType \"{widgetType}\".")
        {

        }
    }
}
