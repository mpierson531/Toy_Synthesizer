using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using FontStashSharp;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.WidgetAdapters;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoSerialization;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;
using GeoLib.GeoUtils.Collections.Trees;
using GeoLib.GIO.Files;

namespace Toy_Synthesizer.Game.UI
{
    public class SaveLoadDialog : DialogBox
    {
        private MetadataGetterDelegate metadataGetter;
        private SaveGroupNameLabelGetterDelegate saveGroupNameLabelGetter;
        private SaveGroupLastModDateLabelGetterDelegate saveGroupLastModDateLabelGetter;
        private SaveGroupAdditionalMetadataLabelGetterDelegate saveGroupAdditionalMetadataLabelGetter;

        private Action<ButtonAdapter> onSaveGroupDoubleClickButtonAdapterAdded;

        private Button okButton;
        private Button noButton;
        private Button cancelButton;
        private ScrollPane scrollPane;
        private TextWidget saveAsNameLabel;
        private TextField nameInputField;

        private SaveGroupStyle saveGroupStyle;

        public FloatValue ScrollPanePositionY { get; set; } //  Based on Size.
        public FloatValue VerticalGroupSpacing { get; set; } // Based on the scrollpane size.
        public Vec2fValue OkButtonPosition { get; set; } // Based on Size.
        public Vec2fValue CancelButtonPosition { get; set; } // Based on Size.
        public Vec2fValue ButtonSize { get; set; } // Based on Size. Will be adjusted based on the amount of buttons.
        public FloatValue HorizontalButtonSpacing { get; set; } // Based on Size.
        public Vec2fValue NameInputLabelPosition { get; set; } // Based on Size.
        public Vec2fValue NameInputFieldPosition { get; set; } // Based on Size.
        public Vec2fValue NameInputFieldSize { get; set; }  // Based on Size.
        public Vec2fValue EdgeSpacing { get; set; } // Based on Size.

        private DialogType currentDialogType;
        private Action<SaveGroupWidget> currentSaveAsAndLoadOnGroupDoubleClick;

        private int selectedSaveIndex = -1;

        private ViewableList<SaveGroupWidget> saveGroupWidgetPool;

        private ViewableList<FileTree.FileData> currentlyShownSaves;

        private readonly FileTree savesFileTree;

        private string currentRoot;
        private string root;
        private FileConfiguration fileConfig;

        public string Root
        {
            get => root;

            set
            {
                root = value;

                ArgumentNullException.ThrowIfNull(root, nameof(root));
            }
        }

        public FileConfiguration FileConfig
        {
            get => fileConfig;

            set
            {
                fileConfig = value;

                ArgumentNullException.ThrowIfNull(fileConfig, nameof(fileConfig));
            }
        }

        public string CurrentRoot
        {
            get => currentRoot;
        }

        // The purpose of this is to allow greater control of what Stage this is added to.
        // This will invoked when one of the Show*X* methods is called, or when an action within wants to show again.
        // This should not be null.
        // This will not be invoked (at least directly) if DialogBox.Show is called.
        public event Action<SaveLoadDialog> OnShowRequested;
        public event SaveRequestedDelegate OnSaveRequested;
        public event SaveBeforeLoadRequestedDelegate OnSaveBeforeLoadRequested;
        public event LoadRequestedDelegate OnLoadRequested;

        public event Action<string> OnError;

        public event Action<SaveGroupWidget> OnSaveGroupInitialized;

        public SaveLoadDialog(Vec2f size,
                              WindowStyle style,
                              DynamicSpriteFont font,
                              bool isModal,
                              bool nonModalHideOnUnfocus,
                              float borderThickness,
                              float titleBarHeight,
                              float closeButtonWidth,
                              Action<SaveLoadDialog> onShowRequested,
                              SaveRequestedDelegate onSaveRequested,
                              SaveBeforeLoadRequestedDelegate onSaveBeforeLoadRequested,
                              LoadRequestedDelegate onLoadRequested,
                              Action<string> onError,
                              Action<SaveGroupWidget> onSaveGroupInitialized,
                              WindowBehaviorFlags behavior,
                              Button okButton,
                              Button noButton,
                              Button cancelButton,
                              ScrollPane scrollPane,
                              TextWidget saveAsNameLabel,
                              TextField nameInputField,
                              MetadataGetterDelegate metadataGetter,
                              SaveGroupNameLabelGetterDelegate saveGroupNameLabelGetter,
                              SaveGroupLastModDateLabelGetterDelegate saveGroupLastModDateLabelGetter,
                              SaveGroupAdditionalMetadataLabelGetterDelegate saveGroupAdditionalMetadataLabelGetter,
                              Action<ButtonAdapter> onSaveGroupDoubleClickButtonAdapterAdded,
                              SaveGroupStyle saveGroupStyle,
                              FloatValue scrollPanePositionY,
                              FloatValue verticalGroupSpacing,
                              Vec2fValue okButtonPosition,
                              Vec2fValue cancelButtonPosition,
                              Vec2fValue buttonSize,
                              FloatValue horizontalButtonSpacing,
                              Vec2fValue nameInputLabelPosition,
                              Vec2fValue nameInputFieldPosition,
                              Vec2fValue nameInputFieldSize,
                              Vec2fValue edgeSpacing,
                              string root,
                              FileConfiguration fileConfig)
            : base(size: size,
                   style: style,
                   font: font,
                   title: TextUtils.EmptyString,
                   borderThickness: borderThickness,
                   titleBarHeight: titleBarHeight,
                   isModal: isModal,
                   nonModalHideOnUnfocus: nonModalHideOnUnfocus,
                   closeButtonWidth: closeButtonWidth,
                   behavior: behavior)
        {
            this.OnShowRequested = onShowRequested;
            this.OnSaveRequested = onSaveRequested;
            this.OnSaveBeforeLoadRequested = onSaveBeforeLoadRequested;
            this.OnLoadRequested = onLoadRequested;
            this.OnError = onError;
            this.OnSaveGroupInitialized = onSaveGroupInitialized;

            this.okButton = okButton;
            this.noButton = noButton;
            this.cancelButton = cancelButton;
            this.scrollPane = scrollPane;
            this.saveAsNameLabel = saveAsNameLabel;
            this.nameInputField = nameInputField;
            this.metadataGetter = metadataGetter;
            this.saveGroupNameLabelGetter = saveGroupNameLabelGetter;
            this.saveGroupLastModDateLabelGetter = saveGroupLastModDateLabelGetter;
            this.saveGroupAdditionalMetadataLabelGetter = saveGroupAdditionalMetadataLabelGetter;
            this.onSaveGroupDoubleClickButtonAdapterAdded = onSaveGroupDoubleClickButtonAdapterAdded;
            this.saveGroupStyle = saveGroupStyle;
            ScrollPanePositionY = scrollPanePositionY;
            VerticalGroupSpacing = verticalGroupSpacing;
            OkButtonPosition = okButtonPosition;
            CancelButtonPosition = cancelButtonPosition;
            ButtonSize = buttonSize;
            HorizontalButtonSpacing = horizontalButtonSpacing;
            NameInputLabelPosition = nameInputLabelPosition;
            NameInputFieldPosition = nameInputFieldPosition;
            NameInputFieldSize = nameInputFieldSize;
            EdgeSpacing = edgeSpacing;
            this.root = root;
            this.fileConfig = fileConfig;
        }

        public void ShowSaveAsDialog()
        {
            ShowDialog(DialogType.SaveAs);
        }

        public void ShowLoadDialog()
        {
            ShowDialog(DialogType.Load);
        }

        private void ShowDialog(DialogType type)
        {
            ValidateDialogType(type);

            SetCurrentDialogType(type);

            switch (type)
            {
                case DialogType.SaveAs:
                    SaveAsDialogInit();
                    break;

                case DialogType.Load:
                    LoadDialogInit();
                    break;

                case DialogType.AskSaveBeforeLoad:
                    AskSaveBeforeLoadDialogInit();
                    break;
            }

            // Common buttons
            AddChild(okButton);
            AddChild(cancelButton);

            OnShowRequested(this);
        }

        protected override void OnShowInternal()
        {
            base.OnShowInternal();

            switch (currentDialogType)
            {
                case DialogType.SaveAs: 
                case DialogType.Load:
                    OnShow_SaveAsAndLoadCommon(); 
                    break;

                case DialogType.AskSaveBeforeLoad: 
                    OnShow_AskSaveBeforeLoad(); 
                    break;

                default: throw new InvalidOperationException("Bad DialogType: " + currentDialogType);
            }
        }

        protected override void OnHideInternal()
        {
            base.OnHideInternal();

            switch (currentDialogType)
            {
                case DialogType.SaveAs:
                case DialogType.Load:
                    OnHide_SaveAsAndLoadCommon(); 
                    break;

                case DialogType.AskSaveBeforeLoad: 
                    OnHide_AskSaveBeforeLoad(); 
                    break;

                default: throw new InvalidOperationException("Bad DialogType: " + currentDialogType);
            }
        }

        private void SetCurrentDialogTypeToNone()
        {
            SetCurrentDialogType(DialogType.None);
        }

        private void SetCurrentDialogType(DialogType type)
        {
            this.currentDialogType = type;
        }

        private void SaveAsDialogInit()
        {
            Title = "Save";

            void OkButton_OnClick()
            {
                SaveAsFromInput();

                Hide();
            }

            void Group_OnDoubleClick(SaveGroupWidget saveGroup)
            {
                nameInputField.Text = saveGroup.saveMetadata.Name;

                OkButton_OnClick();
            }

            okButton.OnClick = OkButton_OnClick;

            SaveAsAndLoadDialogCommonInit(isLoad: false, onGroupDoubleClick: Group_OnDoubleClick);
        }

        private void LoadDialogInit()
        {
            Title = "Load";

            void OkButton_OnClick()
            {
                //LoadSelectedSave();

                Hide();

                ShowDialog(DialogType.AskSaveBeforeLoad);

                scrollPane.Disable();
            }

            void Group_OnDoubleClick(SaveGroupWidget saveGroup)
            {
                OkButton_OnClick();
            }

            okButton.OnClick = OkButton_OnClick;

            cancelButton.OnClick = delegate
            {
                Hide();

                CheckIfScrollPaneNeedsEnabling();

                PoolSaveGroupWidgets();

                SelectedSaveCleanup();

                ClearShownSaves();

                SetNoButton_OnClick_Common();
            };

            SaveAsAndLoadDialogCommonInit(isLoad: true, onGroupDoubleClick: Group_OnDoubleClick);
        }

        private void AskSaveBeforeLoadDialogInit()
        {
            Title = "Save First?";

            void OkButton_OnClick()
            {
                OnSaveBeforeLoadRequested();

                LoadSelectedSave();

                Hide();
            }

            okButton.OnClick = OkButton_OnClick;

            noButton.OnClick = delegate
            {
                LoadSelectedSave();

                CheckIfScrollPaneNeedsEnabling();

                Hide();

                SetNoButton_OnClick_Common();
            };

            OnResize += delegate (Vec2f previousSize, Vec2f newSize)
            {
                SetButtonsBounds(withNoButton: true);
            };
        }

        private void SaveAsAndLoadDialogCommonInit(bool isLoad, Action<SaveGroupWidget> onGroupDoubleClick)
        {
            currentSaveAsAndLoadOnGroupDoubleClick = onGroupDoubleClick;

            OnResize += delegate (Vec2f previousSize, Vec2f newSize)
            {
                SetButtonsBounds(withNoButton: false);

                if (currentDialogType != DialogType.Load)
                {
                    SaveAs_SetNameInputBounds();
                }
            };

            if (!isLoad)
            {
                cancelButton.OnClick = Hide;

                AddChild(saveAsNameLabel);
                AddChild(nameInputField);
            }
        }

        private void OnShow_SaveAsAndLoadCommon()
        {
            SetButtonsBounds(withNoButton: false);

            currentRoot = Root;

            if (currentDialogType != DialogType.Load)
            {
                SaveAs_SetNameInputBounds();
            }

            SetIOScrollPaneBounds();

            GetSaves(currentlyShownSaves);

            Vec2f groupBasePosition = scrollPane.GetViewportPosition();

            if (!currentlyShownSaves.IsEmpty)
            {
                Vec2f edgeSpacing = GetEdgeSpacing();
                float verticalGroupSpacing = GetVerticalGroupSpacing();

                Vec2f groupPosition = new Vec2f
                (
                    groupBasePosition.X + edgeSpacing.X,
                    groupBasePosition.Y + edgeSpacing.Y
                );

                Vec2f groupSize = GetSaveGroupSize();

                for (int index = 0; index < currentlyShownSaves.Count; index++)
                {
                    ref FileTree.FileData save = ref currentlyShownSaves.GetRefUnchecked(index);

                    string metadataPath = save.FullName;

                    try
                    {
                        SaveGroupWidget saveGroup = GetSaveGroup(groupPosition, groupSize, metadataPath, index, currentSaveAsAndLoadOnGroupDoubleClick);

                        scrollPane.AddChild(saveGroup);

                        groupPosition.Y += groupSize.Y + verticalGroupSpacing;
                    }
                    catch (Exception e)
                    {
                        OnError?.Invoke("Error while displaying saves: " + e.Message);
                    }
                }

                scrollPane.Layout();
            }
        }

        private void OnHide_SaveAsAndLoadCommon()
        {
            SetNameInputTextToEmpty();

            RemoveChild(saveAsNameLabel);
            RemoveChild(nameInputField);

            PoolSaveGroupWidgets();

            SelectedSaveCleanup();

            ClearShownSaves();

            SetCurrentDialogTypeToNone();

            currentSaveAsAndLoadOnGroupDoubleClick = null;
        }

        private void OnShow_AskSaveBeforeLoad()
        {
            SetButtonsBounds(withNoButton: true);

            AddChild(noButton);
        }

        private void OnHide_AskSaveBeforeLoad()
        {
            CheckIfScrollPaneNeedsEnabling();

            PoolSaveGroupWidgets();

            SelectedSaveCleanup();

            ClearShownSaves();

            RemoveChild(noButton);

            SetCurrentDialogTypeToNone();
        }

        private void SaveAs_SetNameInputBounds()
        {
            saveAsNameLabel.Size = GetInputNameLabelSize();
            saveAsNameLabel.Position = GetInputNameLabelPosition();

            nameInputField.Size = GetNameInputFieldSize();
            nameInputField.Position = GetNameInputFieldPosition();
            nameInputField.FontScale = saveAsNameLabel.FontScale; // Force correct font scale.
        }

        private void SetNoButton_OnClick_Common()
        {
            noButton.OnClick = Hide;

            noButton.OnClick += CheckIfScrollPaneNeedsEnabling;
        }

        private void CheckIfScrollPaneNeedsEnabling()
        {
            if (scrollPane.TouchMode != Touchable.Enabled)
            {
                scrollPane.Enable();
            }
        }

        private SaveGroupWidget GetSaveGroup(Vec2f position, Vec2f size, string path, int index, Action<SaveGroupWidget> onDoubleClick)
        {
            SaveMetadata metadata = metadataGetter(path);

            SaveGroupWidget group;

            if (!saveGroupWidgetPool.IsEmpty)
            {
                group = saveGroupWidgetPool.Pop();

                group.Set(index: index,
                          path: path,
                          metadata: metadata,
                          position: position,
                          size: size);
            }
            else
            {
                group = new SaveGroupWidget(this,
                                            position: position,
                                            size: size,
                                            index: index,
                                            style: saveGroupStyle,
                                            path: path,
                                            saveMetadata: metadata,
                                            onDoubleClick: onDoubleClick,
                                            onDoubleClickButtonAdapterAdded: onSaveGroupDoubleClickButtonAdapterAdded)
                {
                    Origin = new Vec2f(0f, 0.5f)
                };
            }

            OnSaveGroupInitialized?.Invoke(group);

            return group;
        }

        private void PoolSaveGroupWidgets()
        {
            int index = 0;

            while (index < scrollPane.Count)
            {
                Widget widget = scrollPane.GetUnchecked(index);

                if (widget is SaveGroupWidget saveGroupWidget)
                {
                    saveGroupWidgetPool.Add(saveGroupWidget);

                    scrollPane.RemoveChildAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private Vec2f GetIOScrollPanePosition()
        {
            float yOffset = ScrollPanePositionY.Compute(Size.Y);

            return new Vec2f(Position.X, Position.Y + yOffset);
        }

        private Vec2f GetIOScrollPaneSize()
        {
            float verticalSpacing = GetIOScrollPanePosition().Y - Position.Y;

            Vec2f saveAsInputPosition = GetInputNameLabelPosition();

            return new Vec2f(Size.X, MathF.Abs(saveAsInputPosition.Y - scrollPane.Position.Y - verticalSpacing));
        }

        private Vec2f GetSaveGroupSize()
        {
            Vec2f scale = new Vec2f(0.9f, 0.2f);

            return new Vec2f((scrollPane.Size - scrollPane.ComputeScrollbarTrackSize()) * scale);
        }

        private float GetVerticalGroupSpacing()
        {
            return VerticalGroupSpacing.Compute(scrollPane.Size.Y);
        }

        private Vec2f GetNameInputFieldPosition()
        {
            float widgetSpacingX = GetHorizontalButtonSpacing();

            float x = saveAsNameLabel.GetMaxX() + widgetSpacingX;
            float y = saveAsNameLabel.Position.Y;

            return new Vec2f(x, y);
        }

        private Vec2f GetNameInputFieldSize()
        {
            Vec2f edgeSpacing = GetEdgeSpacing();

            float x = GetNameInputFieldPosition().X;

            float width = GetMaxX() - edgeSpacing.X - x;
            float height = saveAsNameLabel.Size.Y;

            return new Vec2f(width, height);
        }

        private Vec2f GetInputNameLabelPosition()
        {
            return Position + NameInputLabelPosition.Compute(Size);
        }

        private Vec2f GetInputNameLabelSize()
        {
            return Size * 0.05f;
        }

        private void SetButtonsBounds(bool withNoButton)
        {
            Vec2f buttonSize = GetDialogButtonsSize(withNoButton);
            Vec2f edgeSpacing = GetEdgeSpacing();
            float horizontalButtonSpacing = GetHorizontalButtonSpacing();

            float buttonY = GetMaxY() - buttonSize.Y - edgeSpacing.Y;

            okButton.Size = buttonSize;
            cancelButton.Size = buttonSize;

            okButton.Position = new Vec2f(Position.X + edgeSpacing.X, buttonY);

            if (withNoButton)
            {
                noButton.Position = new Vec2f(okButton.Position.X + buttonSize.X + horizontalButtonSpacing, buttonY);
                noButton.Size = buttonSize;

                cancelButton.Position = new Vec2f(noButton.Position.X + buttonSize.X + horizontalButtonSpacing, buttonY);
            }
            else
            {
                cancelButton.Position = new Vec2f(okButton.Position.X + buttonSize.X + horizontalButtonSpacing, buttonY);
            }
        }

        private void SetIOScrollPaneBounds()
        {
            scrollPane.Position = GetIOScrollPanePosition();
            scrollPane.Size = GetIOScrollPaneSize();
        }

        private Vec2f GetDialogButtonsSize(bool withNoButton)
        {
            float buttonsSpacingX = GetHorizontalButtonSpacing();

            Vec2f buttonSize = ButtonSize.Compute(Size);

            if (withNoButton)
            {
                buttonSize.X = (buttonSize.X * 0.33f) + (buttonsSpacingX * 0.33f);
            }
            else
            {
                buttonSize.X *= 0.5f;
            }

            buttonSize.X -= buttonsSpacingX;

            return buttonSize;
        }

        private Vec2f GetEdgeSpacing()
        {
            return EdgeSpacing.Compute(Size);
        }

        private float GetHorizontalButtonSpacing()
        {
            return HorizontalButtonSpacing.Compute(Size.X);
        }

        private void SetNameInputTextToEmpty()
        {
            nameInputField.Text = TextUtils.EmptyString;
        }

        private void SaveAsFromInput()
        {
            Save(nameInputField.Text);
        }

        private void Save(string name)
        {
            string path = Path.Combine(CurrentRoot, name);

            OnSaveRequested(path);
        }

        private void LoadSelectedSave()
        {
            FileTree.FileData fileData = currentlyShownSaves.GetUnchecked(selectedSaveIndex);

            OnLoadRequested(fileData.FullName);
        }

        private void ClearShownSaves()
        {
            currentlyShownSaves.Clear();
        }

        private void SelectedSaveCleanup()
        {
            selectedSaveIndex = -1;
        }

        private void GetSaves(ViewableList<FileTree.FileData> saves)
        {
            ValidateFileConfigFileMode();

            // Make sure the root is set and force a rebuild of the file tree.
            savesFileTree.SetRoot(CurrentRoot);

            saves.Reserve(savesFileTree.Count);

            savesFileTree.ForEachNode(delegate (CompactNAryTree<FileTree.FileData>.Node node, int childCount)
            {
                if (childCount == 0)
                {
                    return;
                }

                FileTree.FileData fileData = node.Value;

                // If childCount is not zero, the fileData should always be a directory.
                // But doing this just to make sure.
                Utils.Assert(fileData.IsDirectory, "A non-directory was found to have children!");

                void Node_ForEachChild_Directories(FileTree.FileData childFileData)
                {
                    if (FilterFileData_Directories(fileData))
                    {
                        saves.Add(childFileData);
                    }
                }

                void Node_ForEachChild_Files(FileTree.FileData childFileData)
                {
                    if (FilterFileData_Files(childFileData))
                    {
                        saves.Add(childFileData);
                    }
                }

                node.ForEachChild(FileConfig.FileMode == FileMode.File ? Node_ForEachChild_Files : Node_ForEachChild_Directories);
            });
        }

        private void ValidateFileConfigFileMode()
        {
            if (FileConfig.FileMode != FileMode.File && FileConfig.FileMode != FileMode.Directory)
            {
                throw new InvalidOperationException("FileConfig.FileMode is invalid: " + FileConfig.FileMode.ToString());
            }
        }

        // Returns true if the allowed file extensions and name filters are null or empty.
        private bool FilterFileData_Files(FileTree.FileData fileData)
        {
            if (fileData.IsDirectory)
            {
                return false;
            }

            return FilterFileDataExtensions(fileData) && FilterFileDataNames(fileData);
        }

        private bool FilterFileData_Directories(FileTree.FileData fileData)
        {
            if (fileData.IsFile)
            {
                return false;
            }

            return FilterFileDataNames(fileData);
        }

        // Returns true if the name filters is null or empty.
        private bool FilterFileDataNames(FileTree.FileData fileData)
        {
            if (ArrayUtils.IsNullOrEmpty(FileConfig.NameFilters))
            {
                return true;
            }

            for (int index = 0; index < FileConfig.NameFilters.Length; index++)
            {
                NameFilter nameFilter = FileConfig.NameFilters[index];

                if (fileData.Name.Contains(nameFilter.Name, nameFilter.ComparisonMode))
                {
                    return true;
                }
            }

            return false;
        }

        // Returns true if the allowed extensions is null or empty.
        private bool FilterFileDataExtensions(FileTree.FileData fileData)
        {
            if (ArrayUtils.IsNullOrEmpty(FileConfig.File_AllowedExtensions))
            {
                return true;
            }

            for (int index = 0; index < FileConfig.File_AllowedExtensions.Length; index++)
            {
                if (fileData.Extension.Equals(FileConfig.File_AllowedExtensions[index], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateDialogType(DialogType dialogType)
        {
            if (dialogType != DialogType.SaveAs
                && dialogType != DialogType.Load
                && dialogType != DialogType.AskSaveBeforeLoad)
            {
                throw new InvalidOperationException("Invalid DialogType: " + Convert.ToString(dialogType));
            }
        }

        public sealed class SaveGroupWidget : GroupWidget
        {
            private readonly SaveLoadDialog dialog;

            private string path;

            internal SaveMetadata saveMetadata;

            private TextWidget nameLabel;
            private TextWidget modificationDateLabel;
            private readonly ViewableList<TextWidget> additionalMetadataLabels;
            private ButtonAdapter buttonAdapter;

            private Action<SaveGroupWidget> onDoubleClick;

            internal bool isSelected = false;

            public ButtonAdapter ButtonAdapter
            {
                get => buttonAdapter;
            }

            public int Index { get; set; }

            public SaveGroupWidget(SaveLoadDialog dialog, Vec2f position, Vec2f size, int index, SaveGroupStyle style,
                                   string path, SaveMetadata saveMetadata,
                                   Action<SaveGroupWidget> onDoubleClick,
                                   Action<ButtonAdapter> onDoubleClickButtonAdapterAdded)
                : base(position, size,
                       positionChildren: false,
                       sizeChildren: false,
                       tintChildren: true,
                       scaleChildren: true)
            {
                this.path = path;
                this.saveMetadata = saveMetadata;
                this.additionalMetadataLabels = new ViewableList<TextWidget>();
                this.dialog = dialog;

                this.Index = index;

                this.onDoubleClick = onDoubleClick;

                CreateLabels();
                SetLabelsText();

                CreateButtonAdapter(style, onDoubleClickButtonAdapterAdded);
            }

            public void Set(int index,
                            string path, SaveMetadata metadata,
                            Vec2f position, Vec2f size)
            {
                this.path = path;
                this.saveMetadata = metadata;

                this.Index = index;

                CurrentRenderData = buttonAdapter.Style.Normal;

                Position = position;
                Size = size;

                SetLabelsText();

                InitLabels();
            }

            private void SetLabelsText()
            {
                SetName();
                SetModificationDate();

                if (saveMetadata.AdditionalMetadata is not null)
                {
                    for (int index = 0; index < saveMetadata.AdditionalMetadata.Count; index++)
                    {
                        additionalMetadataLabels.GetUnchecked(index).Text = saveMetadata.AdditionalMetadata.GetUnchecked(index).Metadata;
                    }
                }
            }

            private void SetName()
            {
                nameLabel.Text = saveMetadata.Name;
            }

            private void SetModificationDate()
            {
                modificationDateLabel.Text = saveMetadata.LastModificationDate;
            }

            private void CreateLabels()
            {
                Vec2f namePosition = Position + (Size * saveMetadata.NameLabelPositionScalar);
                Vec2f nameSize = Size * saveMetadata.NameLabelSizeScalar;
                Vec2f modificationDatePosition = Position + (Size * saveMetadata.LastModificationDateLabelPositionScalar);
                Vec2f modificationDateSize = Size * saveMetadata.LastModificationDateLabelSizeScalar;

                nameLabel = dialog.saveGroupNameLabelGetter(namePosition, nameSize);
                modificationDateLabel = dialog.saveGroupLastModDateLabelGetter(modificationDatePosition, modificationDateSize);

                if (saveMetadata.AdditionalMetadata is not null)
                {
                    for (int index = 0; index < saveMetadata.AdditionalMetadata.Count; index++)
                    {
                        AdditionalSaveMetadataUIData metadata = saveMetadata.AdditionalMetadata.GetUnchecked(index);

                        Vec2f position = Position + (Size * metadata.PositionScalar);
                        Vec2f size = Size * metadata.SizeScalar;

                        TextWidget label = dialog.saveGroupAdditionalMetadataLabelGetter(position, size);

                        additionalMetadataLabels.Add(label);
                    }
                }

                InitLabels();

                AddChild(nameLabel);
                AddChild(modificationDateLabel);

                if (saveMetadata.AdditionalMetadata is not null)
                {
                    for (int index = 0; index < saveMetadata.AdditionalMetadata.Count; index++)
                    {
                        AddChild(additionalMetadataLabels.GetUnchecked(index));
                    }
                }
            }

            private void InitLabels()
            {
                nameLabel.FitText = false;
                modificationDateLabel.FitText = false;

                nameLabel.ScaleTextOnResize = true;
                modificationDateLabel.ScaleTextOnResize = true;

                modificationDateLabel.FontScale = 1f;

                nameLabel.TouchMode = Touchable.Disabled;
                modificationDateLabel.TouchMode = Touchable.Disabled;

                if (saveMetadata.AdditionalMetadata is not null)
                {
                    for (int index = 0; index < saveMetadata.AdditionalMetadata.Count; index++)
                    {
                        TextWidget label = additionalMetadataLabels.GetUnchecked(index);

                        label.FitText = false;
                        label.ScaleTextOnResize = true;
                        label.FontScale = 1f;
                        label.TouchMode = Touchable.Disabled;
                    }
                }

                SetLabelBounds();
            }

            private void CreateButtonAdapter(SaveGroupStyle style, Action<ButtonAdapter> onDoubleClickButtonAdapterAdded)
            {
                buttonAdapter = new ButtonAdapter(style)
                {
                    RenderDataGetter = delegate (ButtonAdapter adapter, Widget widget)
                    {
                        if (dialog.selectedSaveIndex == Index)
                        {
                            return style.Selected;
                        }

                        return null;
                    }
                };

                buttonAdapter.Listener.OnClick = delegate
                {
                    if (dialog.selectedSaveIndex != Index)
                    {
                        if (dialog.selectedSaveIndex != -1)
                        {
                            Parent.GetUnchecked(dialog.selectedSaveIndex).CurrentRenderData = style.Normal;
                        }

                        dialog.selectedSaveIndex = Index;

                        CurrentRenderData = style.Selected;
                    }
                    else if (onDoubleClick is not null && buttonAdapter.Listener.IsDoubleClicked)
                    {
                        onDoubleClick(this);
                    }
                };

                Adapters.Add(buttonAdapter);

                onDoubleClickButtonAdapterAdded?.Invoke(buttonAdapter);
            }

            private void SetLabelBounds()
            {
                nameLabel.Position = Position + (Size * saveMetadata.NameLabelPositionScalar);
                modificationDateLabel.Position = Position + (Size * saveMetadata.LastModificationDateLabelPositionScalar);

                nameLabel.Size = Size * saveMetadata.NameLabelSizeScalar;
                modificationDateLabel.Size = Size * saveMetadata.LastModificationDateLabelSizeScalar;

                if (saveMetadata.AdditionalMetadata is not null)
                {
                    for (int index = 0; index < saveMetadata.AdditionalMetadata.Count; index++)
                    {
                        AdditionalSaveMetadataUIData metadata = saveMetadata.AdditionalMetadata.GetUnchecked(index);

                        Vec2f position = Position + (Size * metadata.PositionScalar);
                        Vec2f size = Size * metadata.SizeScalar;

                        TextWidget label = additionalMetadataLabels.GetUnchecked(index);

                        label.Position = position;
                        label.Size = size;
                    }
                }
            }

            protected override void PositionChanged(ref Vec2f previousPosition, ref Vec2f newPosition)
            {
                base.PositionChanged(ref previousPosition, ref newPosition);

                Layout();
            }

            protected override void SizeChanged(ref Vec2f previousSize, ref Vec2f newSize)
            {
                base.SizeChanged(ref previousSize, ref newSize);

                Layout();
            }

            protected override void LayoutInternal(GroupWidget.LayoutArgs layoutArgs)
            {
                base.LayoutInternal(layoutArgs);

                SetLabelBounds();
            }
        }

        public delegate SaveMetadata MetadataGetterDelegate(string path);

        public delegate void SaveRequestedDelegate(string path);
        public delegate void SaveBeforeLoadRequestedDelegate();
        public delegate void LoadRequestedDelegate(string path);

        public delegate TextWidget SaveGroupNameLabelGetterDelegate(Vec2f position, Vec2f size);
        public delegate TextWidget SaveGroupLastModDateLabelGetterDelegate(Vec2f position, Vec2f size);
        public delegate TextWidget SaveGroupAdditionalMetadataLabelGetterDelegate(Vec2f position, Vec2f size);

        public enum DialogType
        {
            None,

            SaveAs,
            Load,

            AskSaveBeforeLoad,
        }

        public class FileConfiguration : ICopyable
        {
            public FileMode FileMode;
            public string[] File_AllowedExtensions;
            public NameFilter[] NameFilters;

            public FileConfiguration(FileMode fileMode, 
                                     string[] file_AllowedExtensions = null,
                                     NameFilter[] nameFilters = null)
            {
                FileMode = fileMode;
                File_AllowedExtensions = file_AllowedExtensions;
                NameFilters = nameFilters;
            }

            public FileConfiguration Copy(bool deepCopy)
            {
                string[] file_AllowedExtensions = deepCopy ? ArrayUtils.ArrayCopy(File_AllowedExtensions) : File_AllowedExtensions;
                NameFilter[] nameFilters = deepCopy ? ArrayUtils.ArrayCopy(NameFilters) : NameFilters;

                return new FileConfiguration(FileMode, file_AllowedExtensions, nameFilters);
            }

            object ICopyable.Copy(bool deepCopy)
            {
                return Copy(deepCopy);
            }
        }

        public enum FileMode
        {
            Directory,
            File
        }

        public class NameFilter
        {
            public string Name;
            public StringComparison ComparisonMode;

            public NameFilter(string name, StringComparison comparisonMode)
            {
                Name = name;
                ComparisonMode = comparisonMode;
            }
        }

        public sealed class SaveGroupStyle : Button.ButtonStyle
        {
            public RenderData Selected { get; set; }

            public override object Copy(bool deepCopy)
            {
                SaveGroupStyle style = base.CopyAs<SaveGroupStyle>(deepCopy);

                style.Selected = RenderData.Copy(Selected, deepCopy);

                return style;
            }
        }

        public class SaveMetadata
        {
            public string Name;
            public string LastModificationDate;

            public Vec2f NameLabelPositionScalar;
            public Vec2f NameLabelSizeScalar;
            public Vec2f LastModificationDateLabelPositionScalar;
            public Vec2f LastModificationDateLabelSizeScalar;

            public ViewableList<AdditionalSaveMetadataUIData> AdditionalMetadata;

            public SaveMetadata(string name, 
                                string lastModificationDate, 
                                Vec2f nameLabelPositionScalar, 
                                Vec2f nameLabelSizeScalar, 
                                Vec2f lastModificationDateLabelPositionScalar, 
                                Vec2f lastModificationDateLabelSizeScalar,
                                ViewableList<AdditionalSaveMetadataUIData> additionalMetadata)
            {
                Name = name;
                LastModificationDate = lastModificationDate;
                NameLabelPositionScalar = nameLabelPositionScalar;
                NameLabelSizeScalar = nameLabelSizeScalar;
                LastModificationDateLabelPositionScalar = lastModificationDateLabelPositionScalar;
                LastModificationDateLabelSizeScalar = lastModificationDateLabelSizeScalar;
                AdditionalMetadata = additionalMetadata;
            }
        }

        public class AdditionalSaveMetadataUIData
        {
            public Vec2f PositionScalar;
            public Vec2f SizeScalar;
            public string Metadata;

            public AdditionalSaveMetadataUIData(Vec2f positionScalar, Vec2f sizeScalar, string metadata)
            {
                PositionScalar = positionScalar;
                SizeScalar = sizeScalar;
                Metadata = metadata;
            }

            public AdditionalSaveMetadataUIData()
            {
                PositionScalar = Vec2f.Zero;
                SizeScalar = Vec2f.Zero;
                Metadata = null;
            }
        }
    }
}
