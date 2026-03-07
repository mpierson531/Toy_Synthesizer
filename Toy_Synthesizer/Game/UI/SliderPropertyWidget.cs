using System;

using GeoLib;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;

using Toy_Synthesizer.Game.Data;
using Toy_Synthesizer.Game.Data.Generic;

namespace Toy_Synthesizer.Game.UI
{
    public class SliderPropertyWidget<Source> : PropertyWidget<Slider, Source, float>
    {
        private readonly float baseDisplaySpacingWidth;
        private readonly Vec2f displaySizeScalar;

        private Slider slider;
        private Label display;
        private PropertyRange range;

        public int Precision
        {
            get => slider.Precision;
            set => slider.Precision = value;
        }

        public float CurrentValue
        {
            get => slider.CurrentValue;
            set => slider.CurrentValue = value;
        }

        public PropertyRange Range
        {
            get => range;

            set
            {
                range = value;

                slider.MinValue = range.Min;
                slider.MaxValue = range.Max;
            }
        }

        public Action<Slider, float, float> OnValueChange
        {
            get => slider.OnValueChange;
            set => slider.OnValueChange = value;
        }

        public bool ShouldSetImmediately;
        public Func<Source> SourceGetter; // This should only be used when ShouldSetImmediately is true.

        public SliderPropertyWidget(Property<Source, float> settable, UIManager uiManager, ref Vec2f position, float labelWidth, Vec2f groupSize, float horizontalSpacing,
                                    string name, PropertyRange range,
                                    float displayWidthScalar = 0.06f,
                                    float displayHeightScalar = 1f,
                                    bool shouldSetImmediately = false,
                                    Func<Source> sourceGetter = null)
            : base(uiManager, settable, ref position, labelWidth, groupSize, horizontalSpacing, name, null)
        {
            ShouldSetImmediately = shouldSetImmediately;
            SourceGetter = sourceGetter;

            ControlGenerator generator = GetControlGenerator();
            AddControlGenerator(generator);

            this.range = range;

            baseDisplaySpacingWidth = uiManager.ScaleWidth(12f);
            displaySizeScalar = new Vec2f(displayWidthScalar, displayHeightScalar);

            base.Init();
        }

        protected override void SizeChanged(ref Vec2f previousSize, ref Vec2f newSize)
        {
            base.SizeChanged(ref previousSize, ref newSize);

            Vec2f displaySize = GetDisplaySize();
            Vec2f displayPosition = GetDisplayPosition(displaySize.X);

            display.Position = displayPosition;
            display.Size = displaySize;
        }

        private Vec2f GetSliderPosition(float sliderWidth, float displayX, float displaySpacingWidth)
        {
            return new Vec2f(displayX - displaySpacingWidth - sliderWidth, Position.Y);
        }

        private Vec2f GetSliderSize(float minX, float displayWidth, float displaySpacingWidth)
        {
            return new Vec2f((Position.X + Size.X - displayWidth - displaySpacingWidth) - minX, Size.Y);
        }

        private Vec2f GetDisplayPosition(float displayWidth)
        {
            return new Vec2f(Position.X + Size.X - displayWidth, Position.Y);
        }

        private Vec2f GetDisplaySize()
        {
            return Size * displaySizeScalar;
        }

        public void SetValueWithoutCallbacks(float value)
        {
            slider.SetValueWithoutCallbacks(value);
            display.Text = slider.CurrentValue.ToString();
        }

        private ControlGenerator GetControlGenerator()
        {
            return delegate (int index, UIManager uiManager, Vec2f beginPosition, Vec2f groupSize, Vec2f labelSize,
                             float horizontalSpacing)
            {
                Vec2f displaySize = GetDisplaySize();
                Vec2f displayPosition = GetDisplayPosition(displaySize.X);

                float sliderMinX = beginPosition.X + labelSize.X + horizontalSpacing;
                Vec2f sliderSize = GetSliderSize(sliderMinX, displaySize.X, baseDisplaySpacingWidth);
                Vec2f sliderPosition = GetSliderPosition(sliderSize.X, displayPosition.X, baseDisplaySpacingWidth);

                Label display = uiManager.BackgroundedLabel(displayPosition, displaySize, "0", alignment: Alignment.Center);
                Slider slider = uiManager.Slider(sliderPosition, sliderSize, 0f, NumberRange<float>.From(range.Min, range.Max), range.Increment);

                slider.AddChild(display);

                slider.OnValueChange += delegate (Slider slider, float previousValue, float newValue)
                {
                    display.Text = slider.CurrentValue.ToString();

                    if (ShouldSetImmediately && SourceGetter is not null)
                    {
                        SetSourceValue(SourceGetter());
                    }
                };

                this.slider = slider;
                this.display = display;

                return slider;
            };
        }

        protected sealed override void AddTooltipToControl(Slider control, Tooltip<Label> tooltip)
        {
            GeoDebug.Assert(control == this.slider && control[0] == this.display);

            control.AddListener(tooltip);
            control[0].AddListener(tooltip); // label/display
            control.Rail.AddListener(tooltip);
            control.Knob.AddListener(tooltip);
        }

        protected sealed override float GetValue(Slider slider)
        {
            return slider.CurrentValue;
        }

        public override void SetWidgetValue(float value)
        {
            Widget.CurrentValue = value;
        }
    }
}
