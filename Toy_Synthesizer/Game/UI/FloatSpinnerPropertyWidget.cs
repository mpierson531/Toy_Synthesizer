using System;

using Microsoft.Xna.Framework.Input;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using Toy_Synthesizer.Game.Data.Generic;
using Toy_Synthesizer.Game.Data;


namespace Toy_Synthesizer.Game.UI
{
    public class FloatSpinnerPropertyWidget<Source> : PropertyWidget<NumberSpinner<float>, Source, float>
    {
        protected TextField spinnerView;
        protected PropertyRange range;
        public bool ShouldSetImmediately;
        public Func<Source> SourceGetter; // This should only be used when ShouldSetImmediately is true.

        public TextField SpinnerView
        {
            get => spinnerView;
        }

        public float CurrentValue
        {
            get => Widget.CurrentValue;
            set => Widget.CurrentValue = value;
        }

        public PropertyRange Range
        {
            get => range;

            set
            {
                range = value;

                Widget.MinValue = range.Min;
                Widget.MaxValue = range.Max;
                Widget.IncrementBy = range.Increment;
            }
        }

        public Action<float> OnValidNumberInput { get; set; }

        public FloatSpinnerPropertyWidget(Property<Source, float> property, bool shouldInitialize, UIManager uiManager,
                                     ref Vec2f position, float labelWidth, Vec2f groupSize, float horizontalSpacing,
                                     string name, PropertyRange range, Action onEnter,
                                     bool shouldSetImmediately = false, Func<Source> sourceGetter = null)
            : base(uiManager, property, ref position, labelWidth, groupSize, horizontalSpacing, name, null)
        {
            ControlGenerator generator = GetControlGenerator();

            this.range = range;

            ShouldSetImmediately = shouldSetImmediately;
            SourceGetter = sourceGetter;

            AddControlGenerator(generator);
            AddOnEnter(onEnter);

            if (shouldInitialize)
            {
                base.Init();
            }
        }

        public bool SetValueIfNotEqual(float value)
        {
            return Widget.SetValueIfNotEqual(value);
        }

        private void AddOnEnter(Action onEnter)
        {
            if (onEnter is not null)
            {
                InputListener keyEnterCaptureListener = new InputListener
                {
                    KeyEnter = delegate (InputEvent e, Keys key)
                    {
                        onEnter();

                        e.HandleAndStop();
                    }
                };

                AddCaptureListener(keyEnterCaptureListener);
            }
        }

        private ControlGenerator GetControlGenerator()
        {
            return delegate (int index, UIManager uiManager, Vec2f beginPosition, Vec2f groupSize, Vec2f labelSize,
                            float horizontalSpacing)
            {
                Vec2f position = new Vec2f(beginPosition.X + labelSize.X + horizontalSpacing, beginPosition.Y);
                Vec2f size = new Vec2f((beginPosition.X + groupSize.X) - position.X, groupSize.Y);

                NumberSpinner<float> spinner = uiManager.DefaultNumberSpinner(position, size,
                                                      min: range.Min, max: range.Max, increment: range.Increment,
                                                      start: 0f,
                                                      maxCharacters: 500);

                TextField field = (TextField)spinner.View;

                OnValidNumberInput += delegate (float value)
                {
                    SetWidgetValue(value);
                };

                UIManager.AddNumberFieldValueListener(field, OnValidNumberInput);

                spinnerView = (TextField)spinner.View;

                spinner.OnIncrementOrDecrement += delegate
                {
                    if (ShouldSetImmediately && SourceGetter is not null)
                    {
                        SetSourceValue(SourceGetter());
                    }
                };

                return spinner;
            };
        }

        protected sealed override void AddTooltipToControl(NumberSpinner<float> control, Tooltip<Label> tooltip)
        {
            control.Parent.AddListener(tooltip);

            //control.View.AddListener(tooltip);
        }

        protected sealed override float GetValue(NumberSpinner<float> spinner)
        {
            return float.Parse(SpinnerView.Text);
        }

        public sealed override void SetWidgetValue(float value)
        {
            Widget.CurrentValue = value;
        }
    }
}
