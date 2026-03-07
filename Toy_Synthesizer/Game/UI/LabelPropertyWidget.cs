using System;

using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;

using Toy_Synthesizer.Game.Data;
using Toy_Synthesizer.Game.Data.Generic;

namespace Toy_Synthesizer.Game.UI
{
    public class LabelPropertyWidget<Source> : PropertyWidget<DefaultTextWidget, Source, string>
    {
        public bool ShouldSetImmediately;
        public Func<Source> SourceGetter; // This should only be used when ShouldSetImmediately is true.

        public string CurrentValue
        {
            get => Widget.Text;
            set => Widget.Text = value;
        }

        public Action<string> OnValueChanged { get; set; }

        public LabelPropertyWidget(Property<Source, string> property, UIManager uiManager, ref Vec2f position, float labelWidth, Vec2f groupSize,
                                      float horizontalSpacing, string name,
                                      string value,
                                      bool shouldSetImmediately = false,
                                      Func<Source> sourceGetter = null)
            : base(uiManager, property, ref position, labelWidth, groupSize, horizontalSpacing, name, null)
        {
            ShouldSetImmediately = shouldSetImmediately;
            SourceGetter = sourceGetter;

            AddControlGenerator(GetControlGenerator(property.UIData, value));

            base.Init();
        }

        private ControlGenerator GetControlGenerator(PropertyUIData uiData, string value)
        {
            return delegate (int index, UIManager uiManager, Vec2f beginPosition, Vec2f groupSize, Vec2f labelSize, float horizontalSpacing)
            {
                Vec2f position = new Vec2f(beginPosition.X + labelSize.X + horizontalSpacing, beginPosition.Y);
                Vec2f size = new Vec2f((beginPosition.X + groupSize.X) - position.X, groupSize.Y);

                DefaultTextWidget label;

                if (uiData.UsePlainLabelForLabel)
                {
                    label = uiManager.PlainLabel(position, size, value, alignment: uiData.LabelAlignment);
                }
                else
                {
                    label = uiManager.BackgroundedLabel(position, size, value, alignment: uiData.LabelAlignment);
                }

                label.OnTextChanged += delegate (DefaultTextWidget label, string text)
                {
                    if (ShouldSetImmediately && SourceGetter is not null)
                    {
                        SetSourceValue(SourceGetter());
                    }
                };

                return label;
            };
        }

        public void SetFont(FontStashSharp.DynamicSpriteFont font)
        {
            //float scale = Widget.FontScale;
            //float previousTrueFontSize = scale * Widget.Font.FontSize;

            Widget.Font = font;
            //Widget.FontScale = previousTrueFontSize / font.FontSize;
        }

        public override void SetWidgetValue(string value)
        {
            CurrentValue = value;
        }

        protected override void AddTooltipToControl(DefaultTextWidget control, Tooltip<Label> tooltip)
        {
            control.AddListener(tooltip);
        }

        protected override string GetValue(DefaultTextWidget control)
        {
            return control.Text;
        }
    }
}