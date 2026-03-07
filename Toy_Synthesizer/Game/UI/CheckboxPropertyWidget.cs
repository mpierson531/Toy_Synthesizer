using System;

using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;

using Toy_Synthesizer.Game.Data.Generic;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.UI
{
    public class CheckboxPropertyWidget<Source> : PropertyWidget<Button, Source, bool>
    {
        public bool ShouldSetImmediately;
        public Func<Source> SourceGetter; // This should only be used when ShouldSetImmediately is true.

        public CheckboxPropertyWidget(Property<Source, bool> settable, UIManager uiManager, ref Vec2f position, float labelWidth, Vec2f groupSize,
                                      float horizontalSpacing, string name,
                                      bool shouldSetImmediately = false, Func<Source> sourceGetter = null)
            : base(uiManager, settable, ref position, labelWidth, groupSize, horizontalSpacing, name, null)
        {
            ShouldSetImmediately = shouldSetImmediately;
            SourceGetter = sourceGetter;

            AddControlGenerator(GetControlGenerator());

            Init();
        }

        private ControlGenerator GetControlGenerator()
        {
            return delegate (int index, UIManager uiManager, Vec2f beginPosition, Vec2f groupSize, Vec2f labelSize,
                            float horizontalSpacing)
            {
                // this ensures the checkbox is a square and is positioned accordingly
                Vec2f position = new Vec2f(beginPosition.X + groupSize.X - labelSize.Y, beginPosition.Y);
                Vec2f size = new Vec2f(labelSize.Y);

                Button button = uiManager.Checkbox(position, size);

                button.OnClick += delegate
                {
                    if (ShouldSetImmediately && SourceGetter is not null)
                    {
                        SetSourceValue(SourceGetter());
                    }
                };

                return button;
            };
        }

        protected sealed override void AddTooltipToControl(Button button, Tooltip<Label> tooltip)
        {
            button.AddListener(tooltip);
        }

        public void Check()
        {
            Widget.Check();
        }

        public void Uncheck()
        {
            Widget.Uncheck();
        }

        protected override bool GetValue(Button control)
        {
            return control.IsChecked;
        }

        public override void SetWidgetValue(bool value)
        {
            if (value != Widget.IsChecked)
            {
                if (value)
                {
                    Widget.Check();
                }
                else
                {
                    Widget.Uncheck();
                }
            }
        }
    }
}

