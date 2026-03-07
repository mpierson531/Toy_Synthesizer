using System;

using Microsoft.Xna.Framework.Input;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using Toy_Synthesizer.Game.Data.Generic;


namespace Toy_Synthesizer.Game.UI
{
    public class TextFieldPropertyWidget<Source> : PropertyWidget<TextField, Source, string>
    {
        public bool ShouldSetImmediately;
        public Func<Source> SourceGetter; // This should only be used when ShouldSetImmediately is true.

        public string CurrentValue
        {
            get => Widget.Text;
            set => Widget.Text = value;
        }

        public Action<float> OnValidNumberInput { get; set; }

        public TextFieldPropertyWidget(Property<Source, string> property, UIManager uiManager,
                                     ref Vec2f position, float labelWidth, Vec2f groupSize, float horizontalSpacing,
                                     string name, Action onEnter,
                                     bool shouldSetImmediately = false, Func<Source> sourceGetter = null)
            : base(uiManager, property, ref position, labelWidth, groupSize, horizontalSpacing, name, null)
        {
            ControlGenerator generator = GetControlGenerator(property.UIData.TextFieldMaxCharacters);

            ShouldSetImmediately = shouldSetImmediately;
            SourceGetter = sourceGetter;

            AddControlGenerator(generator);
            AddOnEnter(onEnter);

            base.Init();
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

        private ControlGenerator GetControlGenerator(int maxCharacters)
        {
            return delegate (int index, UIManager uiManager, Vec2f beginPosition, Vec2f groupSize, Vec2f labelSize,
                            float horizontalSpacing)
            {
                Vec2f position = new Vec2f(beginPosition.X + labelSize.X + horizontalSpacing, beginPosition.Y);
                Vec2f size = new Vec2f((beginPosition.X + groupSize.X) - position.X, groupSize.Y);

                TextField textField = uiManager.GeneralTextField(position, size, maxCharacters, defaultText: null);

                textField.OnTextInput += delegate (string value)
                {
                    if (ShouldSetImmediately && SourceGetter is not null)
                    {
                        SetSourceValue(SourceGetter());
                    }
                };

                return textField;
            };
        }

        protected sealed override void AddTooltipToControl(TextField control, Tooltip<Label> tooltip)
        {
            control.AddListener(tooltip);
        }

        protected sealed override string GetValue(TextField control)
        {
            return control.Text;
        }

        public sealed override void SetWidgetValue(string value)
        {
            Widget.Text = value;
        }
    }
}
