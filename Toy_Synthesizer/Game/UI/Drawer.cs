using System;
using System.Collections.Generic;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Actions;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using SharpDX.DXGI;

using static GeoLib.GeoGraphics.UI.Widgets.GroupWidget;

namespace Toy_Synthesizer.Game.UI
{
    public class Drawer : GroupWidget, IExpandable
    {
        public enum RetreatFunction
        {
            None = 0,

            RelativeToDrawer = 1,

            RelativeToChild = 2,
        }

        public const int DRAWER_CONTENT_BEGIN_INDEX = 1;
        public const bool DEFAULT_APPLY_SHOW_ACTION_TO_HIDING = true;
        public const bool DEFAULT_ANIMATE_PARENT_CHILDREN = true;
        public const bool DEFAULT_ANIMATE_PARENT_CHILDREN_WITH_INTERPOLATION = true;
        public const bool DEFAULT_RESIZE_PARENT = true;
        public const RetreatFunction DEFAULT_RETREAT_MODE = RetreatFunction.RelativeToDrawer;

        private LayoutOrientation direction;

        private readonly PreciseGroupLayoutAdapter layoutAdapter;

        private readonly DelayedAction postExpansionAction;

        private readonly SizeByAction resizeParentAction;

        // When collapsing, this value is used to determine how much to move children of this drawer.
        // It normalized, and the end position of the child is determined with this formula:
        // child.Position + ((child.Position - Position) * NormalizedRetreatAmount)
        public Vec2f? NormalizedRetreatAmount { get; set; }

        public RetreatFunction RetreatMode { get; set; }

        private bool settingSizeInternally;
        private bool layingOutInternally;
        private bool settingSizeFromSizeChanged;

        private bool parentResize_IsLayoutEnabled;

        private bool isExpanded;

        private Vec2f preExpansionSize;
        private Vec2f normalizedMovedAmount;

        private AABB? previousParentPreciseLayoutAdapterBounds_This;
        private readonly ViewableList<PreciseGroupLayoutAdapter.WidgetState> previousParentPreciseLayoutAdapterBounds = new ViewableList<PreciseGroupLayoutAdapter.WidgetState>();

        // For remembering states and correct layouts.
        private CompactBoolList childExpandablesExpanded;

        private Button coverButton;

        public Button CoverButton
        {
            get => coverButton;
        }

        public bool IsExpanded
        {
            get => isExpanded;
        }

        public float ShowDuration { get; set; }

        public Interpolation ShowInterpolation { get; set; }

        public Func<MoveToAction> ShowActionGetter { get; set; }

        public Action<MoveToAction> OnMoveActionRemoved { get; set; }

        public bool ApplyShowActionToHiding { get; set; }

        // If true, when a drawer expands, it will apply its animation (if any) when moving its parent's children.
        public bool AnimateParentChildren { get; set; }
        // If true, and if AnimateParentChildren is true, a drawer will apply its ShowInterpolation when moving its parent's children. 
        public bool AnimateParentChildrenWithInterpolation { get; set; }

        // If true, the parent of the drawer will be resized when expanding/collapsing.
        public bool ResizeParent { get; set; }

        public Vec2fValue? ExtraSizePadding { get; set; }

        public LayoutOrientation Direction
        {
            get => direction;

            set
            {
                direction = value;

                LayoutChildren(animate: false);
                Layout();
            }
        }

        public Drawer(Vec2f position, Vec2f size, Button coverButton,
                      LayoutOrientation direction,
                              float showDuration = 0f,
                              Interpolation showInterpolation = null,
                              Func<MoveToAction> showActionGetter = null,
                              Action<MoveToAction> onMoveActionCompleted = null,
                              bool applyShowActionToHiding = DEFAULT_APPLY_SHOW_ACTION_TO_HIDING,
                              bool animateParentChildren = DEFAULT_ANIMATE_PARENT_CHILDREN,
                              bool animateParentChildrenWithInterpolation = DEFAULT_ANIMATE_PARENT_CHILDREN_WITH_INTERPOLATION,
                              bool resizeParent = DEFAULT_RESIZE_PARENT,
                              Vec2f? normalizedRetreatAmount = null,
                              Vec2fValue? extraSizePadding = null,
                              RetreatFunction retreatMode = DEFAULT_RETREAT_MODE)
            : this(position: position,
                   size: size,
                   coverButton: coverButton,
                   direction: direction,
                   children: null,
                   showDuration: showDuration,
                   showInterpolation: showInterpolation,
                   showActionGetter: showActionGetter,
                   onMoveActionCompleted: onMoveActionCompleted,
                   applyShowActionToHiding: applyShowActionToHiding,
                   animateParentChildren: animateParentChildren,
                   animateParentChildrenWithInterpolation: animateParentChildrenWithInterpolation,
                   resizeParent: resizeParent,
                   normalizedRetreatAmount: normalizedRetreatAmount,
                   extraSizePadding: extraSizePadding,
                   retreatMode: retreatMode)
        {
        }

        public Drawer(Vec2f position, Vec2f size, Button coverButton,
                      LayoutOrientation direction,
                      IEnumerable<Widget> children,
                              float showDuration = 0f,
                              Interpolation showInterpolation = null,
                              Func<MoveToAction> showActionGetter = null,
                              Action<MoveToAction> onMoveActionCompleted = null,
                              bool applyShowActionToHiding = DEFAULT_APPLY_SHOW_ACTION_TO_HIDING,
                              bool animateParentChildren = DEFAULT_ANIMATE_PARENT_CHILDREN,
                              bool animateParentChildrenWithInterpolation = DEFAULT_ANIMATE_PARENT_CHILDREN_WITH_INTERPOLATION,
                              bool resizeParent = DEFAULT_RESIZE_PARENT,
                              Vec2f? normalizedRetreatAmount = null,
                              Vec2fValue? extraSizePadding = null,
                              RetreatFunction retreatMode = DEFAULT_RETREAT_MODE)
            : base(position, size, tintChildren: false, positionChildren: false, sizeChildren: false)
        {
            this.preExpansionSize = size;

            this.direction = direction;
            this.ShowDuration = showDuration;
            this.ShowInterpolation = showInterpolation;
            this.ShowActionGetter = showActionGetter;
            this.OnMoveActionRemoved = onMoveActionCompleted;
            this.ApplyShowActionToHiding = applyShowActionToHiding;
            this.AnimateParentChildren = animateParentChildren;
            this.AnimateParentChildrenWithInterpolation = animateParentChildrenWithInterpolation;
            this.ResizeParent = resizeParent;

            this.NormalizedRetreatAmount = normalizedRetreatAmount;

            this.ExtraSizePadding = extraSizePadding;

            this.RetreatMode = retreatMode;

            this.resizeParentAction = InitResizeParentAction();

            this.parentResize_IsLayoutEnabled = false;

            layoutAdapter = new PreciseGroupLayoutAdapter();
            layoutAdapter.Disable();

            Adapters.Add(layoutAdapter);

            postExpansionAction = new DelayedAction(PostExpansionAction, 0f);
            //postExpansionAction = new DelayedAction(layoutAdapter.Enable, 0f);

            settingSizeInternally = false;
            settingSizeFromSizeChanged = false;

            this.coverButton = coverButton;

            childExpandablesExpanded = new CompactBoolList(IsEmpty ? 100 : Count);

            AddChild(this.coverButton);

            if (children is not null)
            {
                AddChildRange(children);
            }

            AddCoverButtonListener();

            //CollapseInternal(animate: false);

            layoutAdapter.Enable();
        }

        private void AddCoverButtonListener()
        {
            coverButton.OnCheck += CoverButton_OnCheck;
            coverButton.OnUncheck += CoverButton_OnUncheck;
        }

        private void CoverButton_OnCheck()
        {
            ExpandInternal(animate: false);
        }

        private void CoverButton_OnUncheck()
        {
            CollapseInternal(animate: false);
        }

        /*private void LayoutParent(float moveAmount)
        {
            if (Parent is null || Parent.Count <= 1) return;

            int thisIndex = Parent.IndexOf(this);
            if (thisIndex < 0 || thisIndex == 0) return;

            Vec2f fullMoveAmount = GetDirectionScalars() * moveAmount;

            for (int index = thisIndex + 1; index < Parent.Count; index++)
            {
                Widget sibling = Parent[index];

                MoveChildTo(sibling, sibling.Position + fullMoveAmount,
                            setVisibility: false,
                            animate: true,
                            animateWithInterpolation: false);
            }

            layingOutInternally = true;
            Parent.Layout();
        }*/

        // NEEDS IMPROVED
        private void LayoutParent(Vec2f moveAmount)
        {
            // Greater than one, because if Parent.Count is only 1, then the parent only contains this instance.
            if (Parent is not null && Parent.Count > 1)
            {
                if (TryGetParentPreciseLayoutAdapter(out PreciseGroupLayoutAdapter parentLayoutAdapter))
                {
                    ReadOnlySpan<PreciseGroupLayoutAdapter.WidgetState> parentLayoutStates = parentLayoutAdapter.GetWidgetStates();

                    if (IsExpanded)
                    {
                        for (int index = 0; index < parentLayoutStates.Length; index++)
                        {
                            PreciseGroupLayoutAdapter.WidgetState state = parentLayoutStates[index];

                            if (state.widget == this)
                            {
                                continue;
                            }

                            previousParentPreciseLayoutAdapterBounds.Add(state.Copy());

                            Vec2f newChildParentAbsolutePosition = state.widget.Position;

                            if ((moveAmount.X != 0f && state.widget.Position.X > Position.X) || (moveAmount.Y != 0f && state.widget.Position.Y >= Position.Y))
                            {
                                newChildParentAbsolutePosition += moveAmount;
                            }

                            AABB newParentChildAbsoluteBounds = new AABB
                            {
                                Position = newChildParentAbsolutePosition,
                                Size = state.widget.Size
                            };

                            AABB newParentChildNormalizedBounds = PreciseGroupLayoutAdapter.GetNormalizedBounds(Parent.GetBoundsAABB(), newParentChildAbsoluteBounds);

                            parentLayoutAdapter.TrySetNormalizedBounds(state.widget, newParentChildNormalizedBounds);
                        }
                    }
                    else if (!previousParentPreciseLayoutAdapterBounds.IsEmpty)
                    {
                        for (int index = 0; index < previousParentPreciseLayoutAdapterBounds.Count; index++)
                        {
                            PreciseGroupLayoutAdapter.WidgetState state = previousParentPreciseLayoutAdapterBounds[index];

                            if (state.widget == this)
                            {
                                continue;
                            }

                            if (state.widget.Parent != Parent)
                            {
                                continue;
                            }

                            AABB newParentChildNormalizedBounds = previousParentPreciseLayoutAdapterBounds[index].normalizedBounds;

                            parentLayoutAdapter.TrySetNormalizedBounds(state.widget, newParentChildNormalizedBounds);
                        }

                        previousParentPreciseLayoutAdapterBounds.Clear();
                    }
                }

                Parent.ForEach(delegate (Widget parentChild)
                {
                    if (parentChild == this)
                    {
                        return;
                    }

                    if (DoesSibilingNeedMoved(parentChild, moveAmount, out Vec2f siblingMoveAmount))
                    {
                        MoveChildTo(parentChild, parentChild.Position + siblingMoveAmount,
                                    setVisibility: false,
                                    animate: AnimateParentChildren,
                                    animateWithInterpolation: AnimateParentChildrenWithInterpolation,
                                    onAnimationComplete: null);
                    }
                });

                TrySetParentPreciseLayoutAdapterEnabled(isEnabled: false);

                Parent.Layout();

                TrySetParentPreciseLayoutAdapterEnabled(isEnabled: true);
            }
        }

        private bool DoesSibilingNeedMoved(Widget sibling, Vec2f moveAmount, out Vec2f siblingMoveAmount)
        {
            siblingMoveAmount = Vec2f.Zero;

            if (moveAmount.X != 0f && sibling.Position.X > Position.X)
            {
                siblingMoveAmount.X = moveAmount.X;
            }

            if (moveAmount.Y != 0f && sibling.Position.Y >= Position.Y)
            {
                siblingMoveAmount.Y = moveAmount.Y;
            }

            return !siblingMoveAmount.IsZero();
        }

        private bool TryRevertParentPreciseLayoutAdapterBounds()
        {
            if (!previousParentPreciseLayoutAdapterBounds_This.HasValue)
            {
                return false;
            }

            PreciseGroupLayoutAdapter adapter = Parent.Adapters.FindFirstOfType<PreciseGroupLayoutAdapter>();

            if (adapter is null)
            {
                return false;
            }

            adapter.TrySetNormalizedBounds(this, previousParentPreciseLayoutAdapterBounds_This.Value);

            return true;
        }

        private bool TrySetCurrentParentPreciseLayoutAdapterBounds()
        {
            if (!TryGetParentPreciseLayoutAdapter(out PreciseGroupLayoutAdapter parentAdapter))
            {
                return false;
            }

            parentAdapter.TryGetNormalizedBounds(this, out AABB previousBounds);

            this.previousParentPreciseLayoutAdapterBounds_This = previousBounds;

            AABB currentBounds = PreciseGroupLayoutAdapter.GetNormalizedBounds(Parent, this);

            parentAdapter.TrySetNormalizedBounds(this, currentBounds);

            return true;
        }

        private bool TrySetParentPreciseLayoutAdapterEnabled(bool isEnabled)
        {
            if (!TryGetParentPreciseLayoutAdapter(out PreciseGroupLayoutAdapter parentAdapter))
            {
                return false;
            }

            if (!isEnabled)
            {
                parentAdapter.Disable();
            }
            else
            {
                parentAdapter.Enable();
            }

            return true;
        }

        private bool TryGetParentPreciseLayoutAdapter(out PreciseGroupLayoutAdapter parentAdapter)
        {
            parentAdapter = Parent.Adapters.FindFirstOfType<PreciseGroupLayoutAdapter>();

            if (parentAdapter is null)
            {
                return false;
            }

            return true;
        }

        private void SizeParentBy(Vec2f amount, bool animate)
        {
            if (Parent is null)
            {
                return;
            }

            if (animate)
            {
                resizeParentAction.WidthAmount = amount.X;
                resizeParentAction.HeightAmount = amount.Y;
                resizeParentAction.Duration = ShowDuration;

                Parent.AddAction(resizeParentAction);
            }
            else
            {
                parentResize_IsLayoutEnabled = Parent.IsLayoutEnabled;

                if (parentResize_IsLayoutEnabled)
                {
                    Parent.DisableLayout();
                }

                Parent.Size += amount;

                if (parentResize_IsLayoutEnabled)
                {
                    parentResize_IsLayoutEnabled = false;

                    Parent.EnableLayout();
                }
            }
        }

        public void Expand()
        {
            if (IsExpanded)
            {
                return;
            }

            coverButton.Check();
        }

        public void Collapse()
        {
            if (!IsExpanded)
            {
                return;
            }

            coverButton.Uncheck();
        }

        // Animate controls whether or not animations should be done.
        // It is purely for bookkeeping/state synchronization, where animations are probably undesirable.
        private void ExpandInternal(bool animate)
        {
            isExpanded = true;

            SetDrawerItemsVisible(animate);
        }

        // Animate controls whether or not animations should be done.
        // It is purely for bookkeeping/state synchronization, where animations are probably undesirable.
        public void CollapseInternal(bool animate)
        {
            isExpanded = false;

            for (int index = DRAWER_CONTENT_BEGIN_INDEX; index < Count; index++)
            {
                if (GetUnchecked(index) is IExpandable expandable)
                {
                    expandable.Collapse();
                    childExpandablesExpanded.Set(index, false); // prevent auto-re-expand during parent's expand
                }
            }

            SetDrawerItemsVisible(animate);
        }

        // Animate controls whether or not animations should be done.
        // It is purely for bookkeeping/state synchronization, where animations are probably undesirable.
        private void SetDrawerItemsVisible(bool animate)
        {
            DisableLayoutAdapterAndAddPostExpansionAction();

            Vec2f targetSize = !IsExpanded ? preExpansionSize : GetTargetExpansionSize();

            Vec2f startingSize = Size;

            if (IsExpanded)
            {
                RescaleAdapterNormalizedBounds(Size, targetSize);
            }

            Vec2f direction = GetDirectionScalars();
            Vec2f moveAmount;

            if (IsExpanded)
            {
                moveAmount = direction * (targetSize - Size);

                normalizedMovedAmount = moveAmount / Size;

                preExpansionSize = Size;
            }
            else
            {
                moveAmount = -1f * normalizedMovedAmount * preExpansionSize;
            }

            LayoutChildren(animate, baseSize: targetSize);

            SetSizeInternally(targetSize);

            if (!IsExpanded)
            {
                RescaleAdapterNormalizedBounds(startingSize, targetSize);

                TryRevertParentPreciseLayoutAdapterBounds();
            }
            else
            {
                TrySetCurrentParentPreciseLayoutAdapterBounds();
            }

            LayoutParent(moveAmount);

            if (ResizeParent)
            {
                SizeParentBy(moveAmount, animate);
            }
        }

        private void SetChildrenVisibility()
        {
            for (int index = DRAWER_CONTENT_BEGIN_INDEX; index < Count; index++)
            {
                GetUnchecked(index).IsVisible = IsExpanded;
            }
        }

        private Vec2f GetTargetExpansionSize()
        {
            Utils.Assert(IsExpanded);

            ReadOnlySpan<PreciseGroupLayoutAdapter.WidgetState> layoutStates = layoutAdapter.GetWidgetStates();

            Vec2f max = GetMax();

            for (int index = DRAWER_CONTENT_BEGIN_INDEX; index < layoutStates.Length; index++)
            {
                PreciseGroupLayoutAdapter.WidgetState state = layoutStates[index];

                AABB stateAbsoluteBounds = PreciseGroupLayoutAdapter.GetAbsoluteBounds(this, state);

                Vec2f stateMax = stateAbsoluteBounds.Max();

                if (stateMax.X > max.X)
                {
                    max.X = stateMax.X;
                }

                if (stateMax.Y > max.Y)
                {
                    max.Y = stateMax.Y;
                }
            }

            Vec2f size = max - Position;

            if (ExtraSizePadding.HasValue)
            {
                size += ExtraSizePadding.Value.Compute(size);
            }

            return size;
        }

        private void RescaleAdapterNormalizedBounds(Vec2f oldSize, Vec2f newSize)
        {
            if (oldSize.IsXOrYZero() || newSize.IsXOrYZero())
            {
                return;
            }

            Vec2f scale = oldSize / newSize;

            for (int index = 0; index < Count; index++)
            {
                Widget child = GetUnchecked(index);

                if (layoutAdapter.TryGetNormalizedBounds(child, out AABB bounds))
                {
                    bounds.Position *= scale;
                    bounds.Size *= scale;

                    layoutAdapter.TrySetNormalizedBounds(child, bounds);
                }
            }
        }

        /*private void SetLayoutAdapterChildBounds()
        {
            if (Count <= 1)
            {
                return;
            }

            AABB baseBounds = new AABB
            {
                Position = Position,
                Size = IsExpanded ? preExpansionSize : Size
            };

            for (int index = 0; index < Count; index++)
            {
                Widget child = GetUnchecked(index);

                AABB bounds = PreciseGroupLayoutAdapter.GetNormalizedBounds(baseBounds, child);

                layoutAdapter.TrySetNormalizedBounds(child, bounds);
            }
        }*/

        /*private void SetLayoutAdapterChildBounds(Widget child, Vec2f targetPosition)
        {
            AABB baseBounds = new AABB
            {
                Position = Position,
                Size = IsExpanded ? preExpansionSize : Size
            };

            AABB childBaseBounds = new AABB
            {
                Position = targetPosition,
                Size = child.Size
            };

            AABB childBounds = PreciseGroupLayoutAdapter.GetNormalizedBounds(baseBounds, childBaseBounds);

            layoutAdapter.TrySetNormalizedBounds(child, childBounds);
        }*/

        //baseSize is only used if IsExpanded is true.
        private void LayoutChildren(bool animate, Vec2f? baseSize = null)
        {
            ReadOnlySpan<PreciseGroupLayoutAdapter.WidgetState> layoutStates = layoutAdapter.GetWidgetStates();

            if (IsExpanded)
            {
                AABB baseBounds = new AABB
                {
                    Position = Position,
                    Size = baseSize ?? Size
                };

                for (int index = DRAWER_CONTENT_BEGIN_INDEX; index < Count; index++)
                {
                    Widget child = GetUnchecked(index);

                    Vec2f targetPosition = PreciseGroupLayoutAdapter.GetAbsoluteBounds(baseBounds, layoutStates[index]).Position;

                    MoveChildTo(child, targetPosition,
                                setVisibility: true,
                                animate: animate,
                                animateWithInterpolation: true,
                                onAnimationComplete: null);

                    if (childExpandablesExpanded.Get(index))
                    {
                        ((IExpandable)child).Expand();
                    }
                }
            }
            else
            {
                AABB baseBounds = new AABB
                {
                    Position = Position,
                    Size = preExpansionSize
                };

                for (int index = DRAWER_CONTENT_BEGIN_INDEX; index < Count; index++)
                {
                    Widget child = GetUnchecked(index);

                    AABB childTargetBounds = PreciseGroupLayoutAdapter.GetAbsoluteBounds(baseBounds, layoutStates[index]);

                    Vec2f targetPosition = childTargetBounds.Position + Retreat(child, childTargetBounds.Size);

                    MoveChildTo(child, targetPosition,
                                setVisibility: true,
                                animate: animate,
                                animateWithInterpolation: true,
                                onAnimationComplete: null);

                    if (childExpandablesExpanded.Get(index))
                    {
                        ((IExpandable)child).Expand();
                    }
                }
            }
        }

        private void MoveChildTo(Widget child, Vec2f targetPosition,
                                 bool setVisibility,
                                 bool animate,
                                 bool animateWithInterpolation,
                                 Action onAnimationComplete)
        {
            MoveChildBy(child,
                        targetPosition - child.Position,
                        setVisibility,
                        animate,
                        animateWithInterpolation,
                        onAnimationComplete);
        }

        private void MoveChildBy(Widget child, Vec2f amount,
                                 bool setVisibility,
                                 bool animate,
                                 bool animateWithInterpolation,
                                 Action onAnimationComplete)
        {
            if (ShowActionGetter is not null)
            {
                if (animate && (IsExpanded || ApplyShowActionToHiding))
                {
                    MoveToAction action = ShowActionGetter();

                    action.Restart();
                    action.Alignment = Alignment.None;
                    action.Duration = ShowDuration;
                    action.Interpolation = !animateWithInterpolation ? null : ShowInterpolation;
                    action.EndX = child.Position.X + amount.X;
                    action.EndY = child.Position.Y + amount.Y;

                    void OnActionComplete_Hiding(Widget child)
                    {
                        child.IsVisible = false;

                        onAnimationComplete?.Invoke();
                    }

                    void OnActionRemoved(ActorAction action)
                    {
                        MoveToAction typedAction = (MoveToAction)action;

                        typedAction.OnRemoved -= OnActionRemoved;

                        OnMoveActionRemoved?.Invoke(typedAction);
                    }

                    if (setVisibility)
                    {
                        if (IsExpanded)
                        {
                            child.IsVisible = true;

                            // Cleanup, even if it isn't known if action actually contains OnActionComplete.
                            // This is needed to remove the possibility of this Drawer instance mutating external state of things.
                            action.OnComplete -= OnActionComplete_Hiding;
                        }
                        else
                        {
                            if (ApplyShowActionToHiding)
                            {
                                action.OnComplete += OnActionComplete_Hiding;
                            }
                            else
                            {
                                child.IsVisible = false;
                            }
                        }
                    }

                    action.OnRemoved += OnActionRemoved;

                    child.AddAction(action);
                }
                else
                {
                    child.Position += amount;

                    if (setVisibility)
                    {
                        child.IsVisible = IsExpanded;
                    }
                }
            }
            else
            {
                child.Position += amount;

                if (setVisibility)
                {
                    child.IsVisible = IsExpanded;
                }
            }
        }

        private void DisableLayoutAdapterAndAddPostExpansionAction()
        {
            layoutAdapter.Disable();

            postExpansionAction.Delay = ShowDuration;

            AddAction(postExpansionAction);
        }

        private void SetSizeInternally(Vec2f size)
        {
            settingSizeInternally = true;

            bool isLayoutAdapterEnabled = layoutAdapter.IsEnabled;

            if (isLayoutAdapterEnabled)
            {
                layoutAdapter.Disable();
            }

            Size = size;

            if (isLayoutAdapterEnabled)
            {
                layoutAdapter.Enable();
            }
        }

        public Vec2f GetDirectionScalars()
        {
            return Direction switch
            {
                LayoutOrientation.Horizontal => new Vec2f(1f, 0f),
                LayoutOrientation.Vertical => new Vec2f(0f, 1f),

                _ => throw new InvalidOperationException("Invalid LayoutDirection: " + Convert.ToString(Direction))
            };
        }

        private Vec2f Retreat(Widget child, Vec2f childSize)
        {
            if (!NormalizedRetreatAmount.HasValue)
            {
                return Vec2f.Zero;
            }

            switch (RetreatMode)
            {
                case RetreatFunction.None: return Vec2f.Zero;

                case RetreatFunction.RelativeToDrawer:
                    {
                        Vec2f area = child.Position - Position;

                        return area * NormalizedRetreatAmount.Value;
                    }

                case RetreatFunction.RelativeToChild: return childSize * NormalizedRetreatAmount.Value;

                default:
                    throw new InvalidOperationException("Invalid RetreatFunction: " + Convert.ToString(RetreatMode));
            }
        }

        /*protected override void LayoutInternal(GroupWidget.LayoutArgs layoutArgs)
        {
            base.LayoutInternal(layoutArgs);

            if (layingOutInternally) // Should only be hit when manually laying out Parent.
            {
                layingOutInternally = false;

                return;
            }

            LayoutArgs parentLayoutArgs = LayoutArgs.Create(Parent, this);

            Parent?.Layout(parentLayoutArgs);
        }*/

        private void PostExpansionAction()
        {
            if (layingOutInternally) // Should only be hit when manually laying out Parent.
            {
                layingOutInternally = false;

                return;
            }

            LayoutArgs parentLayoutArgs = LayoutArgs.Create(Parent, this);

            Parent?.Layout(parentLayoutArgs);

            layoutAdapter.Enable();
        }

        protected override void SizeChanged(ref Vec2f previousSize, ref Vec2f newSize)
        {
            // If already internally resizing, do not call base.SizeChanged.
            if (settingSizeInternally)
            {
                settingSizeInternally = false;

                return;
            }

            if (IsExpanded)
            {
                preExpansionSize *= newSize / previousSize;
            }

            // If not empty, then the child sizes will be changed, which will trigger ChildSizeChanged,
            // which will need to not do any work, which would cause a stack overflow.
            if (!IsEmpty)
            {
                settingSizeFromSizeChanged = true;
            }

            // Else, set settingSizeInternally to true, so that if/when children are resized by base.SizeChanged, it doesn't also trigger resizes in this widget.

            base.SizeChanged(ref previousSize, ref newSize);

            if (!IsEmpty)
            {
                settingSizeFromSizeChanged = false;
            }
        }

        protected override void ChildrenChanged(bool endOfRange)
        {
            base.ChildrenChanged(endOfRange);

            if (endOfRange)
            {
                // Sync state as needed.

                SetChildrenVisibility();
            }
        }

        protected override void ChildAdded(Widget child, int index)
        {
            base.ChildAdded(child, index);

            if (child != coverButton)
            {
                //child.OnResize += ChildSizeChanged;

                if (child is IExpandable expandable)
                {
                    childExpandablesExpanded.Add(expandable.IsExpanded);
                }
                else
                {
                    childExpandablesExpanded.Add(false);
                }
            }
            else
            {
                childExpandablesExpanded.Add(false);
            }
        }

        protected override void ChildRemoved(Widget child, int index)
        {
            base.ChildRemoved(child, index);

            /*if (child != coverButton)
            {
                child.OnResize -= ChildSizeChanged;
            }*/

            childExpandablesExpanded.RemoveAt(index);
        }

        /*private void ChildSizeChanged(Vec2f previousSize, Vec2f newSize)
        {
            if (settingSizeFromSizeChanged)
            {
                return;
            }

            if (settingSizeInternally)
            {
                settingSizeInternally = false;

                return;
            }

            SetSizeInternally(Size + (newSize - previousSize));
        }*/

        private SizeByAction InitResizeParentAction()
        {
            SizeByAction action = new SizeByAction();

            action.OnBegin += delegate
            {
                parentResize_IsLayoutEnabled = Parent.IsLayoutEnabled;

                if (parentResize_IsLayoutEnabled)
                {
                    Parent.DisableLayout();
                }
            };

            action.OnComplete += delegate
            {
                if (parentResize_IsLayoutEnabled)
                {
                    parentResize_IsLayoutEnabled = false;

                    Parent.EnableLayout();
                }
            };

            return action;
        }
    }
}