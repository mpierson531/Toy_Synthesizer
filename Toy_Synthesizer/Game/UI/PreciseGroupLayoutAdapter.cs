using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FontStashSharp;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

namespace Toy_Synthesizer.Game.UI
{
    public class PreciseGroupLayoutAdapter : WidgetAdapter
    {
        private readonly ViewableList<WidgetState> layoutState;
        private bool stateSavedFromLastAttachment;
        private bool currentlyAttachedGroup_PositionChildren;
        private bool currentlyAttachedGroup_SizeChildren;
        private bool isEnabled;

        private Widget currentlyAttachedWidget;

        // If true, layout state will be saved between attachements and applied when attached to a GroupWidget again.
        // Should probably only be used for GroupWidgets whose child hierarchy will not be changed.
        public bool SaveStateWhenDetached { get; set; }

        public bool IsEnabled
        {
            get => isEnabled;
        }

        public bool IsPositioningEnabled { get; set; } = true;
        public bool IsSizingEnabled { get; set; } = true;

        public PreciseGroupLayoutAdapter(bool saveStateWhenDetached = false,
                                         int defaultCapacity = 100)
        {
            this.SaveStateWhenDetached = saveStateWhenDetached;

            this.stateSavedFromLastAttachment = false;

            this.currentlyAttachedGroup_PositionChildren = false;
            this.currentlyAttachedGroup_SizeChildren = false;

            this.layoutState = new ViewableList<WidgetState>(defaultCapacity);

            isEnabled = true;
        }

        public bool TryGetNormalizedBounds(Widget widget, out AABB normalizedBounds)
        {
            WidgetState state = layoutState.Find(state => state.widget == widget);

            if (state is null)
            {
                normalizedBounds = default;

                return false;
            }

            normalizedBounds = state.normalizedBounds;

            return true;
        }

        public bool TrySetNormalizedBounds(Widget widget, AABB normalizedBounds)
        {
            WidgetState state = layoutState.Find(state => state.widget == widget);

            if (state is null)
            {
                return false;
            }

            state.normalizedBounds = normalizedBounds;

            return true;
        }

        public void Enable()
        {
            isEnabled = true;
        }

        public void Disable()
        {
            isEnabled = false;
        }

        protected override void Attached(Widget widget)
        {
            if (widget is not GroupWidget group)
            {
                return;
            }

            currentlyAttachedWidget = widget;

            if (stateSavedFromLastAttachment)
            {
                stateSavedFromLastAttachment = false;

                Layout(GroupWidget.LayoutArgs.CreateFromThis(group));

                return;
            }

            for (int index = 0; index < group.Count; index++)
            {
                Widget child = group[index];

                WidgetState state = CreateState(group, child);

                this.layoutState.Add(state);
            }

            group.OnLayout += AttachedWidget_OnLayout;

            group.OnChildAdded += AttachedWidget_OnChildAdded;
            group.OnChildRemoved += AttachedWidget_OnChildRemoved;
        }

        protected override void Removed(Widget widget)
        {
            if (widget is GroupWidget group)
            {
                group.OnLayout -= AttachedWidget_OnLayout;

                group.OnChildAdded -= AttachedWidget_OnChildAdded;
                group.OnChildRemoved -= AttachedWidget_OnChildRemoved;

                if (currentlyAttachedGroup_PositionChildren)
                {
                    group.PositionChildren = true;

                    currentlyAttachedGroup_PositionChildren = false;
                }

                if (currentlyAttachedGroup_SizeChildren)
                {
                    group.SizeChildren = true;

                    currentlyAttachedGroup_SizeChildren = false;
                }
            }

            if (SaveStateWhenDetached)
            {
                stateSavedFromLastAttachment = true;

                return;
            }

            currentlyAttachedWidget = null;

            stateSavedFromLastAttachment = false;

            layoutState.Clear();
        }

        private void AttachedWidget_OnChildAdded(GroupWidget group, Widget child, int index)
        {
            WidgetState state = CreateState(group, child);

            this.layoutState.Add(state);
        }

        private void AttachedWidget_OnChildRemoved(GroupWidget group, Widget child, int index)
        {
            int stateIndex = layoutState.FindIndexOf(state => state.widget == child);

            if (stateIndex != -1)
            {
                layoutState.RemoveAt(stateIndex);
            }
        }

        private void AttachedWidget_OnLayout(GroupWidget.LayoutArgs layoutArgs)
        {
            Layout(layoutArgs);
        }

        private void CheckAttachedWidgetPositionAndSizeChildren()
        {
            if (currentlyAttachedWidget is GroupWidget group)
            {
                if (group.PositionChildren)
                {
                    currentlyAttachedGroup_PositionChildren = true;

                    group.PositionChildren = false;
                }

                if (group.SizeChildren)
                {
                    currentlyAttachedGroup_SizeChildren = true;

                    group.SizeChildren = false;
                }
            }
        }

        private void Layout(GroupWidget.LayoutArgs layoutArgs)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (layoutArgs.Sender is Drawer && layoutArgs.Target != layoutArgs.Sender)
            {
                return;
            }

            CheckAttachedWidgetPositionAndSizeChildren();

            GroupWidget group = layoutArgs.Target;

            for (int index = 0; index < layoutState.Count; index++)
            {
                WidgetState state = layoutState[index];

                if (!group.Contains(state.widget))
                {
                    layoutState.RemoveAt(index);

                    index--;

                    continue;
                }

                if (group is ScrollPane scrollPane)
                {
                    if (state.widget == scrollPane.HScrollBar 
                        || state.widget == scrollPane.VScrollBar)
                    {
                        continue;
                    }
                }

                Layout(group, state);
            }

            if (group is ScrollPane s)
            {
                // TODO: Improve (in ScrollPane in engine or here)

                // Not a great solution
                // The problem is that the scrollpane will layout,
                // triggering this to layout, *after*,
                // meaning the scrollpane will not set scrolling parameters based the new bounds of its children but the old bounds.
                // This brute forces it but probably isn't very efficient.

                s.LayoutScrolling();
            }
        }

        private void Layout(GroupWidget group, WidgetState state)
        {
            AABB absoluteBounds = GetAbsoluteBounds(group, state);

            if (IsPositioningEnabled)
            {
                CheckIfNeedsScrollPaneOffset(group, ref absoluteBounds, areBoundsNormalized: false, addOffset: false);

                state.widget.Position = absoluteBounds.Position;
            }

            if (IsSizingEnabled)
            {
                state.widget.Size = absoluteBounds.Size;
            }
        }

        public ReadOnlySpan<WidgetState> GetWidgetStates()
        {
            return layoutState.ToReadonlySpan();
        }

        public static AABB GetAbsoluteBounds(GroupWidget group, WidgetState state)
        {
            return GetAbsoluteBounds(group.GetBoundsAABB(), state);
        }

        public static AABB GetAbsoluteBounds(AABB baseBounds, WidgetState state)
        {
            return new AABB
            {
                Position = baseBounds.Position + (state.normalizedBounds.Position * baseBounds.Size),
                Size = state.normalizedBounds.Size * baseBounds.Size
            };
        }

        public static WidgetState CreateState(GroupWidget group, Widget child)
        {
            AABB bounds = GetNormalizedBounds(group, child);

            WidgetState state = new WidgetState
            {
                widget = child,
                normalizedBounds = bounds
            };

            return state;
        }

        public static AABB GetNormalizedBounds(GroupWidget group, Widget child)
        {
            return GetNormalizedBounds(group, child.GetBoundsAABB());
        }

        public static AABB GetNormalizedBounds(GroupWidget group, AABB childBounds)
        {
            AABB bounds = new AABB
            {
                Position = Vec2f.DivideOrZero(childBounds.Position - group.Position, group.Size),
                Size = Vec2f.DivideOrZero(childBounds.Size, group.Size)
            };

            CheckIfNeedsScrollPaneOffset(group, ref bounds, areBoundsNormalized: true, addOffset: true);

            return bounds;
        }

        // If group is a ScrollPane, subtracts its current offset from bounds by default. 
        // If addOffset is true, adds the offset instead.
        private static void CheckIfNeedsScrollPaneOffset(GroupWidget group, ref AABB bounds, bool areBoundsNormalized, 
                                                         bool addOffset = false)
        {
            if (group is ScrollPane scrollPaneGroup)
            {
                Vec2f scrollPaneOffset = scrollPaneGroup.CurrentOffset;

                if (addOffset)
                {
                    scrollPaneOffset = -scrollPaneOffset;
                }

                if (areBoundsNormalized)
                {
                    Vec2f normalizedScrollPaneOffset = scrollPaneOffset / scrollPaneGroup.GetViewportSize();

                    bounds.Position += new Vec2f(normalizedScrollPaneOffset.X, normalizedScrollPaneOffset.Y);
                }
                else
                {
                    bounds.Position += new Vec2f(scrollPaneOffset.X, scrollPaneOffset.Y);
                }
            }
        }

        public class WidgetState
        {
            public Widget widget;
            public AABB normalizedBounds;

            public WidgetState Copy()
            {
                return new WidgetState
                {
                    widget = widget,
                    normalizedBounds = normalizedBounds
                };
            }
        }
    }
}
