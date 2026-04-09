using System;

using FontStashSharp;

using GeoLib.GeoGraphics.UI.Data;
using GeoLib.GeoGraphics.UI.Data.Generic;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils.Collections;
using GeoLib.GeoUtils.Converters;

namespace Toy_Synthesizer.Game.UI
{
    public class DropDownListView : DropDownWidget
    {
        private DropDownListAdapter dropDownListAdapter;

        public int CurrentIndex
        {
            get => dropDownListAdapter.CurrentIndex;
        }

        public object CurrentValue
        {
            get => dropDownListAdapter.CurrentValue;
            set => dropDownListAdapter.CurrentValue = value;
        }

        public DropDownListAdapter.ValueChangedDelegate OnValueChanged
        {
            get => dropDownListAdapter.OnValueChanged;
            set => dropDownListAdapter.OnValueChanged = value;
        }

        public DropDownListView(Vec2f position, Vec2f size,
                                Func<Vec2f, Vec2f, Button> coverButtonProvider,
                                Func<string, int, Vec2f, Vec2f, Button> childProvider,
                                Func<Vec2f, Vec2f, GroupWidget> groupProvider,
                                Vec2fValue dropDownPosition,
                                FloatValue? dropDownWidth,
                                FloatValue? dropDownMaxHeight,
                                FloatValue? dropDownHeightPadding,
                                Vec2fValue buttonStartPosition,
                                Vec2fValue buttonSize,
                                Vec2fValue buttonSpacing,
                                ViewableList<object> values, int defaultIndex,
                                ToStringConverter toStringProvider)
            : base(position, size, GetAdapterProvider(coverButtonProvider: coverButtonProvider,
                                           childProvider: childProvider,
                                           groupProvider: groupProvider,
                                           dropDownPosition: dropDownPosition,
                                           dropDownWidth: dropDownWidth,
                                           dropDownMaxHeight: dropDownMaxHeight,
                                           dropDownHeightPadding: dropDownHeightPadding,
                                           buttonStartPosition: buttonStartPosition,
                                           buttonSize: buttonSize,
                                           buttonSpacing: buttonSpacing,
                                           values: values,
                                           defaultIndex: defaultIndex,
                                           toStringProvider: toStringProvider))
        {

        }

        public void SetValueWithoutProperty(object value)
        {
            dropDownListAdapter.SetValueWithoutProperty(value);
        }

        public ConvertingPropertyBinding<T, object> BindProperty<T>(PropertyBindable<T> property, Func<T, object> sourceToTarget, Func<object, T> targetToSource)
        {
            return dropDownListAdapter.BindProperty(property, sourceToTarget, targetToSource);
        }

        public ConvertingPropertyBinding<T, object> BindProperty<T>(PropertyBindable<T> property)
        {
            return dropDownListAdapter.BindProperty(property);
        }

        protected override void AdapterInitialized(DropDownAdapter adapter)
        {
            this.dropDownListAdapter = (DropDownListAdapter)adapter;
        }

        public void SetFont(DynamicSpriteFont font)
        {
            dropDownListAdapter.SetFont(font);
        }

        private static Func<DropDownWidget, DropDownAdapter> GetAdapterProvider(Func<Vec2f, Vec2f, Button> coverButtonProvider,
                                                                          Func<string, int, Vec2f, Vec2f, Button> childProvider,
                                                                          Func<Vec2f, Vec2f, GroupWidget> groupProvider,
                                                                          Vec2fValue dropDownPosition,
                                                                          FloatValue? dropDownWidth,
                                                                          FloatValue? dropDownMaxHeight,
                                                                          FloatValue? dropDownHeightPadding,
                                                                          Vec2fValue buttonStartPosition,
                                                                          Vec2fValue buttonSize,
                                                                          Vec2fValue buttonSpacing,
                                                                          ViewableList<object> values, int defaultIndex,
                                                                          ToStringConverter toStringProvider)
        {
            return delegate (DropDownWidget dropDown)
            {
                return new DropDownListAdapter(dropDown,
                                           coverButtonProvider: coverButtonProvider,
                                           childProvider: childProvider,
                                           groupProvider: groupProvider,
                                           dropDownPosition: dropDownPosition,
                                           dropDownWidth: dropDownWidth,
                                           dropDownMaxHeight: dropDownMaxHeight,
                                           dropDownHeightPadding: dropDownHeightPadding,
                                           buttonStartPosition: buttonStartPosition,
                                           buttonSize: buttonSize,
                                           buttonSpacing: buttonSpacing,
                                           values: values,
                                           defaultIndex: defaultIndex,
                                           toStringProvider: toStringProvider);
            };
        }
    }
}