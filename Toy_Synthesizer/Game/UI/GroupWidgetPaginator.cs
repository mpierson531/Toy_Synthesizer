using System;
using System.Collections.Generic;

using FontStashSharp;
using GeoLib;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoGraphics.UI.Actions;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;
using GeoLib.GeoUtils.Pooling;

namespace Toy_Synthesizer.Game.UI
{
    public class GroupWidgetPaginator
    {
        private readonly GroupWidget root;
        private readonly Button backButton;
        private readonly ViewableList<ViewableList<Widget>> pages;

        private readonly Vec2f normalizedBackButtonSpacing;
        private readonly Vec2f normalizedBackButtonRootSpacing;

        private Vec2f lastBackButtonOffset;

        private DynamicSpriteFont newFontNeedsSetValue;

        // All pages share one baseline
        private Vec2f baseRootSize;
        private readonly ViewableList<ViewableList<Vec2f>> pageLocalOffsets;
        private readonly ViewableList<ViewableList<Vec2f>> pageOriginalSizes;

        private readonly Dictionary<Widget, MoveByAction> currentMoveActions;
        private readonly FastPool<MoveByAction> pageMoveActionsPool;
        private readonly PageAnimationFinishedAction pageAnimationFinishedAction;

        private int firstPageIndex;
        private int currentPageIndex;
        private int previousPageIndex;

        private bool isPositioningRoot;

        public int FirstPageIndex
        {
            get => firstPageIndex;
            set => firstPageIndex = value;
        }

        public int CurrentPageIndex => currentPageIndex;

        public bool AnimatePageSwitching { get; set; }
        public PageAnimationData AnimationData { get; set; }

        public GroupWidgetPaginator(GroupWidget root,
                                    Button backButton,
                                    Vec2f normalizedBackButtonSpacing,
                                    Vec2f normalizedBackButtonRootSpacing,
                                    ViewableList<ViewableList<Widget>> pages,
                                    int firstPageIndex,
                                    bool animatePageSwitching,
                                    PageAnimationData animationData)
        {
            this.root = root;
            this.backButton = backButton;
            this.pages = pages;
            this.normalizedBackButtonSpacing = normalizedBackButtonSpacing;
            this.normalizedBackButtonRootSpacing = normalizedBackButtonRootSpacing;
            this.firstPageIndex = firstPageIndex;

            this.AnimatePageSwitching = animatePageSwitching;
            this.AnimationData = animationData;

            int pageMaxWidgetCount = 0;

            for (int index = 0; index < pages.Count; index++)
            {
                ViewableList<Widget> currentPage = pages.GetUnchecked(index);

                if (currentPage.Count > pageMaxWidgetCount)
                {
                    pageMaxWidgetCount = currentPage.Count;
                }
            }

            currentMoveActions = new Dictionary<Widget, MoveByAction>(pageMaxWidgetCount);
            pageMoveActionsPool = new FastPool<MoveByAction>(pageMaxWidgetCount);
            pageAnimationFinishedAction = new PageAnimationFinishedAction()
            {
                paginator = this
            };

            currentPageIndex = -1;
            previousPageIndex = -1;
            isPositioningRoot = false;

            baseRootSize = root.Size;
            pageLocalOffsets = new ViewableList<ViewableList<Vec2f>>(pages.Count);
            pageOriginalSizes = new ViewableList<ViewableList<Vec2f>>(pages.Count);

            // Snapshot of original widget layout.
            for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                ViewableList<Widget> page = pages[pageIndex];

                ViewableList<Vec2f> localOffsetList = new ViewableList<Vec2f>(page.Count);
                ViewableList<Vec2f> sizeList = new ViewableList<Vec2f>(page.Count);

                for (int pageWidgetIndex = 0; pageWidgetIndex < page.Count; pageWidgetIndex++)
                {
                    Widget widget = page.GetUnchecked(pageWidgetIndex);

                    localOffsetList.Add(widget.Position - root.Position);
                    sizeList.Add(widget.Size);
                }

                pageLocalOffsets.Add(localOffsetList);
                pageOriginalSizes.Add(sizeList);
            }

            root.OnReposition += Root_OnReposition;
            root.OnResize += Root_OnResize;

            backButton.OnClick += delegate
            {
                // Sanity check. backButton is only visible and in the UI when currentPageIndex is greater than zero.
                GeoDebug.Assert(currentPageIndex > 0);

                ShowPage(Math.Max(previousPageIndex, 0));
            };
        }

        public void ShowPage(int index, bool allowAnimations = true)
        {
            if (currentPageIndex == index)
            {
                return;
            }

            previousPageIndex = currentPageIndex;
            currentPageIndex = index;

            if ((!AnimatePageSwitching || AnimationData is null) && previousPageIndex != -1)
            {
                root.RemoveChildren(pages[previousPageIndex]);
            }

            if (currentPageIndex > -1 && currentPageIndex != FirstPageIndex)
            {
                if (!root.Parent.Contains(backButton))
                {
                    root.Parent.AddChild(backButton);

                    PositionBackButton();
                    OffsetRootForBackButton();
                }
            }
            else if (currentPageIndex == FirstPageIndex)
            {
                if (root.Parent.Contains(backButton))
                {
                    root.Parent.RemoveChild(backButton);

                    isPositioningRoot = true;

                    root.Position -= lastBackButtonOffset;

                    lastBackButtonOffset = Vec2f.Zero;
                }
            }

            LayoutPage(currentPageIndex: this.currentPageIndex,
                       previousPageIndex: this.previousPageIndex,
                       modifyRoot: true,
                       animationDirection: 1,
                       animate: this.AnimatePageSwitching && allowAnimations,
                       isAnimatingPreviousPage: false);
        }

        private void LayoutPage(int currentPageIndex,
                                int previousPageIndex,
                                bool modifyRoot,
                                int animationDirection,
                                bool animate,
                                bool isAnimatingPreviousPage)
        {
            GeoDebug.Assert(animationDirection == 1 || animationDirection == -1);

            ViewableList<Widget> currentPage = pages[currentPageIndex];

            // Scale from the global baseline.

            Vec2f currentRootPosition = root.Position;
            Vec2f currentRootSize = root.Size;

            Vec2f rootScale = Stage.GetScale(root.Stage, root.ScaleMode, baseRootSize, currentRootSize);

            ViewableList<Vec2f> currentPageLocalOffsets = pageLocalOffsets[currentPageIndex];
            ViewableList<Vec2f> currengPageOriginalSizes = pageOriginalSizes[currentPageIndex];

            Vec2f rootOffsetRaw = AnimationData.NormalizedOffsetFromRoot * currentRootSize;
            Vec2f rootOffset = rootOffsetRaw * animationDirection;

            if (animate && AnimationData is not null)
            {
                currentPage.ForEachWithIndex(delegate (Widget pageWidget, int pageWidgetIndex)
                {
                    if (pageWidgetIndex >= pageLocalOffsets[currentPageIndex].Count)
                    {
                        return;
                    }

                    Vec2f originalLocal = pageLocalOffsets[currentPageIndex][pageWidgetIndex];
                    Vec2f originalSize = pageOriginalSizes[currentPageIndex][pageWidgetIndex];

                    Vec2f targetPosition = currentRootPosition + (originalLocal * rootScale);

                    Vec2f offsetPosition = targetPosition + rootOffset;
                    //Vec2f offsetPosition = targetPosition + (rootOffset - ((targetPosition - currentRootPosition) * AnimationData.NormalizedOffsetFromRoot.Sign().Abs()));

                    if (!isAnimatingPreviousPage && previousPageIndex > -1)
                    {
                        offsetPosition = targetPosition - (rootOffset * (-Math.Sign(currentPageIndex - previousPageIndex)));
                    }
                    else if (isAnimatingPreviousPage && previousPageIndex > -1 && currentPageIndex > previousPageIndex)
                    {
                        offsetPosition = targetPosition - rootOffset;
                    }

                    if (animationDirection == -1)
                    {
                        Utils.Swap(ref targetPosition, ref offsetPosition);
                    }

                    float backButtonOffsetY = (normalizedBackButtonRootSpacing * root.Size).Y;

                    if (isAnimatingPreviousPage)
                    {
                        if (currentPageIndex == 0 && previousPageIndex > 0)
                        {
                            targetPosition.Y -= backButtonOffsetY;
                            offsetPosition.Y -= backButtonOffsetY;
                        }
                        else if (currentPageIndex > 0 && previousPageIndex == 0)
                        {
                            targetPosition.Y += backButtonOffsetY;
                            offsetPosition.Y += backButtonOffsetY;
                        }
                    }

                    pageWidget.Size = originalSize * rootScale;
                    pageWidget.Position = offsetPosition;

                    MoveByAction moveAction;

                    // Check if it already contains a move action.
                    if (currentMoveActions.TryGetValue(pageWidget, out moveAction))
                    {
                        moveAction.Restart();
                    }
                    else
                    {
                        moveAction = pageMoveActionsPool.Get();

                        currentMoveActions.Add(pageWidget, moveAction);

                        moveAction.OnComplete = delegate
                        {
                            pageMoveActionsPool.Return(moveAction);

                            currentMoveActions.Remove(pageWidget);
                        };

                        pageWidget.AddAction(moveAction);
                    }

                    moveAction.AmountX = (targetPosition.X - offsetPosition.X);
                    moveAction.AmountY = (targetPosition.Y - offsetPosition.Y);
                    moveAction.Duration = AnimationData.Duration;
                    moveAction.Interpolation = AnimationData.Interpolation;

                    if (newFontNeedsSetValue is not null)
                    {
                        if (pageWidget is ITextWidget textWidget)
                        {
                            textWidget.Font = newFontNeedsSetValue;
                        }
                        else if (pageWidget is GroupWidget group)
                        {
                            group.ForEachOfType<ITextWidget>(tw => tw.Font = newFontNeedsSetValue, includeThis: false);
                        }
                    }
                });

                if (!isAnimatingPreviousPage)
                {
                    if (root.ContainsAction(pageAnimationFinishedAction))
                    {
                        pageAnimationFinishedAction.Restart();
                    }
                    else
                    {
                        root.AddAction(pageAnimationFinishedAction);
                    }

                    pageAnimationFinishedAction.Duration = AnimationData.Duration;

                    if (previousPageIndex != -1)
                    {
                        pageAnimationFinishedAction.removePreviousPageWhenDone = true;
                        pageAnimationFinishedAction.enableBackButtonWhenDone = currentPageIndex > 0;

                        LayoutPage(currentPageIndex: previousPageIndex,
                                   previousPageIndex: currentPageIndex,
                                   modifyRoot: false,
                                   animationDirection: -1,
                                   animate: animate,
                                   isAnimatingPreviousPage: true);
                    }
                }
            }
            else
            {
                currentPage.ForEachWithIndex(delegate (Widget pageWidget, int pageWidgetIndex)
                {
                    if (pageWidgetIndex >= pageLocalOffsets[this.currentPageIndex].Count)
                    {
                        return;
                    }

                    Vec2f originalLocal = pageLocalOffsets[this.currentPageIndex][pageWidgetIndex];
                    Vec2f originalSize = pageOriginalSizes[this.currentPageIndex][pageWidgetIndex];

                    pageWidget.Position = currentRootPosition + (originalLocal * rootScale);
                    pageWidget.Size = originalSize * rootScale;

                    if (newFontNeedsSetValue is not null)
                    {
                        if (pageWidget is ITextWidget textWidget)
                        {
                            textWidget.Font = newFontNeedsSetValue;
                        }
                        else if (pageWidget is GroupWidget group)
                        {
                            group.ForEachOfType<ITextWidget>(tw => tw.Font = newFontNeedsSetValue, includeThis: false);
                        }
                    }
                });
            }

            if (modifyRoot)
            {
                if (animationDirection == -1)
                {
                    root.RemoveChildren(currentPage);
                }
                else
                {
                    GeoDebug.Assert(animationDirection == 1);

                    root.AddChildRange(currentPage);
                }
            }
        }

        public void SetFont(DynamicSpriteFont font, bool onlyNotShownPages)
        {
            newFontNeedsSetValue = font;
        }

        private void PositionBackButton()
        {
            Vec2f spacing = normalizedBackButtonSpacing * root.Size;

            backButton.Position = root.Position + spacing;
        }

        private void OffsetRootForBackButton()
        {
            Vec2f spacing = normalizedBackButtonRootSpacing * root.Size;

            lastBackButtonOffset = spacing;

            isPositioningRoot = true;

            root.Position += spacing;
        }

        private void Root_OnReposition(Vec2f previousPosition, Vec2f newPosition)
        {
            if (isPositioningRoot)
            {
                isPositioningRoot = false;

                return;
            }

            if (!root.PositionChildren)
            {
                return;
            }

            Vec2f delta = newPosition - previousPosition;

            if (!root.Parent.Contains(backButton))
            {
                backButton.Position += delta;
            }
        }

        private void Root_OnResize(Vec2f previousSize, Vec2f newSize)
        {
            if (!root.SizeChildren)
            {
                return;
            }

            Vec2f scale = Stage.GetScale(root.Stage, root.ScaleMode, previousSize, newSize);

            if (scale.IsOne())
            {
                return;
            }

            if (!root.Parent.Contains(backButton))
            {
                backButton.Size *= scale;
            }

            // This code below is unnecessary, because it is already scaled in ShowPage.
            // This could be used if scaling in ShowPage is removed.
            // Both should have the same end result.
            /*// Update baseRootSize.
            baseRootSize = newSize;

            // Scale original layout data.
            for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                ViewableList<Widget> page = pages[pageIndex];

                ViewableList<Vec2f> pageLocalOffsets = this.pageLocalOffsets[pageIndex];
                ViewableList<Vec2f> pageOriginalSizes = this.pageOriginalSizes[pageIndex];

                for (int pageWidgetIndex = 0; pageWidgetIndex < page.Count; pageWidgetIndex++)
                {
                    pageLocalOffsets[pageWidgetIndex] *= scale;
                    pageOriginalSizes[pageWidgetIndex] *= scale;
                }
            }*/
        }

        private sealed class PageAnimationFinishedAction : TemporalAction
        {
            internal Touchable previousRootTouchMode;
            internal Touchable previousBackButtonTouchMode;
            internal bool removePreviousPageWhenDone;
            internal bool enableBackButtonWhenDone;
            internal GroupWidgetPaginator paginator;

            internal PageAnimationFinishedAction()
            {

            }

            protected override void Update(float percent)
            {
                base.Update(percent);

                if (percent < 1)
                {
                    paginator.root.Layout();
                }
            }

            protected override void Begin()
            {
                base.Begin();

                previousRootTouchMode = paginator.root.TouchMode;

                paginator.root.TouchMode = Touchable.Disabled;

                if (enableBackButtonWhenDone)
                {
                    previousBackButtonTouchMode = paginator.backButton.TouchMode;

                    paginator.backButton.TouchMode = Touchable.Disabled;
                }
            }

            protected override void End()
            {
                base.End();

                paginator.root.TouchMode = previousRootTouchMode;

                if (enableBackButtonWhenDone)
                {
                    enableBackButtonWhenDone = false;

                    paginator.backButton.TouchMode = previousBackButtonTouchMode;
                }

                paginator.root.Layout();

                if (removePreviousPageWhenDone)
                {
                    removePreviousPageWhenDone = false;

                    paginator.root.RemoveChildren(paginator.pages[paginator.previousPageIndex]);
                }
            }
        }

        public class PageAnimationData
        {
            public Vec2f NormalizedOffsetFromRoot; // Normalized to root size.
            public Interpolation Interpolation;
            public float Duration;

            public PageAnimationData(Vec2f normalizedOffsetFromRoot, Interpolation interpolation, float duration)
            {
                NormalizedOffsetFromRoot = normalizedOffsetFromRoot;
                Interpolation = interpolation;
                Duration = duration;
            }
        }
    }
}