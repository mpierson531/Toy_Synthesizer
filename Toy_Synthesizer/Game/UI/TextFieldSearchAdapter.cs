using System;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Input;

using FontStashSharp;

using GeoLib;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoInput;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Actions;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;
using GeoLib.GeoUtils.Providers;

namespace Toy_Synthesizer.Game.UI
{
    public class TextFieldSearchAdapter : WidgetAdapter
    {
        public const bool DEFAULT_ADD_TO_ATTACHED_WIDGET_IF_GROUP = true;
        public const bool DEFAULT_LAYOUT_BASED_ON_ATTACHED_WIDGET = true;
        public const bool DEFAULT_LAYOUT_GROUP_ON_PARENT_RESIZED = true;

        public const int DEFAULT_MIN_COUNT_FOR_PARALLELISM = 100;

        private sealed class SearchDelayAction : TemporalAction
        {
            private TextFieldSearchAdapter searchAdapter;

            public SearchDelayAction(TextFieldSearchAdapter searchAdapter, float delay) : base(delay)
            {
                this.searchAdapter = searchAdapter;
            }

            protected override void Update(float percent)
            {

            }

            protected override void End()
            {
                base.End();

                searchAdapter.Search();
            }
        }

        // Cancel and search button positions/sizes will be based on the text field position/size.

        public GroupWidget Group;
        public Vec2fValue TargetGroupPosition;
        public Vec2fValue TargetGroupSize;
        private Vec2fValue targetPosition;
        private Vec2fValue targetCancelButtonPosition;
        private Vec2fValue targetSearchButtonPosition;
        private Vec2fValue targetSize;
        private Vec2fValue targetCancelButtonSize;
        private Vec2fValue targetSearchButtonSize;

        private IProvider<TextField> fieldProvider;
        private IProvider<Button> cancelButtonProvider;
        private IProvider<Button> searchButtonProvider;

        private IProvider<IObjectStream<string>> searchStreamProvider;
        private ParallelOptions parallelOptions;
        private ViewableList<string> parallelList;
        private readonly object parallelLockObject = new object();

        private GroupWidget attachedGroup;
        private Widget currentlyAttachedWidget;
        private TextField field;
        private Button cancelButton;
        private Button searchButton;
        private bool parentIsStage;
        private DynamicSpriteFont cachedFontToSet;

        private SearchDelayAction searchDelayAction;

        public event Action<TextFieldSearchAdapter> OnShow;
        public event Action<TextFieldSearchAdapter> OnHide;
        public event Action OnSearchBegin;
        public event Action<ReadOnlyMemory<int>> OnSearchFound;
        public event Action OnSearchNotFound;
        public event Action OnReset; // Called when the cancel button is pressed, the escape key is pressed, or the new search text is empty.

        private bool isShowing;
        private bool isSearching;

        private StringComparison stringComparisonType;

        public int MaxParallelism
        {
            get => parallelOptions.MaxDegreeOfParallelism;
            set => parallelOptions.MaxDegreeOfParallelism = value;
        }

        public int MinCountForParallelism { get; set; }

        private InputListener attachedWidgetKeyListener;
        private InputListener fieldEscapeKeyListener;

        private KeyBinding activateKeybind;
        private KeyBinding fieldEscapeKeybind;

        public KeyBinding ActivateKeybind
        {
            get => activateKeybind;
        }

        // If true and the widget this is attached to is a GroupWidget, adds the TextField to the GroupWidget.
        // Else, adds the TextField to the currently attached widget's Stage.
        public bool AddToAttachedWidgetIfGroup { get; set; }
        // If true, normalizes the TextField's position and size based on the currently attached widget.
        // Else, normalizes the bounds based on the currently attached widget's Stage.
        public bool LayoutBasedOnAttachedWidget { get; set; }

        public bool FocusOnSubsequentActivation { get; set; }

        public bool LayoutGroupOnParentResize { get; set; }

        public bool IsShowing
        {
            get => isShowing;
        }

        public bool IsSearching
        {
            get => isSearching;
        }

        public StringComparison StringComparisonType
        {
            get => stringComparisonType;
            set => stringComparisonType = value;
        }

        public TextFieldSearchAdapter(StringComparison stringComparisonType,
                                      Vec2fValue targetPosition,
                                      Vec2fValue targetCancelButtonPosition,
                                      Vec2fValue targetSearchButtonPosition,
                                      Vec2fValue targetSize,
                                      Vec2fValue targetCancelButtonSize,
                                      Vec2fValue targetSearchButtonSize,
                                      IProvider<TextField> fieldProvider,
                                      IProvider<Button> cancelButtonProvider,
                                      IProvider<Button> searchButtonProvider,
                                      IProvider<IObjectStream<string>> searchStreamProvider,
                                      bool layoutBasedOnAttachedWidget = DEFAULT_LAYOUT_BASED_ON_ATTACHED_WIDGET,
                                      bool addToAttachedWidgetIfGroup = DEFAULT_ADD_TO_ATTACHED_WIDGET_IF_GROUP,
                                      Action<TextFieldSearchAdapter> onShow = null,
                                      Action<TextFieldSearchAdapter> onHide = null,
                                      Action onSearchBegin = null,
                                      Action<ReadOnlyMemory<int>> onSearchFound = null,
                                      Action onSearchNotFound = null,
                                      Action onReset = null,
                                      bool focusOnSubsequentActivation = false,
                                      bool layoutGroupOnParentResize = DEFAULT_LAYOUT_GROUP_ON_PARENT_RESIZED,
                                      int? maxParallelism = null,
                                      int minCountForParallelism = DEFAULT_MIN_COUNT_FOR_PARALLELISM)
        {
            this.stringComparisonType = stringComparisonType;

            this.targetPosition = targetPosition;
            this.targetCancelButtonPosition = targetCancelButtonPosition;
            this.targetSearchButtonPosition = targetSearchButtonPosition;
            this.targetSize = targetSize;
            this.targetCancelButtonSize = targetCancelButtonSize;
            this.targetSearchButtonSize = targetSearchButtonSize;

            this.fieldProvider = fieldProvider;
            this.cancelButtonProvider = cancelButtonProvider;
            this.searchButtonProvider = searchButtonProvider;

            this.searchStreamProvider = searchStreamProvider;

            this.OnShow = onShow;
            this.OnHide = onHide;

            this.OnSearchBegin = onSearchBegin;
            this.OnSearchFound = onSearchFound;
            this.OnSearchNotFound = onSearchNotFound;
            this.OnReset = onReset;

            this.FocusOnSubsequentActivation = focusOnSubsequentActivation;

            this.LayoutGroupOnParentResize = layoutGroupOnParentResize;

            this.parallelOptions = new ParallelOptions();

            this.MaxParallelism = maxParallelism ?? Environment.ProcessorCount;
            this.MinCountForParallelism = minCountForParallelism;

            parallelList = new ViewableList<string>(MinCountForParallelism);

            AddToAttachedWidgetIfGroup = addToAttachedWidgetIfGroup;
            LayoutBasedOnAttachedWidget = layoutBasedOnAttachedWidget;

            this.isShowing = false;

            InitListenersAndEvents();

            InitActivateKeybind();
            InitFieldEscapeKeybind();

            searchDelayAction = new SearchDelayAction(this, 0.5f);
        }

        public bool HasSearch()
        {
            return field is not null && !field.IsDefaultTextActive;
        }

        public bool Check(string value)
        {
            return ValueContainsSearch(value, field.Text, StringComparisonType);
        }

        private void Search()
        {
            OnSearchBegin?.Invoke();

            IObjectStream<string> stream = searchStreamProvider.Get();

            int streamLength = stream.Length;

            string searchString = field.IsDefaultTextActive ? TextUtils.EmptyString : field.Text;

            if (searchString is null)
            {
                throw new InvalidOperationException("Error! searchString should never be null!");
            }

            // searchString should not be null, just checking for emptiness.
            if (searchString.Length == 0)
            {
                OnReset?.Invoke();

                return;
            }

            SearchInternal(searchString, stream, streamLength);
        }

        private void SearchInternal(string searchString, IObjectStream<string> stream, int streamLength)
        {
            ViewableList<int> resultsList = Pools.GetObject<ViewableList<int>>();
            resultsList.Reserve(streamLength);

            if (streamLength >= MinCountForParallelism)
            {
                parallelList.Reserve(streamLength);

                for (int index = 0; index < streamLength; index++)
                {
                    parallelList.AddUnchecked(stream.Next());
                }

                SearchRawParallel(searchString, StringComparisonType, parallelList, streamLength, resultsList, parallelOptions);

                parallelList.Clear();
            }
            else
            {
                SearchRaw(searchString, StringComparisonType, stream, streamLength, resultsList);
            }

            ReadOnlyMemory<int> results = resultsList.ToReadonlyMemory();

            Pools.ReturnObject(resultsList);

            SearchFinished(results);
        }

        private void SearchRawParallel(string searchString, 
                                       StringComparison stringComparisonType, 
                                       ViewableList<string> values, 
                                       int count, 
                                       ViewableList<int> resultsList, 
                                       ParallelOptions parallelOptions)
        {
            Parallel.For(0, count, parallelOptions, delegate (int index)
            {
                string value = values.GetUnchecked(index);

                if (ValueContainsSearch(value, searchString, stringComparisonType))
                {
                    lock (parallelLockObject)
                    {
                        resultsList.Add(index);
                    }
                }
            });
        }

        // If I add lowercase/uppercase comparisons, I'll probably need to make this an instance method.
        private static void SearchRaw(string searchString, StringComparison stringComparisonType, IObjectStream<string> stream, int streamLength, ViewableList<int> resultsList)
        {
            for (int index = 0; index < streamLength; index++)
            {
                string value = stream.Next();

                if (ValueContainsSearch(value, searchString, stringComparisonType))
                {
                    resultsList.Add(index);
                }
            }
        }

        // If I add lowercase/uppercase comparisons, I'll probably need to make this an instance method.
        private static bool ValueContainsSearch(string value, string search, StringComparison stringComparisonType)
        {
            return value.Contains(search, stringComparisonType);
        }

        private void SearchFinished(ReadOnlyMemory<int> results)
        {
            if (results.IsEmpty)
            {
                OnSearchNotFound?.Invoke();

                return;
            }

            OnSearchFound?.Invoke(results);
        }

        public void Clear(bool focusField)
        {
            field.Text = TextUtils.EmptyString;

            Search();

            if (focusField && isShowing)
            {
                FocusSearchField();
            }
        }

        public void Activate()
        {
            if (isShowing)
            {
                if (FocusOnSubsequentActivation)
                {
                    FocusSearchField();
                }

                return;
            }

            field = fieldProvider.Get();

            Utils.RequireNotNull(field);

            attachedGroup = GetGroupToAttachTo();

            if (attachedGroup == Group)
            {
                currentlyAttachedWidget.Stage.AddWidget(Group);
            }
            else if (attachedGroup == currentlyAttachedWidget.Stage.Root)
            {
                parentIsStage = true;
            }

            attachedGroup.OnParentChanged += AttachedGroup_OnParentChanged;
            attachedGroup.Parent.OnResize += AttachedGroupParent_OnResize;

            AABB emptyAABB = new AABB();

            bool cancelButtonIsValid = TryInitCancelButton(ref emptyAABB, layout: false);
            bool searchButtonIsValid = TryInitSearchButton(ref emptyAABB, layout: false);

            LayoutWidgets(fromParentResize: false);

            attachedGroup.AddChild(field);

            if (cancelButtonIsValid)
            {
                attachedGroup.AddChild(cancelButton);
            }

            if (cancelButtonIsValid)
            {
                attachedGroup.AddChild(searchButton);
            }

            TryAddFieldEventsAndListeners();
            TryAddButtonEvents();

            FocusSearchField();

            SetFontIfNeedsSet();

            isShowing = true;

            OnShow?.Invoke(this);
        }

        public void Deactivate()
        {
            if (!isShowing)
            {
                return;
            }

            attachedGroup.RemoveChild(field);

            if (cancelButton is not null)
            {
                attachedGroup.RemoveChild(cancelButton);
            }

            if (searchButton is not null)
            {
                attachedGroup.RemoveChild(searchButton);
            }

            if (parentIsStage)
            {
                parentIsStage = false;
            }

            attachedGroup.Parent.OnResize -= AttachedGroupParent_OnResize;

            if (attachedGroup == Group)
            {
                currentlyAttachedWidget.Stage.RemoveWidget(Group);
            }

            attachedGroup.OnParentChanged -= AttachedGroup_OnParentChanged;

            attachedGroup = null;

            // This will force default text to activate in the text field.
            if (!field.IsDefaultTextActive)
            {
                field.Text = TextUtils.EmptyString;
            }

            TryRemoveFieldEventsAndListeners();
            TryRemoveButtonEvents();

            isShowing = false;

            OnReset?.Invoke();

            OnHide?.Invoke(this);

            field = null;
            cancelButton = null;
            searchButton = null;
        }

        private GroupWidget GetGroupToAttachTo()
        {
            if (Group is not null)
            {
                return Group;
            }

            if (AddToAttachedWidgetIfGroup && currentlyAttachedWidget is GroupWidget group)
            {
                return group;
            }

            return currentlyAttachedWidget.Stage.Root;
        }

        public void SetFont(DynamicSpriteFont font)
        {
            cachedFontToSet = font;
        }

        private void SetFontIfNeedsSet()
        {
            if (cachedFontToSet is null)
            {
                return;
            }

            GroupWidget groupToAttachTo = GetGroupToAttachTo();

            if (groupToAttachTo == Group)
            {
                Group.SetFontOfTextWidgets(cachedFontToSet);
            }

            if (groupToAttachTo != Group || !isShowing)
            {
                field.Font = cachedFontToSet;

                if (searchButton is ITextWidget searchButtonTextWidget)
                {
                    searchButtonTextWidget.Font = cachedFontToSet;
                }

                if (cancelButton is ITextWidget cancelButtonTextWidget)
                {
                    cancelButtonTextWidget.Font = cachedFontToSet;
                }
            }

            cachedFontToSet = null;
        }

        private void FocusSearchField()
        {
            field.Stage.Focus(field, fromKey: false);
        }

        private void AttachedGroup_OnParentChanged(GroupWidget previousParent, GroupWidget currentParent)
        {
            if (previousParent is not null)
            {
                previousParent.OnResize -= AttachedGroupParent_OnResize;
            }

            if (currentParent is not null)
            {
                currentParent.OnResize += AttachedGroupParent_OnResize;
            }
        }

        private void AttachedGroupParent_OnResize(Vec2f previousSize, Vec2f newSize)
        {
            LayoutWidgets(fromParentResize: true);
        }

        private void LayoutWidgets(bool fromParentResize)
        {
            bool layoutGroup;

            if (fromParentResize)
            {
                GeoDebug.Assert(isShowing);

                layoutGroup = attachedGroup == Group && LayoutGroupOnParentResize;
            }
            else
            {
                layoutGroup = attachedGroup == Group;
            }

            AABB baseBounds;

            if (LayoutBasedOnAttachedWidget)
            {
                baseBounds = currentlyAttachedWidget.GetBoundsAABB();
            }
            else
            {
                baseBounds = currentlyAttachedWidget.Stage.Root.GetBoundsAABB();
            }

            if (attachedGroup == Group)
            {
                if (layoutGroup)
                {
                    Group.Position = baseBounds.Position + TargetGroupPosition.Compute(baseBounds.Size);
                    Group.Size = TargetGroupSize.Compute(baseBounds.Size);
                }

                baseBounds = Group.GetBoundsAABB();
            }

            // If attachedGroup is a Window, its children will be offset by the title bar height.
            // This needs to be accounted for while laying out the search widgets.
            if (fromParentResize && attachedGroup is Window window)
            {
                baseBounds.Position.Y += window.GetTitleBarSize().Y;
            }

            field.Size = targetSize.Compute(baseBounds.Size);
            field.Position = baseBounds.Position + (targetPosition.Compute(baseBounds.Size));

            LayoutCancelButton(ref baseBounds);
            LayoutSearchButton(ref baseBounds);
        }

        private bool TryInitCancelButton(ref AABB baseBounds, bool layout)
        {
            if (cancelButtonProvider is null)
            {
                return false;
            }

            cancelButton = cancelButtonProvider.Get();

            Utils.RequireNotNull(cancelButton);

            if (layout)
            {
                LayoutCancelButton(ref baseBounds);
            }

            return true;
        }

        private bool TryInitSearchButton(ref AABB baseBounds, bool layout)
        {
            if (searchButtonProvider is null)
            {
                return false;
            }

            searchButton = searchButtonProvider.Get();

            Utils.RequireNotNull(searchButton);

            if (layout)
            {
                LayoutSearchButton(ref baseBounds);
            }

            return true;
        }

        private void LayoutCancelButton(ref AABB baseBounds)
        {
            if (cancelButton is null)
            {
                return;
            }

            cancelButton.Size = targetCancelButtonSize.Compute(baseBounds.Size);
            cancelButton.Position = baseBounds.Position + (targetCancelButtonPosition.Compute(baseBounds.Size));
        }

        private void LayoutSearchButton(ref AABB baseBounds)
        {
            if (searchButton is null)
            {
                return;
            }

            searchButton.Size = targetSearchButtonSize.Compute(baseBounds.Size);
            searchButton.Position = baseBounds.Position + (targetSearchButtonPosition.Compute(baseBounds.Size));
        }

        private void TryAddFieldEventsAndListeners()
        {
            // Only do the automatic search if there is no search button.

            if (searchButton is null)
            {
                field.OnTextInput += Field_OnTextInput_AutomaticSearch;
            }

            field.AddListener(fieldEscapeKeyListener);
        }

        private void TryRemoveFieldEventsAndListeners()
        {
            // Only do the automatic search if there is no search button.

            if (searchButton is null)
            {
                field.OnTextInput -= Field_OnTextInput_AutomaticSearch;
            }

            field.RemoveListener(fieldEscapeKeyListener);
        }

        private void TryAddButtonEvents()
        {
            if (cancelButton is not null)
            {
                cancelButton.OnClick += CancelButton_OnClick;
            }

            if (searchButton is not null)
            {
                searchButton.OnClick += SearchButton_OnClick;
            }
        }

        private void TryRemoveButtonEvents()
        {
            if (cancelButton is not null)
            {
                cancelButton.OnClick -= CancelButton_OnClick;
            }

            if (searchButton is not null)
            {
                searchButton.OnClick -= SearchButton_OnClick;
            }
        }

        private void Field_OnTextInput_AutomaticSearch(string _)
        {
            if (searchDelayAction.HasBegun)
            {
                searchDelayAction.Restart();
            }
            else
            {
                field.AddAction(searchDelayAction);
            }
        }

        private void CancelButton_OnClick()
        {
            Deactivate();
        }

        private void SearchButton_OnClick()
        {
            Search();
        }

        private void AddAdapterListeners()
        {
            currentlyAttachedWidget.AddListener(attachedWidgetKeyListener);
        }

        private void RemoveAdapterListeners()
        {
            currentlyAttachedWidget.AddListener(attachedWidgetKeyListener);
        }

        private void InitListenersAndEvents()
        {
            attachedWidgetKeyListener = new InputListener()
            {
                KeyDown = delegate (InputEvent inputEvent, Keys key)
                {
                    if (activateKeybind.IsPressed(Geo.Instance.Input.keyboard, key))
                    {
                        if (isShowing && !FocusOnSubsequentActivation)
                        {
                            Deactivate();
                        }
                        else
                        {
                            Activate();
                        }

                        inputEvent.HandleAndStop();
                    }
                }
            };

            fieldEscapeKeyListener = new InputListener()
            {
                KeyDown = delegate (InputEvent inputEvent, Keys key)
                {
                    if (fieldEscapeKeybind.IsPressed(Geo.Instance.Input.keyboard, key))
                    {
                        GeoDebug.Assert(isShowing);

                        Deactivate();

                        inputEvent.HandleAndStop();
                    }
                }
            };
        }

        private void InitActivateKeybind()
        {
            KeyBinding.Modifier[] modifiers = new KeyBinding.Modifier[]
            {
                new KeyBinding.Modifier(Keys.LeftControl, isOptional: true),
                new KeyBinding.Modifier(Keys.RightControl, isOptional: true),
            };

            KeyBinding.Key[] keys = new KeyBinding.Key[]
            {
                new KeyBinding.Key(Keys.F, PressMode.JustPressed)
            };

            activateKeybind = new KeyBinding(modifiers, keys);
        }

        private void InitFieldEscapeKeybind()
        {
            KeyBinding.Modifier[] modifiers = Array.Empty<KeyBinding.Modifier>();

            KeyBinding.Key[] keys = new KeyBinding.Key[]
            {
                new KeyBinding.Key(Keys.Escape, PressMode.JustPressed)
            };

            fieldEscapeKeybind = new KeyBinding(modifiers, keys);
        }

        private void AddAllListenersAndEvents()
        {
            AddAdapterListeners();
        }

        private void RemoveAllListenersAndEvents()
        {
            RemoveAdapterListeners();
        }

        protected override void Attached(Widget widget)
        {
            this.currentlyAttachedWidget = widget;

            AddAllListenersAndEvents();
        }

        protected override void Removed(Widget widget)
        {
            RemoveAllListenersAndEvents();

            this.currentlyAttachedWidget = null;
        }

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            if (currentlyAttachedWidget is not null)
            {
                RemoveAllListenersAndEvents();
            }
        }
    }
}
