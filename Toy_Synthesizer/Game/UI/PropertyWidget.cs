using System;

using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;

using Toy_Synthesizer.Game.Data.Generic;

namespace Toy_Synthesizer.Game.UI
{
    public abstract class PropertyWidget<Source> : GroupWidget
    {
        public readonly string Name;

        public bool AllowValueResets { get; set; }

        public abstract string LabelText { get; }

        public abstract bool IsPropertySetImmediately { get; }

        public PropertyWidget(ref Vec2f position, Vec2f groupSize, string name)
            : base(position, groupSize, touchable: Touchable.Enabled)
        {
            this.Name = name;
            this.AllowValueResets = true; // Set to false manually if needed.
        }

        public abstract bool SetSourceValue(Source source);
        public abstract bool ResetSourceAndUI(Source source);
        public abstract void ResetUI();
        public abstract void SyncUIWithSource(Source source);

        public abstract void SwitchLabelText(string newText);

        public abstract void AddTooltip(Tooltip<Label> tooltip);
    }

    // This is purely for convenience, so you don't have to know what control it's using
    public abstract class PropertyWidget<Source, Value> : PropertyWidget<Source>
    {
        public readonly Property<Source, Value> Property;
        protected Button toggleCheckbox; // Used when Property is not null and Property.UIData.IsToggleable is true.

        public bool IsToggledOn
        {
            get => Property is not null && Property.UIData.IsToggleable && toggleCheckbox.IsChecked;
        }

        public sealed override bool IsPropertySetImmediately
        {
            get => Property.ShouldSetImmediately;
        }

        public PropertyWidget(Property<Source, Value> property, ref Vec2f position, Vec2f groupSize, string name)
            : base(ref position, groupSize, name)
        {
            Property = property;
        }

        public abstract void SetWidgetValue(Value value);
    }

    public abstract class PropertyWidget<Control, Source, Value> : PropertyWidget<Source, Value> where Control : Widget
    {
        public delegate Control ControlGenerator(int index, UIManager uiManager, Vec2f beginPosition, Vec2f groupSize, Vec2f labelSize,
                                                 float horizontalSpacing);

        private class InitializationData
        {
            internal UIManager uiManager;
            internal float labelWidth;
            internal float horizontalSpacing;
            internal ControlGenerator generator;
            internal Action<Control> postCreationAction;
        }

        private InitializationData initData;
        protected PlainLabel label;
        private Control widget;

        public Control Widget
        {
            get => widget;
        }

        public sealed override string LabelText
        {
            get => label.Text;
        }

        public float FontScale
        {
            get => label.FontScale;
        }

        public PropertyWidget(UIManager uiManager, Property<Source, Value> property, ref Vec2f position, float labelWidth, Vec2f groupSize,
                              float horizontalSpacing, string name,
                              ControlGenerator controlGenerator)
            : base(property, ref position, groupSize, name)
        {
            initData = new InitializationData
            {
                uiManager = uiManager,
                labelWidth = labelWidth,
                horizontalSpacing = horizontalSpacing,
                generator = controlGenerator
            };
        }

        protected void Init()
        {
            InitializationData initData = this.initData;
            Vec2f position = Position;

            Vec2f labelSize = new Vec2f(initData.labelWidth, Size.Y);

            InitPair(Name, 0, labelSize, initData.uiManager, position,
                     initData.horizontalSpacing, initData.generator, initData.postCreationAction);

            this.initData = null;
        }

        private void InitPair(string name, int index, Vec2f labelSize, UIManager uiManager, Vec2f position,
                              float horizontalSpacing, ControlGenerator generator, Action<Control> postCreationAction)
        {
            PlainLabel label = uiManager.PlainLabel(position, labelSize, name);
            label.FitText = false;

            Control control = generator(index, uiManager, position, Size, labelSize,
                                               horizontalSpacing);

            this.label = label;
            this.widget = control;

            postCreationAction?.Invoke(control);

            AddChild(label);

            if (Property is not null && Property.UIData.IsToggleable)
            {
                AddCheckbox(position, labelSize, uiManager);
            }

            AddChild(control);
        }

        private void AddCheckbox(Vec2f position, Vec2f labelSize, UIManager uiManager)
        {
            float labelMaxX = position.X + labelSize.X;

            float controlToLabelWidth = Widget.Position.X - labelMaxX;

            Vec2f checkboxSize = new Vec2f(controlToLabelWidth * 0.4f);
            Vec2f checkboxPosition = new Vec2f((labelMaxX + (controlToLabelWidth * 0.5f)) - (checkboxSize.X * 0.5f), (position.Y + (labelSize.Y * 0.5f) - (checkboxSize.Y * 0.5f)));

            toggleCheckbox = uiManager.Checkbox(checkboxPosition, checkboxSize);

            toggleCheckbox.Check();

            toggleCheckbox.OnCheck += delegate
            {
                Widget.Enable();
            };

            toggleCheckbox.OnUncheck += delegate
            {
                Widget.Disable();
            };

            AddChild(toggleCheckbox);
        }

        protected void AddControlGenerator(ControlGenerator initializer)
        {
            initData.generator += initializer;
        }

        protected void AddPostCreationAction(Action<Control> postCreationAction)
        {
            initData.postCreationAction += postCreationAction;
        }

        protected override void SizeChanged(ref Vec2f previousSize, ref Vec2f newSize)
        {
            Vec2f scale = Stage is null ? GetScale(previousSize, newSize) : Stage.GetScale(ScaleMode, previousSize, newSize);

            for (int index = 0; index != Count; index++)
            {
                Widget child = GetUnchecked(index);

                child.Position = (child.Position - Position) * scale + Position;
                child.Size *= scale;
            }
        }

        public sealed override void AddTooltip(Tooltip<Label> tooltip)
        {
            label.AddListener(tooltip);

            if (Property.UIData.AddTooltipToControl)
            {
                AddTooltipToControl(widget, tooltip);
            }
        }

        // Used by AddTooltip
        protected abstract void AddTooltipToControl(Control control, Tooltip<Label> tooltip);

        protected abstract Value GetValue(Control control);

        // returns true the Source value was set/if the value in the UI was different from the current Source value
        public sealed override bool SetSourceValue(Source source)
        {
            if (Property.UIData.IsToggleable && !IsToggledOn)
            {
                return false;
            }

            return Property.SetValue(source, GetValue(widget));
        }

        // returns true the Source value was set/if the default value was different from the current Source value; also sets UI to the default value
        public sealed override bool ResetSourceAndUI(Source source)
        {
            bool valueChanged = Property.Reset(source);

            if (AllowValueResets)
            {
                ResetUI();
            }

            return valueChanged;
        }

        public sealed override void ResetUI()
        {
            if (!AllowValueResets)
            {
                return;
            }

            SetWidgetValue(Property.DefaultValue);
        }

        public sealed override void SyncUIWithSource(Source source)
        {
            SetWidgetValue(Property.GetValue(source));
        }

        public sealed override void SwitchLabelText(string newText)
        {
            label.Text = newText;
        }
    }
}
