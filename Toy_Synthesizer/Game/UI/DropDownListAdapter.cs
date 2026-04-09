using System;

using FontStashSharp;

using GeoLib;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Data;
using GeoLib.GeoGraphics.UI.Data.Generic;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils.Collections;
using GeoLib.GeoUtils.Converters;

namespace Toy_Synthesizer.Game.UI
{
    public class DropDownListAdapter : DropDownAdapter
    {
        public delegate void ValueChangedDelegate(object previousValue, int previousIndex, object newValue, int newIndex);

        private static string[] GetNames(ViewableList<object> values, ToStringConverter converter)
        {
            if (CollectionUtils.IsNullOrEmpty(values))
            {
                return Array.Empty<string>();
            }

            return values.ProcessToArray<string>(value => ToStringConverter.Convert(converter, value));
        }

        private IPropertyBinding propertyBinding;

        private ViewableList<object> values;
        private object previousValue;
        private object currentValue;
        private int previousIndex;
        private int currentIndex;
        private ToStringConverter toStringConverter;

        public ToStringConverter ToStringProvider
        {
            get => toStringConverter;

            set
            {
                toStringConverter = value;

                UpdateNames();
            }
        }

        public int ValueCount
        {
            get => CollectionUtils.IsNullOrEmpty(values) ? 0 : values.Count;
        }

        public object CurrentValue
        {
            get => currentValue;
            set => SetCurrentValue(value, updateProperty: true);
        }

        public int CurrentIndex
        {
            get => currentIndex;
        }

        public object PreviousValue
        {
            get => previousValue;
        }

        public int PreviousIndex
        {
            get => previousIndex;
        }

        public ValueChangedDelegate OnValueChanged { get; set; }

        // This will mutate group.
        // Any children you manually add will be affected by this object.
        public DropDownListAdapter(GroupWidget group,
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
            : base(group: group,
                   itemNames: GetNames(values, toStringProvider),
                   itemCount: CollectionUtils.IsNullOrEmpty(values) ? 0 : values.Count,
                   coverButtonProvider: coverButtonProvider,
                   childProvider: childProvider,
                   groupProvider: groupProvider,
                   dropDownPosition: dropDownPosition,
                   dropDownWidth: dropDownWidth,
                   dropDownMaxHeight: dropDownMaxHeight,
                   dropDownHeightPadding: dropDownHeightPadding,
                   buttonStartPosition: buttonStartPosition,
                   buttonSize: buttonSize,
                   buttonSpacing: buttonSpacing)
        {
            this.toStringConverter = toStringProvider;

            this.values = values;

            currentIndex = defaultIndex;

            if (ValueCount > 0)
            {
                previousIndex = defaultIndex;
                previousValue = values[defaultIndex];
                currentValue = values[defaultIndex];

                SetCurrentValue(defaultIndex);
            }
            else
            {
                previousIndex = -1;
                previousValue = null;
                currentValue = null;
            }
        }

        public void SetValueWithoutProperty(object value)
        {
            SetCurrentValue(value, updateProperty: false);
        }

        public ConvertingPropertyBinding<T, object> BindProperty<T>(PropertyBindable<T> property, Func<T, object> sourceToTarget, Func<object, T> targetToSource)
        {
            if (propertyBinding is not null)
            {
                propertyBinding.Dispose();
            }

            propertyBinding = new ConvertingPropertyBinding<T, object>(property, Property_GetTarget, Property_SetTarget, sourceToTarget, targetToSource);

            return (ConvertingPropertyBinding<T, object>)propertyBinding;
        }

        public ConvertingPropertyBinding<T, object> BindProperty<T>(PropertyBindable<T> property)
        {
            if (propertyBinding is not null)
            {
                propertyBinding.Dispose();
            }

            propertyBinding = new ConvertingPropertyBinding<T, object>(property, Property_GetTarget, Property_SetTarget, Property_DefaultSourceToTarget<T>, Property_DefaultTargetToSource<T>);

            return (ConvertingPropertyBinding<T, object>)propertyBinding;
        }

        private object Property_GetTarget()
        {
            return CurrentValue;
        }

        private void Property_SetTarget(object value)
        {
            SetCurrentValue(value, updateProperty: false);
        }

        private static object Property_DefaultSourceToTarget<T>(T value)
        {
            return value;
        }

        private static T Property_DefaultTargetToSource<T>(object value)
        {
            return (T)value;
        }

        protected override void SelectInternal(Button button, int index)
        {
            SetCurrentValue(index);
        }

        private void SetCurrentValue(object value, bool updateProperty)
        {
            int index = values.IndexOf(value);

            SetCurrentValueInternal(CurrentIndex, index, CurrentValue, value, updateProperty);
        }

        private void SetCurrentValue(int index)
        {
            SetCurrentValueInternal(CurrentIndex, index, CurrentValue, values[index], updateProperty: true);
        }

        private void SetCurrentValueInternal(int previousIndex, int index, object previousValue, object value, bool updateProperty)
        {
            this.previousIndex = previousIndex;
            this.previousValue = previousValue;

            currentIndex = index;
            currentValue = value;

            if (CoverButton is ITextWidget coverButtonTextWidget)
            {
                coverButtonTextWidget.Text = ConvertToString(currentValue);
            }

            OnValueChanged?.Invoke(previousValue, previousIndex, currentValue, index);

            if (updateProperty && propertyBinding is not null)
            {
                propertyBinding.UpdateSource();
            }
        }

        private string ConvertToString(object value)
        {
            return ToStringConverter.Convert(ToStringProvider, value);
        }

        public void SetFont(DynamicSpriteFont font)
        {
            GeoDebug.Assert(Group.Contains(CoverButton));

            Group.ForEachOfType<ITextWidget>(delegate (ITextWidget textWidget)
            {
                textWidget.Font = font;
            });

            DropDownGroup.ForEachOfType<ITextWidget>(delegate (ITextWidget textWidget)
            {
                textWidget.Font = font;
            });
        }

        private void UpdateNames()
        {
            for (int index = 0; index < ValueCount; index++)
            {
                TextButton button = (TextButton)DropDownGroup[index];

                button.Text = ConvertToString(index);
            }
        }

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            if (propertyBinding is not null)
            {
                propertyBinding.Dispose();

                propertyBinding = null;
            }
        }

        private static ValueTuple<Vec2f, Vec2f> FindMinAndMax(GroupWidget group)
        {
            Vec2f min = group.Position;
            Vec2f max = group.Position;

            for (int index = 0; index != group.Count; index++)
            {
                Widget child = group.GetUnchecked(index);

                if (child is GroupWidget)
                {
                    ValueTuple<Vec2f, Vec2f> childGroupMinMax = FindMinAndMax((GroupWidget)child);

                    min.X = MathF.Min(min.X, MathF.Min(childGroupMinMax.Item1.X, childGroupMinMax.Item2.X));
                    min.Y = MathF.Min(min.Y, MathF.Min(childGroupMinMax.Item1.Y, childGroupMinMax.Item2.Y));

                    max.X = MathF.Max(max.X, MathF.Max(childGroupMinMax.Item1.X, childGroupMinMax.Item2.X));
                    max.Y = MathF.Max(max.Y, MathF.Max(childGroupMinMax.Item1.Y, childGroupMinMax.Item2.Y));

                    continue;
                }

                if (child.Position.X < min.X)
                {
                    min.X = child.Position.X;
                }

                if (child.Position.Y < min.Y)
                {
                    min.Y = child.Position.Y;
                }

                if (child.GetMaxX() > max.X)
                {
                    max.X = child.GetMaxX();
                }

                if (child.GetMaxY() > max.Y)
                {
                    max.Y = child.GetMaxY();
                }
            }

            return new ValueTuple<Vec2f, Vec2f>(min, max);
        }
    }
}