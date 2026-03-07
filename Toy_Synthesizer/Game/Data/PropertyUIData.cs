using GeoLib.GeoUtils;

namespace Toy_Synthesizer.Game.Data
{
    public struct PropertyUIData
    {
        public const bool DEFAULT_IS_TOGGLEABLE = false;
        public const int DEFAULT_TEXTFIELD_MAX_CHARACTERS = int.MaxValue;
        public const Alignment DEFAULT_LABEL_ALIGNMENT = Alignment.Center;
        public const bool DEFAULT_USE_PLAINLABEL_FOR_LABEL = false;
        public const bool DEFAULT_ADD_TOOLTIP_TO_CONTROL = false;

        public readonly PropertyWidgetType WidgetType;
        public readonly bool IsToggleable;
        public readonly int TextFieldMaxCharacters;
        public readonly Alignment LabelAlignment;
        public readonly bool UsePlainLabelForLabel;
        public readonly bool AddTooltipToControl; // If true, adds a tooltip to the control itself, rather than just the label of the property widget.

        public PropertyUIData(PropertyWidgetType widgetType,
                              bool isToggleable = DEFAULT_IS_TOGGLEABLE,
                              int textFieldMaxCharacters = DEFAULT_TEXTFIELD_MAX_CHARACTERS,
                              Alignment labelAlignment = DEFAULT_LABEL_ALIGNMENT,
                              bool usePlainLabelForLabel = DEFAULT_USE_PLAINLABEL_FOR_LABEL,
                              bool addTooltipToControl = DEFAULT_ADD_TOOLTIP_TO_CONTROL)
        {
            WidgetType = widgetType;
            IsToggleable = isToggleable;
            TextFieldMaxCharacters = textFieldMaxCharacters;
            LabelAlignment = labelAlignment;
            UsePlainLabelForLabel = usePlainLabelForLabel;
            AddTooltipToControl = addTooltipToControl;
        }
    }
}
