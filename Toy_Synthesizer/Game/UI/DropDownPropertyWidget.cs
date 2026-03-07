using System;

using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;
using GeoLib.GeoUtils.Converters;

using Toy_Synthesizer.Game.Data.Generic;

namespace Toy_Synthesizer.Game.UI
{
    public class DropDownPropertyWidget<Source, DataType> : PropertyWidget<DropDownListView, Source, DataType>
    {
        private DataType[] values;
        private DataType currentValue;
        public bool ShouldSetImmediately;
        public Func<Source> SourceGetter; // This should only be used when ShouldSetImmediately is true.

        public bool IsShowing
        {
            get => Widget.IsShowing;
        }

        public DataType CurrentValue
        {
            get => currentValue;
            set => currentValue = value;
        }

        public Action<DataType, DataType> OnValueChanged { get; set; }

        public DropDownPropertyWidget(Property<Source, DataType> property, UIManager uiManager, ref Vec2f position, float labelWidth, Vec2f groupSize,
                                      float horizontalSpacing, string name,
                                      DataType[] values, DataType defaultValue,
                                      bool shouldSetImmediately = false,
                                      Func<Source> sourceGetter = null)
            : base(uiManager, property, ref position, labelWidth, groupSize, horizontalSpacing, name, null)
        {
            this.values = values;

            ShouldSetImmediately = shouldSetImmediately;
            SourceGetter = sourceGetter;

            AddControlGenerator(GetControlGenerator(property.ToStringConverter, defaultValue));

            base.Init();
        }

        private ControlGenerator GetControlGenerator(ToStringConverter toStringProvider, DataType defaultValue)
        {
            return delegate (int index, UIManager uiManager, Vec2f beginPosition, Vec2f groupSize, Vec2f labelSize, float horizontalSpacing)
            {
                Vec2f position = new Vec2f(beginPosition.X + labelSize.X + horizontalSpacing, beginPosition.Y);
                Vec2f size = new Vec2f((beginPosition.X + groupSize.X) - position.X, groupSize.Y);

                DropDownListView listView = uiManager.DropDownList(position, size, new ViewableList<object>(ArrayUtils.ToArrayOfType_Casting<DataType, object>(values)), Array.IndexOf(values, defaultValue),
                                                                   toStringConverter: toStringProvider);

                listView.OnValueChanged += delegate (object previousValue, int previousIndex, object value, int index)
                {
                    currentValue = (DataType)value;

                    if (OnValueChanged is not null)
                    {
                        OnValueChanged((DataType)previousValue, currentValue);
                    }

                    if (ShouldSetImmediately && SourceGetter is not null)
                    {
                        SetSourceValue(SourceGetter());
                    }
                };

                return listView;
            };
        }

        public void SetFont(FontStashSharp.DynamicSpriteFont font)
        {
            Widget.SetFont(font);
        }

        public override void SetWidgetValue(DataType value)
        {
            currentValue = value;

            Widget.CurrentValue = value;
        }

        protected override void AddTooltipToControl(DropDownListView control, Tooltip<Label> tooltip)
        {
            control.AddListener(tooltip);
        }

        protected override DataType GetValue(DropDownListView control)
        {
            return currentValue;
        }

        public void Show()
        {
            Widget.Show();
        }

        public void Hide()
        {
            Widget.Hide();
        }
    }
}
