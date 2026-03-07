using System;

using FontStashSharp;

using GeoLib;
using GeoLib.GeoGraphics.UI;
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
            return values.ProcessToArray<string>(value => ToStringConverter.Convert(converter, value));
        }

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
            get => values.Count;
        }

        public object CurrentValue
        {
            get => currentValue;
            set => SetCurrentValue(value);
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
                               Vec2fValue? dropDownMaxSize,
                               Vec2fValue buttonStartPosition,
                               Vec2fValue buttonSize,
                               Vec2fValue buttonSpacing,
                               Vec2fValue? additionalDropDownSize,
                               ViewableList<object> values, int defaultIndex,
                               ToStringConverter toStringProvider)
            : base(group: group,
                   itemNames: GetNames(values, toStringProvider),
                   itemCount: values.Count,
                   coverButtonProvider: coverButtonProvider,
                   childProvider: childProvider,
                   groupProvider: groupProvider,
                   dropDownPosition: dropDownPosition,
                   dropDownMaxSize: dropDownMaxSize,
                   buttonStartPosition: buttonStartPosition,
                   buttonSize: buttonSize,
                   buttonSpacing: buttonSpacing,
                   additionalDropDownSize: additionalDropDownSize)
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

        protected override void SelectInternal(Button button, int index)
        {
            base.SelectInternal(button, index);

            SetCurrentValue(index);
        }

        private void SetCurrentValue(object value)
        {
            int index = values.IndexOf(value);

            SetCurrentValueInternal(CurrentIndex, index, CurrentValue, value);
        }

        private void SetCurrentValue(int index)
        {
            SetCurrentValueInternal(CurrentIndex, index, CurrentValue, values[index]);
        }

        private void SetCurrentValueInternal(int previousIndex, int index, object previousValue, object value)
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
        }

        /*private void SetCurrent(int index)
        {
            int previousIndex = currentIndex;

            SetCurrentInternal(previousIndex, index, invokeOnValueChanged: true);
        }

        protected internal void SetCurrentRaw(object value)
        {
            SetCurrentInternal(previousValue, value, invokeOnValueChanged: false);
        }

        private void SetCurrentInternal(object previousValue, object value, bool invokeOnValueChanged)
        {
            currentValue = values[index];
            currentIndex = index;

            if (CoverButton is ITextWidget coverButtonTextWidget)
            {
                coverButtonTextWidget.Text = ConvertToString(currentValue);
            }

            if (invokeOnValueChanged && OnValueChanged is not null)
            {
                object previousValue = previousIndex < 0 ? null : values[previousIndex];

                OnValueChanged(previousValue, previousIndex, currentValue, index);
            }
        }*/

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




    /*public class DropDownListAdapter<T> : DropDownAdapter
    {
        private readonly T[] values;
        private T currentValue;
        private Func<T, string> toStringProvider;

        public Func<T, string> ToStringProvider
        {
            get => toStringProvider;

            set
            {
                toStringProvider = value;

                UpdateNames();
            }
        }

        public T CurrentValue
        {
            get => currentValue;

            set
            {
                *//*if (!values.Contains(value))
                {
                    throw new InvalidOperationException("value is not contained in the set dropdown values.");
                }*//*

                SetCurrentValue(ref value);
            }
        }

        public Action<T, T> OnValueChange { get; set; }

        // This will mutate group.
        // Any children you manually add will be affected by this object.
        public DropDownListAdapter(GroupWidget group,
                               Func<Vec2f, Vec2f, Button> coverButtonProvider,
                               Func<int, Vec2f, Vec2f, Button> childProvider,
                               Func<Vec2f, Vec2f, GroupWidget> groupProvider,
                               Func<DropDownAdapter, Vec2f> dropDownPositionGetter,
                               Func<DropDownAdapter, Vec2f> dropDownSizeGetter,
                               Func<DropDownAdapter, Vec2f> buttonStartPositionGetter,
                               Func<DropDownAdapter, Vec2f> buttonSizeGetter,
                               Func<DropDownAdapter, Vec2f> buttonSpacingGetter,
                                   T[] values, T defaultValue,
                                   Func<T, string> toStringProvider)
            : base(group,
                   count: values.Length,
                   coverButtonProvider: coverButtonProvider,
                   childProvider: childProvider,
                   groupProvider: groupProvider,
                   dropDownPositionGetter: dropDownPositionGetter,
                   dropDownSizeGetter: dropDownSizeGetter,
                   buttonStartPositionGetter: buttonStartPositionGetter,
                   buttonSizeGetter: buttonSizeGetter,
                   buttonSpacingGetter: buttonSpacingGetter)
        {
            this.toStringProvider = toStringProvider;

            this.values = Utils.ArrayCopy(values);

            currentValue = defaultValue;

            OnSelect = delegate (Button button, int index)
            {
                SetCurrentValue(ref this.values[index]);
            };
        }

        private void SetCurrentValue(ref T value)
        {
            int indexOf = Array.IndexOf(values, value);

            if (indexOf == -1)
            {
                SetCurrentValueInternal(currentValue, value);

                return;
            }

            SetCurrentValue(indexOf);
        }

        private void SetCurrentValue(int index)
        {
            T previousValue = currentValue;

            SetCurrentValueInternal(previousValue, values[index]);
        }

        private void SetCurrentValueInternal(T previousValue, T value)
        {
            currentValue = value;

            if (CoverButton is ITextWidget coverButtonTextWidget)
            {
                coverButtonTextWidget.Text = ConvertToString(currentValue);
            }

            OnValueChange?.Invoke(previousValue, currentValue);
        }

        private string ConvertToString(T value)
        {
            if (ToStringProvider is null)
            {
                return Convert.ToString(value);
            }

            return ToStringProvider(value);
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
            if (ToStringProvider is not null)
            {
                for (int index = 0; index != values.Length; index++)
                {
                    TextButton button = (TextButton)DropDownGroup[index];

                    button.Text = ToStringProvider(values[index]);
                }
            }
            else
            {
                for (int index = 0; index != values.Length; index++)
                {
                    TextButton button = (TextButton)DropDownGroup[index];

                    button.Text = Convert.ToString(values[index]);
                }
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
    }*/
}