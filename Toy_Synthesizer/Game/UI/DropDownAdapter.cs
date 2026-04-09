using System;
using Microsoft.Xna.Framework.Input;

using GeoLib;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils.Collections;

namespace Toy_Synthesizer.Game.UI
{
    // TODO: Implement better sizing parameters for drop down group.
    public class DropDownAdapter : Disposable
    {
        private bool isInitialized;

        private readonly int subChildBeginIndex;
        private int itemCount;
        private bool isShowing;

        private readonly ViewableList<string> itemNames;
        private readonly GroupWidget group;
        private GroupWidget dropDownGroup;
        private Button coverButton;
        private Button checkedButton;
        private Widget previousFocus;

        public Func<Vec2f, Vec2f, Button> CoverButtonProvider { get; set; }
        public Func<string, int, Vec2f, Vec2f, Button> ChildProvider { get; set; }

        // Treated as values relative to group/the parent.
        public Vec2fValue DropDownPosition { get; set; }
        public FloatValue? DropDownWidth { get; set; }
        public FloatValue? DropDownMaxHeight { get; set; }
        public FloatValue? DropDownHeightPadding { get; set; }

        public Vec2fValue ButtonStartPosition { get; set; }
        public Vec2fValue ButtonSize { get; set; }
        public Vec2fValue ButtonSpacing { get; set; }

        public GroupWidget Group
        {
            get => group;
        }

        public GroupWidget DropDownGroup
        {
            get => dropDownGroup;
        }

        public Button CoverButton
        {
            get => coverButton;
        }

        public bool IsShowing
        {
            get => isShowing;
        }

        public event Action<Button, int> OnSelect;

        public int ItemCount
        {
            get => itemCount;
        }

        // This will mutate group.
        // Any children you manually add will be affected by this object.
        public DropDownAdapter(GroupWidget group, string[] itemNames, int itemCount,
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
                               Action<Button, int> onSelect = null)
        {
            isInitialized = false;

            this.itemNames = new ViewableList<string>(itemNames);

            this.itemCount = itemCount;

            CoverButtonProvider = coverButtonProvider;
            ChildProvider = childProvider;
            DropDownPosition = dropDownPosition;
            DropDownWidth = dropDownWidth;
            DropDownMaxHeight = dropDownMaxHeight;
            DropDownHeightPadding = dropDownHeightPadding;
            ButtonStartPosition = buttonStartPosition;
            ButtonSize = buttonSize;
            ButtonSpacing = buttonSpacing;

            this.group = group;

            subChildBeginIndex = 0;

            InitGroup(groupProvider);

            InitChildren();

            OnSelect = onSelect;

            isInitialized = true;
        }

        public void Select(Button button, int index)
        {
            SelectInternal(button, index);

            OnSelect?.Invoke(button, index);
        }

        protected virtual void SelectInternal(Button button, int index)
        {

        }

        public void Set(string[] itemNames, int itemCount, Action<Button, int> onSelect)
        {
            this.itemNames.SetRaw(itemNames, itemCount);
            this.itemCount = itemCount;

            this.OnSelect = onSelect;

            InitChildren();
        }

        private void InitGroup(Func<Vec2f, Vec2f, GroupWidget> groupProvider)
        {
            if (dropDownGroup is not null)
            {
                dropDownGroup.Dispose();
            }

            Vec2f buttonPosition = GetButtonStartPosition();
            Vec2f buttonSize = GetButtonSize();
            Vec2f buttonSpacing = GetButtonSpacing();

            Vec2f dropDownPosition = GetDropDownPosition();
            Vec2f dropDownSize = GetDropDownSize(buttonSize, buttonSpacing);

            dropDownGroup = groupProvider(dropDownPosition, dropDownSize);

            group.OnReposition += ParentRepositioned;
            group.OnResize += ParentResized;

            // dropDownGroup is added to the stage directly, so this allows listeners on group to receive events from dropDownGroup.
            InputListener dropDownKeyListener = new InputListener
            {
                KeyDown = delegate (InputEvent e, Keys key)
                {
                    if (e.IsHandled)
                    {
                        return;
                    }

                    group.Fire(e);
                },

                KeyUp = delegate (InputEvent e, Keys key)
                {
                    if (e.IsHandled)
                    {
                        return;
                    }

                    group.Fire(e);
                }
            };

            dropDownGroup.AddListener(dropDownKeyListener);

            AddFocusCaptureListener(dropDownGroup);
        }

        private void InitChildren()
        {
            Vec2f widgetPosition = group.Position;
            Vec2f widgetSize = group.Size;

            if (!isInitialized)
            {
                coverButton = InitCoverButton(widgetPosition, widgetSize, CoverButtonProvider);

                group.AddChild(coverButton);
            }

            if (isInitialized)
            {
                coverButton.Position = widgetPosition;
                coverButton.Size = widgetSize;

                dropDownGroup.Clear();
            }

            Vec2f buttonPosition = GetButtonStartPosition();
            Vec2f buttonSize = GetButtonSize();
            Vec2f buttonSpacing = GetButtonSpacing();

            dropDownGroup.Position = GetDropDownPosition();
            dropDownGroup.Size = GetDropDownSize(buttonSize, buttonSpacing);

            for (int index = 0; index < itemCount; index++)
            {
                Button dropDownChild = ChildProvider(itemNames[index], index, buttonPosition, buttonSize);

                int childIndex = index;

                dropDownChild.OnClick += delegate
                {
                    ButtonClicked(dropDownChild, childIndex);
                };

                dropDownChild.IsVisible = false;

                dropDownGroup.AddChild(dropDownChild);

                buttonPosition.Y += buttonSize.Y + buttonSpacing.Y;
            }
        }

        private Button InitCoverButton(Vec2f position, Vec2f size, Func<Vec2f, Vec2f, Button> provider)
        {
            Button coverButton = provider(position, size);

            coverButton.OnCheck += Show;
            coverButton.OnUncheck += Hide;

            AddFocusCaptureListener(coverButton);

            return coverButton;
        }

        private void AddFocusCaptureListener(Widget widget)
        {
            FocusListener focusListener = GetFocusCaptureListener();

            widget.AddCaptureListener(focusListener);
        }

        private FocusListener GetFocusCaptureListener()
        {
            return new FocusListener
            {
                Unfocus = delegate (FocusEvent e)
                {
                    if (IsShowing && (e.RelatedActor is null || /*!group.IsAscendantOf(e.RelatedActor) || */ !dropDownGroup.IsAscendantOf(e.RelatedActor)))
                    {
                        e.HandleAndStop();

                        if (IsShowing)
                        {
                            Hide();
                        }
                    }
                }
            };
        }

        private void ButtonClicked(Button child, int index)
        {
            if (index != -1)
            {
                Button button = (Button)dropDownGroup.GetUnchecked(index);

                if (button != checkedButton)
                {
                    if (checkedButton is not null)
                    {
                        checkedButton.Uncheck();
                    }

                    checkedButton = button;
                }
            }

            if (coverButton.IsChecked)
            {
                Hide();
            }

            Select(child, index);
        }

        public void Show()
        {
            if (IsShowing)
            {
                return;
            }

            SetListItemsVisible(isVisible: true);
        }

        public void Hide()
        {
            if (!IsShowing)
            {
                return;
            }

            SetListItemsVisible(isVisible: false);
        }

        private void SetListItemsVisible(bool isVisible)
        {
            isShowing = isVisible;

            group.Layout();
            LayoutChildren();

            if (coverButton.IsChecked != isVisible)
            {
                coverButton.IsChecked = isVisible;
            }

            if (isVisible)
            {
                group.Stage.AddWidget(dropDownGroup);

                previousFocus = group.Stage.Focused;

                group.Stage.Focus(dropDownGroup, false);
            }
            else
            {
                if (dropDownGroup is ScrollPane scrollPane)
                {
                    if (scrollPane.CurrentOffset.Y != 0f)
                    {
                        scrollPane.ScrollVerticalBy(scrollPane.CurrentOffset.Y); // If hiding, scroll scrollpane back to zero.
                    }
                }

                dropDownGroup.Stage.RemoveWidget(dropDownGroup);

                if (previousFocus is not null && !dropDownGroup.IsAscendantOf(previousFocus) && group.Stage is not null)
                {
                    group.Stage.Focus(previousFocus, false);
                }
            }

            dropDownGroup.Layout();
        }

        private Vec2f GetDropDownPosition()
        {
            return group.Position + DropDownPosition.Compute(group.Size);
        }

        private Vec2f GetDropDownSize(Vec2f buttonSize, Vec2f buttonSpacing)
        {
            float width = DropDownWidth.HasValue ? DropDownWidth.Value.Compute(group.Size.X) : buttonSize.X;

            float height = (MathF.Max(ItemCount, 1) * buttonSize.Y) + ((ItemCount - 1) * buttonSpacing.Y);

            if (DropDownHeightPadding.HasValue)
            {
                height += DropDownHeightPadding.Value.Compute(group.Size.Y);
            }

            if (DropDownMaxHeight.HasValue)
            {
                height = MathF.Min(height, DropDownMaxHeight.Value.Compute(group.Size.Y));
            }

            return new Vec2f(width, height);
        }

        private Vec2f GetButtonStartPosition()
        {
            return group.Position + ButtonStartPosition.Compute(group.Size);
        }

        private Vec2f GetButtonSize()
        {
            return ButtonSize.Compute(group.Size);
        }

        private Vec2f GetButtonSpacing()
        {
            return ButtonSpacing.Compute(group.Size);
        }

        private void ParentRepositioned(Vec2f previousPosition, Vec2f newPosition)
        {
            LayoutChildren();
        }

        private void ParentResized(Vec2f previousSize, Vec2f newSize)
        {
            LayoutChildren();
        }

        public void Unfocus()
        {
            if (!IsShowing)
            {
                return;
            }

            // IsShowing will only be true if the coverButton is checked and focused.
            // So at this point, Stage.Focused should be coverButton; call Stage.Unfocus, and through the FocusListener on coverButton, it will hide the list view.
            // It does it this way to attempt to maintain correct event logic.
            group.Stage.Unfocus(false);
        }

        private void LayoutChildren()
        {
            Vec2f position = group.Position;
            Vec2f size = group.Size;

            coverButton.Position = position;
            coverButton.Size = size;

            Vec2f buttonPosition = GetButtonStartPosition();
            Vec2f buttonSize = GetButtonSize();
            Vec2f buttonSpacing = GetButtonSpacing();

            dropDownGroup.Position = GetDropDownPosition();
            dropDownGroup.Size = GetDropDownSize(buttonSize, buttonSpacing);

            if (dropDownGroup is ScrollPane scrollPane)
            {
                scrollPane.OverScrollAmount = (buttonPosition - dropDownGroup.Position).Abs();
                //scrollPane.ViewportPadding = new Vec2f(AbsoluteButtonPadding.X == 0f ? 0f : AbsoluteButtonPadding.X - 1f, AbsoluteButtonPadding.Y == 0f ? 0f : AbsoluteButtonPadding.Y - 1);
            }

            if (IsShowing)
            {
                if (dropDownGroup is ScrollPane)
                {
                    buttonPosition += ((ScrollPane)dropDownGroup).CurrentOffset;
                }

                for (int index = subChildBeginIndex; index != dropDownGroup.Count; index++) // starts at one so it doesn't do the cover button
                {
                    Widget child = dropDownGroup.GetUnchecked(index);

                    child.IsVisible = true;

                    child.Position = buttonPosition;
                    child.Size = buttonSize;

                    buttonPosition.Y += buttonSize.Y + buttonSpacing.Y;
                }
            }
            else
            {
                for (int index = subChildBeginIndex; index != dropDownGroup.Count; index++)
                {
                    Widget child = dropDownGroup.GetUnchecked(index);

                    child.IsVisible = false;

                    child.Position = buttonPosition;
                    child.Size = buttonSize;
                }
            }

            dropDownGroup.Layout();
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