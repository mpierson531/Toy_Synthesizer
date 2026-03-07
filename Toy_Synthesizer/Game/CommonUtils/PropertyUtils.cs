using System;

using Microsoft.Xna.Framework;

using FontStashSharp;
using GeoLib.GeoMaths;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;
using Toy_Synthesizer.Game.UI;
using Toy_Synthesizer.Game.Data;
using Toy_Synthesizer.Game.Data.Generic;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.CommonUtils
{
    public static class PropertyUtils
    {
        private static readonly DefaultDropDownPropertyWidgetFactory defaultDropDownPropertyWidgetFactory = new DefaultDropDownPropertyWidgetFactory();

        public delegate FloatSpinnerPropertyWidget<Source> FloatSpinnerGenerator<Source>(Property<Source, float> property,
                                                                                         bool shouldInitialize,
                                                                                         UIManager uiManager,
                                                                                         ref Vec2f labelPosition,
                                                                                         float labelWidth,
                                                                                         Vec2f groupSize,
                                                                                         float horizontalSpacing,
                                                                                         string name,
                                                                                         PropertyRange range,
                                                                                         Action onEnter,
                                                                                         bool shouldSetImmediately,
                                                                                         Func<Source> sourceGetter);

        public delegate CheckboxPropertyWidget<Source> CheckboxGenerator<Source>(Property<Source, bool> property,
                                                                                         UIManager uiManager,
                                                                                         ref Vec2f labelPosition,
                                                                                         float labelWidth,
                                                                                         Vec2f groupSize,
                                                                                         float horizontalSpacing,
                                                                                         string name,
                                                                                         bool shouldSetImmediately,
                                                                                         Func<Source> sourceGetter);

        public delegate SliderPropertyWidget<Source> SliderGenerator<Source>(Property<Source, float> property,
                                                                                         UIManager uiManager,
                                                                                         ref Vec2f labelPosition,
                                                                                         float labelWidth,
                                                                                         Vec2f groupSize,
                                                                                         float horizontalSpacing,
                                                                                         string name,
                                                                                         PropertyRange range,
                                                                                         bool shouldSetImmediately,
                                                                                         Func<Source> sourceGetter);

        public delegate LabelPropertyWidget<Source> LabelGenerator<Source>(Property<Source, string> property,
                                                                                         UIManager uiManager,
                                                                                         ref Vec2f labelPosition,
                                                                                         float labelWidth,
                                                                                         Vec2f groupSize,
                                                                                         float horizontalSpacing,
                                                                                         string name,
                                                                                         bool shouldSetImmediately,
                                                                                         Func<Source> sourceGetter);

        public delegate TextFieldPropertyWidget<Source> TextFieldGenerator<Source>(Property<Source, string> property,
                                                                                         UIManager uiManager,
                                                                                         ref Vec2f labelPosition,
                                                                                         float labelWidth,
                                                                                         Vec2f groupSize,
                                                                                         float horizontalSpacing,
                                                                                         string name,
                                                                                         Action onEnter,
                                                                                         bool shouldSetImmediately,
                                                                                         Func<Source> sourceGetter);

        public static string[] CollectDescriptions<T, E>(T[] properties) where T : Property<E>
        {
            if (properties.Length == 0)
            {
                return Array.Empty<string>();
            }

            ViewableList<string> descriptions = new ViewableList<string>(properties.Length);

            for (int index = 0; index != properties.Length; index++)
            {
                T property = properties[index];

                if (TextUtils.IsNullEmptyOrWhitespace(property.Description))
                {
                    continue;
                }

                descriptions.Add(property.Description);
            }

            return descriptions.ToArray();
        }

        // Runs a switch on the PropertyDataType of the property, casts the PropertyWidget to the appropriate intermediate type (PropertyWidget<Source, ValueType), and sets it.
        // This is done to avoid boxing of structs
        public static void SetWidgetValue<Source>(ref readonly Source source, PropertyWidget<Source> widget, Property<Source> property)
        {
            switch (property.DataType)
            {
                case PropertyDataType.Float:
                    {
                        PropertyWidget<Source, float> typedWidget = (PropertyWidget<Source, float>)widget;
                        Property<Source, float> typedProperty = (Property<Source, float>)property;

                        typedWidget.SetWidgetValue(typedProperty.GetValue(source));

                        break;
                    }

                case PropertyDataType.Bool:
                    {
                        PropertyWidget<Source, bool> typedWidget = (PropertyWidget<Source, bool>)widget;
                        Property<Source, bool> typedProperty = (Property<Source, bool>)property;

                        typedWidget.SetWidgetValue(typedProperty.GetValue(source));

                        break;
                    }

                case PropertyDataType.Color:
                    {
                        PropertyWidget<Source, Color> typedWidget = (PropertyWidget<Source, Color>)widget;
                        Property<Source, Color> typedProperty = (Property<Source, Color>)property;

                        typedWidget.SetWidgetValue(typedProperty.GetValue(source));

                        break;
                    }

                case PropertyDataType.String:
                    {
                        PropertyWidget<Source, string> typedWidget = (PropertyWidget<Source, string>)widget;
                        Property<Source, string> typedProperty = (Property<Source, string>)property;

                        typedWidget.SetWidgetValue(typedProperty.GetValue(source));

                        break;
                    }
            }
        }

        // Will return true if any value was actually changed. I.E. if any value before calling this was different from the corresponding value in the UI.
        public static bool SetSourceFromUI<Source>(Source source, GroupWidget propertiesGroup,
                                                   int startIndex = 0)
        {
            bool anyChanged = false;

            Action<PropertyWidget<Source>> propertyWidgetSetSourceValue = delegate (PropertyWidget<Source> propertyWidget)
            {
                if (!anyChanged)
                {
                    if (propertyWidget.SetSourceValue(source))
                    {
                        anyChanged = true;
                    }
                    // Can't guarantee that it wasn't changed.
                    // Return true so that any config saving that is dependant on returning true saves correctly.
                    else if (propertyWidget.IsPropertySetImmediately)
                    {
                        anyChanged = true;
                    }
                }
                else
                {
                    propertyWidget.SetSourceValue(source);
                }

                if (propertyWidget is IResettableInputWidget) // This is mostly for the shape property widgets
                {
                    ((IResettableInputWidget)propertyWidget).ResetInput();
                }
            };

            propertiesGroup.ForEachOfType(propertyWidgetSetSourceValue, startIndex, propertiesGroup.Count);

            return anyChanged;
        }

        // Will return true if any value was actually changed.
        // I.E. if any value before calling this was different from the corresponding value in the UI.
        public static bool ResetSourceAndUI<Source>(Source source, GroupWidget propertiesGroup)
        {
            bool anyChanged = false;

            void PropertyWidgetReset(PropertyWidget<Source> propertyWidget)
            {
                if (!anyChanged)
                {
                    if (propertyWidget.ResetSourceAndUI(source))
                    {
                        anyChanged = true;
                    }
                    // Can't guarantee that it wasn't changed.
                    // Return true so that any config saving that is dependant on returning true saves correctly.
                    else if (propertyWidget.IsPropertySetImmediately)
                    {
                        anyChanged = true;
                    }
                }
                else
                {
                    propertyWidget.ResetSourceAndUI(source);
                }

                if (propertyWidget is IResettableInputWidget) // This is mostly for the shape property widgets
                {
                    ((IResettableInputWidget)propertyWidget).ResetInput();
                }
            }

            propertiesGroup.ForEachOfType<PropertyWidget<Source>>(PropertyWidgetReset);

            return anyChanged;
        }

        public static void SyncUIWithSource<Source>(Source source, GroupWidget propertiesGroup)
        {
            propertiesGroup.ForEachOfType<PropertyWidget<Source>>(widget => widget.SyncUIWithSource(source));
        }

        // Simply sets the UI to the default value, and only the UI
        public static void ResetUI<Source>(GroupWidget propertiesGroup)
        {
            static void PropertyWidgetReset(PropertyWidget<Source> propertyWidget)
            {
                propertyWidget.ResetUI();
            }

            propertiesGroup.ForEachOfType<PropertyWidget<Source>>(PropertyWidgetReset);
        }

        // Will return true if any value was actually changed. I.E. if any value before calling this was different from the value that is set when the Property is reset.
        public static bool ResetProperties<Source>(Source source, ref readonly Property<Source>[] properties)
        {
            bool anyChanged = false;

            for (int index = 0; index != properties.Length; index++)
            {
                ref readonly Property<Source> property = ref properties[index];

                if (!anyChanged)
                {
                    if (property.Reset(source))
                    {
                        anyChanged = true;
                    }
                    // Can't guarantee that it wasn't changed.
                    // Return true so that any config saving that is dependant on returning true saves correctly.
                    else if (property.ShouldSetImmediately)
                    {
                        anyChanged = true;
                    }
                }
                else
                {
                    property.Reset(source);
                }
            }

            return anyChanged;
        }

        public static void CreateWidgets<Source>(IIndexable<Property<Source>> properties, UIManager uiManager, GroupWidget group,
                                            ref Vec2f widgetPosition, Vec2f groupSize, float horizontalSpacing, float verticalSpacing,
                                            Action onEnter,
                                            Func<Source> sourceGetter,
                                            FloatSpinnerGenerator<Source> floatSpinnerGenerator = null,
                                            CheckboxGenerator<Source> checkboxGenerator = null,
                                            SliderGenerator<Source> sliderGenerator = null,
                                            DropDownPropertyWidgetFactory dropDownFactory = null,
                                            LabelGenerator<Source> labelGenerator = null,
                                            TextFieldGenerator<Source> textFieldGenerator = null) where Source : class
        {
            CreateWidgets<Source>(properties, 0, properties.Count, uiManager, group, ref widgetPosition, groupSize, horizontalSpacing, verticalSpacing, onEnter, sourceGetter,
                                  floatSpinnerGenerator,
                                  checkboxGenerator,
                                  sliderGenerator,
                                  dropDownFactory,
                                  labelGenerator,
                                  textFieldGenerator);
        }

        public static void CreateWidgets<Source>(IIndexable<Property<Source>> properties, int start, int end, UIManager uiManager, GroupWidget group,
                                            ref Vec2f widgetPosition, Vec2f groupSize, float horizontalSpacing, float verticalSpacing,
                                            Action onEnter,
                                            Func<Source> sourceGetter,
                                            FloatSpinnerGenerator<Source> floatSpinnerGenerator = null,
                                            CheckboxGenerator<Source> checkboxGenerator = null,
                                            SliderGenerator<Source> sliderGenerator = null,
                                            DropDownPropertyWidgetFactory dropDownFactory = null,
                                            LabelGenerator<Source> labelGenerator = null,
                                            TextFieldGenerator<Source> textFieldGenerator = null) where Source : class
        {
            floatSpinnerGenerator ??= GetDefaultFloatSpinnerGenerator<Source>();
            checkboxGenerator ??= GetDefaultCheckboxGenerator<Source>();
            sliderGenerator ??= GetDefaultSliderGenerator<Source>();
            dropDownFactory ??= defaultDropDownPropertyWidgetFactory;
            labelGenerator ??= GetDefaultLabelGenerator<Source>();
            textFieldGenerator ??= GetDefaultTextFieldGenerator<Source>();

            float labelWidth = FindLargestPropertyNameWidth(properties, uiManager.MainFont);

            Vec2f position = widgetPosition;

            for (int index = start; index < end; index++)
            {
                Property<Source> property = properties[index];

                if (!property.IsUserfacing)
                {
                    continue;
                }

                CreateWidget(floatSpinnerGenerator, checkboxGenerator, sliderGenerator, dropDownFactory, labelGenerator, textFieldGenerator,
                             sourceGetter, property, group, uiManager, position, labelWidth, groupSize, horizontalSpacing, onEnter);

                position.Y += verticalSpacing;
            }
        }

        // sourceGetter is used for getting the current value of the source and setting it in the PropertyWidget.
        // sourceGetter is also given to the PropertyWidget implementation if it supports it. If so, it will be used for when the property should be set immediately as the value is changed.
        // onEnter is only used for a SpinnerPropertyWidget
        public static void CreateWidget<Source>(FloatSpinnerGenerator<Source> floatSpinnerGenerator,
                                                CheckboxGenerator<Source> checkboxGenerator,
                                                SliderGenerator<Source> sliderGenerator,
                                                DropDownPropertyWidgetFactory dropDownFactory,
                                                LabelGenerator<Source> labelGenerator,
                                                TextFieldGenerator<Source> textFieldGenerator,
                                                Func<Source> sourceGetter, Property<Source> property,
                                                GroupWidget group, UIManager uiManager,
                                                Vec2f labelPosition, float labelWidth, Vec2f groupSize, float horizontalSpacing,
                                                Action onEnter) where Source : class
        {
            ref readonly PropertyUIData uiData = ref property.UIData;

            ValidateUIMapping(property.DataType, in uiData);

            Source source = sourceGetter?.Invoke();

            switch (uiData.WidgetType)
            {
                case PropertyWidgetType.Spinner:
                    {
                        Property<Source, float> typedProperty = (Property<Source, float>)property;

                        FloatSpinnerPropertyWidget<Source> spinnerWidget = floatSpinnerGenerator(typedProperty, true, uiManager, ref labelPosition, labelWidth, groupSize,
                                                                                        horizontalSpacing,
                                                                                        property.Name, property.Range.Value,
                                                                                        onEnter,
                                                                                        shouldSetImmediately: property.ShouldSetImmediately,
                                                                                        sourceGetter: sourceGetter);

                        group.AddChild(spinnerWidget);

                        if (source is not null)
                        {
                            spinnerWidget.SetWidgetValue(typedProperty.GetValue(source));
                        }

                        break;
                    }

                case PropertyWidgetType.Checkbox:
                    {
                        Property<Source, bool> typedProperty = (Property<Source, bool>)property;

                        CheckboxPropertyWidget<Source> checkboxWidget = checkboxGenerator(typedProperty, uiManager, ref labelPosition, labelWidth, groupSize,
                                                                                           horizontalSpacing,
                                                                                           property.Name,
                                                                                           shouldSetImmediately: property.ShouldSetImmediately,
                                                                                           sourceGetter: sourceGetter);

                        group.AddChild(checkboxWidget);

                        if (source is not null)
                        {
                            checkboxWidget.SetWidgetValue(typedProperty.GetValue(source));
                        }

                        break;
                    }

                case PropertyWidgetType.Slider:
                    {
                        Property<Source, float> typedProperty = (Property<Source, float>)property;

                        SliderPropertyWidget<Source> sliderWidget = sliderGenerator(typedProperty, uiManager, ref labelPosition, labelWidth, groupSize,
                                                                                     horizontalSpacing,
                                                                                     property.Name, property.Range.Value,
                                                                                     shouldSetImmediately: property.ShouldSetImmediately,
                                                                                     sourceGetter: sourceGetter);

                        group.AddChild(sliderWidget);

                        // Keep things in sync
                        sliderWidget.CurrentValue = ((Property<Source, float>)property).GetValue(source);
                        sliderWidget.Precision = 0;

                        if (source is not null)
                        {
                            sliderWidget.SetWidgetValue(typedProperty.GetValue(source));
                        }

                        break;
                    }

                case PropertyWidgetType.DropDown: // Only used for BuiltinColor right now. Come back and handle other types at some point.
                    {
                        CreateDropDown(source,
                                             dropDownFactory,
                                             sourceGetter,
                                             property,
                                             group,
                                             uiManager,
                                             labelPosition,
                                             labelWidth,
                                             groupSize,
                                             horizontalSpacing);

                        break;
                    }

                case PropertyWidgetType.Label:
                    {
                        Property<Source, string> typedProperty = (Property<Source, string>)property;

                        LabelPropertyWidget<Source> widget = labelGenerator(typedProperty,
                                                                                             uiManager,
                                                                                             ref labelPosition,
                                                                                             labelWidth,
                                                                                             groupSize,
                                                                                             horizontalSpacing,
                                                                                             property.Name,
                                                                                             shouldSetImmediately: property.ShouldSetImmediately,
                                                                                             sourceGetter: sourceGetter);

                        group.AddChild(widget);

                        if (source is not null)
                        {
                            widget.SetWidgetValue(typedProperty.GetValue(source));
                        }

                        break;
                    }

                case PropertyWidgetType.TextField:
                    {
                        Property<Source, string> typedProperty = (Property<Source, string>)property;

                        TextFieldPropertyWidget<Source> widget = textFieldGenerator(typedProperty,
                                                                                             uiManager,
                                                                                             ref labelPosition,
                                                                                             labelWidth,
                                                                                             groupSize,
                                                                                             horizontalSpacing,
                                                                                             property.Name,
                                                                                             onEnter,
                                                                                             shouldSetImmediately: property.ShouldSetImmediately,
                                                                                             sourceGetter: sourceGetter);

                        group.AddChild(widget);

                        if (source is not null)
                        {
                            widget.SetWidgetValue(typedProperty.GetValue(source));
                        }

                        break;
                    }

                default: throw new InvalidOperationException("Invalid PropertyWidgetType: " + property.UIData.WidgetType.ToString());
            }
        }

        public static void CreateDropDown<Source>(Source source,
                                                DropDownPropertyWidgetFactory dropDownFactory,
                                                Func<Source> sourceGetter, Property<Source> property,
                                                GroupWidget group, UIManager uiManager,
                                                Vec2f labelPosition, float labelWidth, Vec2f groupSize, float horizontalSpacing) where Source : class
        {
            switch (property.DataType)
            {
                case PropertyDataType.Color:
                    {
                        CreateDropDownInternal<Source, Color>(source,
                                                 dropDownFactory,
                                                 sourceGetter,
                                                 property,
                                                 group,
                                                 uiManager,
                                                 labelPosition,
                                                 labelWidth,
                                                 groupSize,
                                                 horizontalSpacing);

                        break;
                    }

                case PropertyDataType.EnumInt:
                    {
                        CreateDropDownInternal<Source, int>(source,
                                                 dropDownFactory,
                                                 sourceGetter,
                                                 property,
                                                 group,
                                                 uiManager,
                                                 labelPosition,
                                                 labelWidth,
                                                 groupSize,
                                                 horizontalSpacing);

                        break;
                    }

                default: throw new InvalidOperationException("This should never reached! Invalid type for a drop down widget: " + property.DataType.ToString());
            }
        }

        private static void CreateDropDownInternal<Source, ValueType>(Source source,
                                                DropDownPropertyWidgetFactory dropDownFactory,
                                                Func<Source> sourceGetter, Property<Source> property,
                                                GroupWidget group, UIManager uiManager,
                                                Vec2f labelPosition, float labelWidth, Vec2f groupSize, float horizontalSpacing) where Source : class
        {
            Property<Source, ValueType> typedProperty = (Property<Source, ValueType>)property;
            ValueType[] values = ArrayUtils.ToArrayOfType_Casting<object, ValueType>(property.Range.Value.Values);

            DropDownPropertyWidget<Source, ValueType> dropDownWidget = dropDownFactory.Create<Source, ValueType>(typedProperty, uiManager,
                                                                                                             ref labelPosition,
                                                                                                             labelWidth,
                                                                                                             groupSize,
                                                                                                             horizontalSpacing,
                                                                                                             property.Name,
                                                                                                             values,
                                                                                                             ((Property<Source, ValueType>)property).DefaultValue,
                                                                                                             shouldSetImmediately: property.ShouldSetImmediately,
                                                                                                             sourceGetter: sourceGetter);

            group.AddChild(dropDownWidget);

            if (source is not null)
            {
                dropDownWidget.SetWidgetValue(typedProperty.GetValue(source));
            }
        }

        // sourceGetter is used for getting the current value of the source and setting it in the PropertyWidget.
        // sourceGetter is also given to the PropertyWidget implementation if it supports it. If so, it will be used for when the property should be set immediately as the value is changed.
        // onEnter is only used for a SpinnerPropertyWidget
        /*public static void CreateWidget<Source>(Func<Source> sourceGetter, Property<Source> property,
                                                            GroupWidget group, UIManager uiManager,
                                                            Vec2f labelPosition, float labelWidth, Vec2f groupSize, float horizontalSpacing,
                                                            Action onEnter)
        {
            ref readonly PropertyUIData uiData = ref property.UIData;

            ValidateUIMapping(property.DataType, in uiData);

            Source source = sourceGetter();

            switch (uiData.WidgetType)
            {
                case PropertyWidgetType.Spinner:
                    {
                        Property<Source, float> typedProperty = (Property<Source, float>)property;

                        FloatSpinnerPropertyWidget<Source> spinnerWidget = new FloatSpinnerPropertyWidget<Source>(typedProperty, true, uiManager, ref labelPosition, labelWidth, groupSize,
                                                                                        horizontalSpacing,
                                                                                        property.Name, property.Range.Value,
                                                                                        onEnter,
                                                                                        shouldSetImmediately: property.ShouldSetImmediately,
                                                                                        sourceGetter: sourceGetter);

                        group.AddChild(spinnerWidget);

                        spinnerWidget.SetWidgetValue(typedProperty.GetValue(source));

                        break;
                    }

                case PropertyWidgetType.Checkbox:
                    {
                        Property<Source, bool> typedProperty = (Property<Source, bool>)property;

                        CheckboxPropertyWidget<Source> checkboxWidget = new CheckboxPropertyWidget<Source>(typedProperty, uiManager, ref labelPosition, labelWidth, groupSize,
                                                                                           horizontalSpacing,
                                                                                           property.Name,
                                                                                           shouldSetImmediately: property.ShouldSetImmediately,
                                                                                           sourceGetter: sourceGetter);

                        group.AddChild(checkboxWidget);

                        checkboxWidget.SetWidgetValue(typedProperty.GetValue(source));

                        break;
                    }

                case PropertyWidgetType.Slider:
                    {
                        Property<Source, float> typedProperty = (Property<Source, float>)property;

                        SliderPropertyWidget<Source> sliderWidget = new SliderPropertyWidget<Source>(typedProperty, uiManager, ref labelPosition, labelWidth, groupSize,
                                                                                     horizontalSpacing,
                                                                                     property.Name, property.Range.Value,
                                                                                     shouldSetImmediately: property.ShouldSetImmediately,
                                                                                     sourceGetter: sourceGetter);

                        group.AddChild(sliderWidget);

                        // Keep things in sync
                        sliderWidget.CurrentValue = ((Property<Source, float>)property).GetValue(source); 
                        sliderWidget.Precision = 0;
                        sliderWidget.SetWidgetValue(typedProperty.GetValue(source));

                        break;
                    }

                case PropertyWidgetType.DropDown: // Only used for BuiltinColor right now. Come back and handle other types at some point.
                    {
                        Property<Source, Color> typedProperty = (Property<Source, Color>)property;
                        Color[] colors = Utils.ArrayToTypeCasting<Color, object>(property.Range.Value.Values);

                        DropDownPropertyWidget<Source, Color> dropDownWidget = new DropDownPropertyWidget<Source, Color>(typedProperty, uiManager,
                                                                                                                         ref labelPosition,
                                                                                                                         labelWidth,
                                                                                                                         groupSize,
                                                                                                                         horizontalSpacing,
                                                                                                                         property.Name,
                                                                                                                         colors,
                                                                                                                         ((Property<Source, Color>)property).DefaultValue,
                                                                                                                         shouldSetImmediately: property.ShouldSetImmediately,
                                                                                                                         sourceGetter: sourceGetter);

                        group.AddChild(dropDownWidget);

                        dropDownWidget.SetWidgetValue(typedProperty.GetValue(source));

                        break;
                    }

                case PropertyWidgetType.Label:
                    {
                        Property<Source, string> typedProperty = (Property<Source, string>)property;
                        string value = TextUtils.EmptyString;

                        LabelPropertyWidget<Source> widget = new LabelPropertyWidget<Source>(typedProperty,
                                                                                             uiManager,
                                                                                             ref labelPosition,
                                                                                             labelWidth,
                                                                                             groupSize,
                                                                                             horizontalSpacing,
                                                                                             property.Name,
                                                                                             value,
                                                                                             shouldSetImmediately: property.ShouldSetImmediately,
                                                                                             sourceGetter: sourceGetter);

                        group.AddChild(widget);

                        widget.SetWidgetValue(typedProperty.GetValue(source));

                        break;
                    }

                default: throw new InvalidOperationException("Invalid PropertyWidgetType: " + property.UIData.WidgetType.ToString());
            }
        }*/

        public static void AddTooltips<Source>(UIManager uiManager, GroupWidget group, IIndexable<Property<Source>> properties)
        {
            void AddTooltip(int index, PropertyWidget<Source> widget)
            {
                Property<Source> property = properties[index];

                if (widget.Name != property.Name)
                {
                    return;
                }

                string tooltip = property.Description;

                if (TextUtils.IsNullEmptyOrWhitespace(tooltip))
                {
                    return;
                }

                Tooltip<Label> tooltipListener = uiManager.TextTooltip(tooltip);

                widget.AddTooltip(tooltipListener);
            }

            group.ForEachOfTypeWithIndex<PropertyWidget<Source>>(AddTooltip, 0, group.Count);
        }

        // WARNING: THIS WILL SHIFT AND ENABLE/DISABLE ALL WIDGETS IN "group", EVEN IF THEY AREN'T PROPERTYWIDGETS!
        public static void ShiftAndEnable(GroupWidget group, int beginIndex, int count, int direction)
        {
            if (beginIndex == group.Count - 1 || direction == 0 || count == 0)
            {
                return;
            }

            direction = Math.Sign(direction);

            Vec2f spacing = count * (direction * (group.GetUnchecked(1).GetMax() - group.GetUnchecked(0).GetMax()));

            int endIndexToEnable = beginIndex + count;

            bool isVisible;
            Touchable touchMode;

            if (direction == -1)
            {
                isVisible = false;
                touchMode = Touchable.Disabled;
            }
            else
            {
                isVisible = true;
                touchMode = Touchable.Enabled;
            }

            for (int index = beginIndex; index < endIndexToEnable; index++)
            {
                Widget widget = group.GetUnchecked(index);

                widget.IsVisible = isVisible;
                widget.TouchMode = touchMode;
            }

            for (int index = endIndexToEnable; index < group.Count; index++)
            {
                group.GetUnchecked(index).MoveBy(spacing);
            }

            group.Layout();
        }

        // WARNING: THIS WILL SHIFT ALL WIDGETS IN "group", EVEN IF THEY AREN'T PROPERTYWIDGETS!
        public static void Shift(GroupWidget group, int beginIndex, int count, int direction, Vec2f spacing)
        {
            if (beginIndex == group.Count - 1 || direction == 0 || count == 0)
            {
                return;
            }

            direction = direction < 0 ? -1 : direction;

            spacing *= direction;

            for (int index = beginIndex; index < group.Count && index < count; index++)
            {
                group.GetUnchecked(index).MoveBy(spacing);
            }

            group.Layout();
        }

        // Any leftover pairs not explicitly checked are incompatible
        public static void ValidateUIMapping(PropertyDataType dataType, ref readonly PropertyUIData uiData)
        {
            PropertyWidgetType widgetType = uiData.WidgetType;

            if (widgetType == PropertyWidgetType.None)
            {
                return;
            }

            if (dataType == PropertyDataType.Float
                && (widgetType == PropertyWidgetType.Spinner || widgetType == PropertyWidgetType.Slider))
            {
                return;
            }

            if (dataType == PropertyDataType.Bool && widgetType == PropertyWidgetType.Checkbox)
            {
                return;
            }

            if (dataType == PropertyDataType.Color && widgetType == PropertyWidgetType.DropDown)
            {
                return;
            }

            if (dataType == PropertyDataType.String
                && (widgetType == PropertyWidgetType.Label || widgetType == PropertyWidgetType.TextField))
            {
                return;
            }

            if ((dataType == PropertyDataType.Color || dataType == PropertyDataType.EnumInt)
                && widgetType == PropertyWidgetType.DropDown)
            {
                return;
            }

            throw new WrongPropertyUIMappingException(dataType, widgetType);
        }

        public static float FindLargestPropertyNameWidth<T>(IIndexable<Property<T>> properties, DynamicSpriteFont font)
        {
            int count = properties.Count;

            Func<int, string> supplier = (index) => properties[index].Name;

            return FontUtils.MeasureLargestWidth(font, count, supplier);
        }

        public static FloatSpinnerGenerator<Source> GetDefaultFloatSpinnerGenerator<Source>()
        {
            return delegate (Property<Source, float> property,
                             bool shouldInitialize,
                             UIManager uiManager,
                             ref Vec2f labelPosition,
                             float labelWidth,
                             Vec2f groupSize,
                             float horizontalSpacing,
                             string name,
                             PropertyRange range,
                             Action onEnter,
                             bool shouldSetImmediately,
                             Func<Source> sourceGetter)
            {
                return new FloatSpinnerPropertyWidget<Source>(property, shouldInitialize, uiManager, ref labelPosition, labelWidth, groupSize, horizontalSpacing,
                                                              name,
                                                              range,
                                                              onEnter,
                                                              shouldSetImmediately,
                                                              sourceGetter);
            };
        }

        public static CheckboxGenerator<Source> GetDefaultCheckboxGenerator<Source>()
        {
            return delegate (Property<Source, bool> property,
                             UIManager uiManager,
                             ref Vec2f labelPosition,
                             float labelWidth,
                             Vec2f groupSize,
                             float horizontalSpacing,
                             string name,
                             bool shouldSetImmediately,
                             Func<Source> sourceGetter)
            {
                return new CheckboxPropertyWidget<Source>(property, uiManager, ref labelPosition, labelWidth, groupSize, horizontalSpacing,
                                                              name,
                                                              shouldSetImmediately,
                                                              sourceGetter);
            };
        }

        public static SliderGenerator<Source> GetDefaultSliderGenerator<Source>()
        {
            return delegate (Property<Source, float> property,
                             UIManager uiManager,
                             ref Vec2f labelPosition,
                             float labelWidth,
                             Vec2f groupSize,
                             float horizontalSpacing,
                             string name,
                             PropertyRange range,
                             bool shouldSetImmediately,
                             Func<Source> sourceGetter)
            {
                return new SliderPropertyWidget<Source>(property, uiManager, ref labelPosition, labelWidth, groupSize, horizontalSpacing,
                                                              name,
                                                              range,
                                                              shouldSetImmediately: shouldSetImmediately,
                                                              sourceGetter: sourceGetter);
            };
        }

        public static LabelGenerator<Source> GetDefaultLabelGenerator<Source>()
        {
            return delegate (Property<Source, string> property,
                             UIManager uiManager,
                             ref Vec2f labelPosition,
                             float labelWidth,
                             Vec2f groupSize,
                             float horizontalSpacing,
                             string name,
                             bool shouldSetImmediately,
                             Func<Source> sourceGetter)
            {
                return new LabelPropertyWidget<Source>(property, uiManager, ref labelPosition, labelWidth, groupSize, horizontalSpacing,
                                                              name,
                                                              TextUtils.EmptyString,
                                                              shouldSetImmediately: shouldSetImmediately,
                                                              sourceGetter: sourceGetter);
            };
        }

        public static TextFieldGenerator<Source> GetDefaultTextFieldGenerator<Source>()
        {
            return delegate (Property<Source, string> property,
                             UIManager uiManager,
                             ref Vec2f labelPosition,
                             float labelWidth,
                             Vec2f groupSize,
                             float horizontalSpacing,
                             string name,
                             Action onEnter,
                             bool shouldSetImmediately,
                             Func<Source> sourceGetter)
            {
                return new TextFieldPropertyWidget<Source>(property, uiManager, ref labelPosition, labelWidth, groupSize, horizontalSpacing,
                                                              name,
                                                              onEnter,
                                                              shouldSetImmediately: shouldSetImmediately,
                                                              sourceGetter: sourceGetter);
            };
        }
    }
}
