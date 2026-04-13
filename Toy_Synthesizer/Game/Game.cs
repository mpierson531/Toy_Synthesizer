using System;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using FontStashSharp;

using MessagePack;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoInput;
using GeoLib.GeoLogging;
using GeoLib.GeoLogging.Loggers;
using GeoLib.GeoMaths;
using GeoLib.GeoSerialization;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;
using GeoLib.GeoUtils.Pooling;

using Toy_Synthesizer.Game.UI;
using Toy_Synthesizer.Game.DigitalSignalProcessing;

namespace Toy_Synthesizer.Game
{
    // TODO: Implement multiple value storage/bit ops in the raw value storage types in CommonUtils.RawValueStorage.
    public sealed class Game : BaseScreen, IInputProcessor
    {
        // This should not interact with setting render targets at all.
        // Just rendering what is passed.
        public abstract class FinalTargetRenderer
        {
            public abstract void Render(Renderer renderer, float delta, RenderTarget2D renderTarget, Color tint);
        }

        public const string Tab = "    ";
        public const string DoubleTab = Tab + Tab;

        public static readonly string NewLine;
        public static readonly string DoubleNewLine;
        public static readonly int NewLineLength;

        public const float BASE_FONT_SIZE = 21f;

        private static Game instance;
        public static Game Instance
        {
            get => instance;
        }

        static Game()
        {
            NewLine = Environment.NewLine;
            DoubleNewLine = NewLine + NewLine;
            NewLineLength = NewLine.Length;

            instance = null;
        }

        private readonly MessagePackSerializerOptions readableSerializerOptions;
        private readonly MessagePackSerializerOptions compactSerializerOptions;

        private AudioBackend audioBackend;

        private Synthesizer.Backend.PolyphonicSynthesizer synthesizer;
        private Synthesizer.Frontend.Frontend synthesizerFrontend;
        private DSP dsp;

        private readonly Stage uiStage;

        private RenderTarget2D uiRenderTarget;
        private RenderTarget2D finalRenderTarget;

        private FontManager fontManager;

        private bool isInitializedInternally;

        public readonly Geo Geo;
        private Renderer renderer;
        private Matrix primitiveProjectionMatrix;
        public UIManager UIManager;
        private SamplerState uiSamplerState;
        private SamplerState uiTextSamplerState;

        private Vec2i targetWindowSize;

        public readonly Color DebugColor = Color.Green;

        private CoreConfig config;
        public CoreConfig Config
        {
            get => config;
        }

        public Renderer Renderer
        {
            get => renderer;
        }

        public Vec2i DisplayDeviceSize
        {
            get => Geo.Display.DeviceSize;
        }

        // THIS WILL NOT CHANGE
        public Vec2i TargetDisplayResolution
        {
            get => new Vec2i(1920, 1080);
        }

        public Vec2i TargetWindowSize
        {
            get => targetWindowSize;
        }

        private Action<Game> onInitialized;

        public Action<Game> OnInitialized
        {
            get => onInitialized;

            set
            {
                if (IsInitialized)
                {
                    throw new InvalidOperationException("Game is already initialized.");
                }

                onInitialized = value;
            }
        }

        public LoggerManager LogManager
        {
            get => Geo.LogManager;
        }

        public FinalTargetRenderer GraphicsTargetRenderer { get; set; }
        public FinalTargetRenderer UITargetRenderer { get; set; }

        public Cursors UICursors
        {
            get => uiStage.Cursors;
            set => uiStage.Cursors = value;
        }

        public string Title
        {
            get => BuildInfo.Name;
        }

        public string Version
        {
            get => BuildInfo.Version;
        }

        public string Description
        {
            get => BuildInfo.Description;
        }

        public string GetConfigRoot()
        {
            return "Config";
        }

        public AudioBackend AudioBackend
        {
            get => audioBackend;
        }

        public Synthesizer.Backend.PolyphonicSynthesizer Synthesizer
        {
            get => synthesizer;
        }

        public Synthesizer.Frontend.Frontend SynthesizerFrontend
        {
            get => synthesizerFrontend;
        }

        public DSP DSP
        {
            get => dsp;
        }

        public Game(Geo geo)
        {
            if (Instance is not null)
            {
                throw new InvalidOperationException("There may be only one Game instance.");
            }

            readableSerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(GeoSerializationResolver.Readable);
            compactSerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(GeoSerializationResolver.Compact);

            uiStage = new Stage(Vec2f.Zero, Vec2f.Zero)
            {
                AllowConcurrency = true,
                AllowMouseOutside = true,

                GlobalTextScaling = true,
                GlobalTextScaleMode = TextScaleMode.PreferWidth,

                DebugColor = DebugColor
            };

            uiStage.Root.PositionChildren = false;
            uiStage.Root.SizeChildren = false;

            uiStage.Root.Adapters.Add(new PreciseGroupLayoutAdapter());

            Geo = geo;

            isInitializedInternally = false;

            instance = this;
        }

        protected override void InitializeInternal()
        {
            InitLogManager();

            MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(GeoLib.GeoSerialization.GeoSerializationResolver.Readable);

            config = CoreConfig.LoadOrDefault(this);

            GraphicsTargetRenderer = null;
            UITargetRenderer = null;

            uiSamplerState = SamplerState.PointClamp;
            uiTextSamplerState = SamplerState.LinearWrap;

            //Geo.IsFixedTimeStep = false;

            Geo.Input.GamepadsEnabled = false;
            Geo.Input.mouse.UseFilters = false;

            renderer = new Renderer(Geo.GraphicsDevice, Geo);

            targetWindowSize = CalculateTargetWindowSize();

            UIManager = new UIManager(this, uiStage);

            uiStage.GlobalLinkStyle = UIManager.GetGlobalTextLinkStyle();

            uiStage.Size = targetWindowSize;

            InitGraphics(TargetWindowSize.X, TargetWindowSize.Y);

            // Do not do exclusive fullscreen.
            // NOTE: This appears to not work in DesktopGL. It will still do fullscreen, but will do exclusive fullscreen regardless of HardwareModeSwitch.
            Geo.Display.Graphics.HardwareModeSwitch = false;

            Geo.Exiting += delegate { Exit(); };

            Geo.Input.Processor = this;

            fontManager = new FontManager(this, fontStepCount: 20);

            AddDisposable(UIManager);

            Geo.Display.Resize(TargetWindowSize);
            isInitializedInternally = true;

            dsp = new DSP(AudioBackend.SAMPLE_RATE);

            audioBackend = new AudioBackend(this, dsp);

            synthesizer = new Synthesizer.Backend.PolyphonicSynthesizer(AudioBackend.SAMPLE_RATE);

            synthesizerFrontend = new Synthesizer.Frontend.Frontend(this);

            dsp.AddAudioSource(synthesizer);

            if (OnInitialized is not null)
            {
                OnInitialized(this);

                OnInitialized = null;
            }

            GC.Collect();
        }

        private void InitLogManager()
        {
            LogManager.ShouldRecordHistory = true;

            // TODO: Only add the console logger in debug builds

            LogManager.AddLogger(ConsoleLogger.Default);
        }

        private void Exit()
        {
            Dispose();
        }

        private Vec2i CalculateTargetWindowSize()
        {
            // Will attempt (display size * 0.5) or (display size / 2), but it deteriorates further and further the lower the display resolution is

            Vec2i displaySize = DisplayDeviceSize;

            Vec2i _1080p = new Vec2i(1920, 1080);
            Vec2i _1440p = new Vec2i(2560, 1440);
            Vec2i _4K = new Vec2i(3840, 2160);

            // three quarters of 1080p
            if (displaySize == _1080p)
            {
                return new Vec2i(1440, 810);
            }

            // returns half of 1440p
            if (displaySize == _1440p)
            {
                return _1440p / 2;
            }

            // returns half of 4K
            if (displaySize == _4K)
            {
                return _1080p;
            }

            return DisplayDeviceSize / 2; // fallback target window size for unmatched display sizes/aspect ratios
        }

        public sealed override void Update(float delta)
        {
            uiStage.Update(delta);
        }

        public sealed override void Draw(float delta)
        {
            Geo.GraphicsDevice.Clear(Color.Black);

            RenderUI(delta);

            FinalRender(delta);
        }

        private void RenderUI(float delta)
        {
            SetRenderTargetAndClear(uiRenderTarget, Color.Transparent);

            uiStage.Draw(renderer, rasterizerState: Geo.DefaultRasterizerState, samplerState: uiSamplerState, textSamplerState: uiTextSamplerState);
        }

        private void FinalRender(float delta)
        {
            SetRenderTargetAndClear(finalRenderTarget, Color.Black);

            if (UITargetRenderer is not null)
            {
                UITargetRenderer.Render(renderer, delta, uiRenderTarget, Config.Game.UITint);
            }
            else
            {
                DefaultRenderTarget(renderer, uiRenderTarget, Config.Game.UITint);
            }

            SetRenderTargetAndClear(null, Color.Black);

            DefaultRenderTarget(renderer, finalRenderTarget, Color.White);
        }

        private void SetRenderTargetAndClear(RenderTarget2D renderTarget, Color clearColor)
        {
            Geo.GraphicsDevice.SetRenderTarget(renderTarget);
            Geo.GraphicsDevice.Clear(clearColor);
        }

        private static void DefaultRenderTarget(Renderer renderer, RenderTarget2D target, Color tint)
        {
            renderer.Begin();

            renderer.Draw(target, Vec2f.Zero, tint);

            renderer.End();
        }

        public float ScaleByDisplayResolution_Min(float value)
        {
            return value * ((Vec2f)DisplayDeviceSize / (Vec2f)TargetDisplayResolution).Min();
        }

        public float ScaleByDisplayResolution_Height(float value)
        {
            return value * ((float)DisplayDeviceSize.Y / (float)TargetDisplayResolution.Y);
        }

        public Vec2f ScaleByDisplayResolution(float x, float y)
        {
            return ScaleByDisplayResolution(new Vec2f(x, y));
        }

        public Vec2f ScaleByDisplayResolution(float value)
        {
            return ScaleByDisplayResolution(new Vec2f(value));
        }

        public Vec2f ScaleByDisplayResolution(Vec2f value)
        {
            return value * ((Vec2f)DisplayDeviceSize / (Vec2f)TargetDisplayResolution);
        }

        public void ForStage(Action<Stage> action)
        {
            action(uiStage);
        }

        public bool IsCursorInUI()
        {
            return uiStage.IsOverAny;
        }

        public void FocusUI(Widget widget, bool fromKey)
        {
            uiStage.Focus(widget, fromKey);
        }

        public void UnfocusUI()
        {
            uiStage.Unfocus(fromKey: false);
        }

        public bool IsUIFocused(Widget widget)
        {
            return uiStage.IsFocused(widget);
        }

        public Widget FocusedUIElement()
        {
            return uiStage.Focused;
        }

        public bool HasUIWidget(Widget widget)
        {
            return uiStage.ContainsDeepSearch(widget);
        }

        public void ShowDialog(DialogBox dialog)
        {
            dialog.Show(uiStage);
        }

        public void AddUIWidget(Widget widget)
        {
            uiStage.AddWidget(widget);
        }

        public void AddUIWidgets<T>(ViewableList<T> widgets) where T : Widget
        {
            uiStage.AddWidgets(widgets);
        }

        public bool RemoveUIWidget(Widget widget)
        {
            return uiStage.RemoveWidget(widget);
        }

        public bool RemoveUIWidgetsOfType<T>()
        {
            return uiStage.Root.RemoveChildrenOfType<T>();
        }

        public void EnableAllUI()
        {
            uiStage.Enable();
        }

        public void DisableAllUI(GroupWidget exception = null)
        {
            uiStage.Disable();

            if (exception is not null)
            {
                exception.GetAscendants().ForEach(ascendant => ascendant.TouchMode = Touchable.ChildrenOnly);

                exception.Enable();
            }
        }

        public void DisableAllUI(Widget[] exceptions)
        {
            uiStage.Disable();

            if (exceptions is not null && exceptions.Length > 0)
            {
                for (int index = 0; index < exceptions.Length; index++)
                {
                    Widget exception = exceptions[index];

                    exception.GetAscendants().ForEach(ascendant => ascendant.TouchMode = Touchable.ChildrenOnly);

                    exception.Enable();
                }
            }
        }

        // If formatMode is Readable, it will be serialized as JSON.
        public bool TrySerializeToConfigRoot<T>(string directory, string path, T data, SerializationFormatMode formatMode,
                                                Func<string> getErrorMessagePrefix = null)
        {
            string configRoot = GetConfigRoot();
            
            directory = Path.Combine(configRoot, directory);
            path = Path.Combine(configRoot, path);

            return TrySerializeTo(directory, path, data, formatMode, getErrorMessagePrefix);
        }

        public bool TrySerializeTo<T>(string directory, string path, T data, SerializationFormatMode formatMode,
                                      Func<string> getErrorMessagePrefix = null)
        {
            try
            {
                byte[] serializedData;

                if (formatMode == SerializationFormatMode.Compact)
                {
                    serializedData = Binary.Serialize(data, compactSerializerOptions);
                }
                else
                {
                    if (formatMode != SerializationFormatMode.Readable)
                    {
                        LogManager.Error(GetErrorMessage($"Bad serialization format: {formatMode}", getErrorMessagePrefix));

                        return false;
                    }

                    string json = Json.PrettyPrintJson(Json.ToJson(data, readableSerializerOptions));

                    serializedData = Encoding.UTF8.GetBytes(json);
                }

                return TryWriteFile(directory, path, serializedData, getErrorMessagePrefix);
            }
            catch (Exception e)
            {
                string errorMessage = GetErrorMessage(e, getErrorMessagePrefix);

                LogManager.Error(errorMessage);

                return false;
            }
        }

        public bool TryDeserializeFromConfigRoot<T>(string path, SerializationFormatMode formatMode,
                                                    out T value,
                                                    Action onFileNotFound = null,
                                                    Func<string> getErrorMessagePrefix = null)
        {
            path = Path.Combine(GetConfigRoot(), path);

            return TryDeserialize(path, formatMode, out value, onFileNotFound, getErrorMessagePrefix);
        }

        public bool TryDeserialize<T>(string path, SerializationFormatMode formatMode,
                                      out T value,
                                      Action onFileNotFound = null,
                                      Func<string> getErrorMessagePrefix = null)
        {
            value = default;

            if (!TryReadFile(path, out byte[] bytes, onFileNotFound, getErrorMessagePrefix))
            {
                return false;
            }

            try
            {
                if (formatMode == SerializationFormatMode.Compact)
                {
                    value = Binary.Deserialize<T>(bytes, compactSerializerOptions);

                    return true;
                }
                else
                {
                    if (formatMode != SerializationFormatMode.Readable)
                    {
                        string error = GetErrorMessage($"Bad serialization format: {formatMode}", getErrorMessagePrefix);

                        LogManager.Error(error);

                        return false;
                    }

                    string json = Encoding.UTF8.GetString(bytes);

                    value = Json.FromJson<T>(json, readableSerializerOptions);

                    return true;
                }
            }
            catch (Exception e)
            {
                LogManager.Error(GetErrorMessage(e, getErrorMessagePrefix));

                return false;
            }
        }

        private bool TryReadFile(string path, out byte[] bytes,
                                 Action onFileNotFound = null,
                                 Func<string> getErrorMessagePrefix = null)
        {
            void OnFail(Exception e)
            {
                string error = GetErrorMessage(e, getErrorMessagePrefix);

                LogManager.Error(error);
            }

            return CommonUtils.FileUtils.Read(path, out bytes, onFileNotFound, OnFail);
        }

        private bool TryWriteFile(string directory, string path, byte[] data,
                                  Func<string> getErrorMessagePrefix = null)
        {
            void OnFail(Exception e)
            {
                string error = GetErrorMessage(e, getErrorMessagePrefix);

                LogManager.Error(error);
            }

            return CommonUtils.FileUtils.Write(directory, path, data,
                                               onFail: OnFail);
        }

        private static string GetErrorMessage(Exception e, Func<string> getErrorMessagePrefix)
        {
            return GetErrorMessage(e.Message, getErrorMessagePrefix);
        }

        private static string GetErrorMessage(string baseMessage, Func<string> getErrorMessagePrefix)
        {
            return getErrorMessagePrefix is not null ? getErrorMessagePrefix() + baseMessage : baseMessage;
        }

        public void BeginInputEvents()
        {
            uiStage.BeginInputEvents();

            SynthesizerFrontend.BeginInputEvents();
        }

        public void EndInputEvents()
        {
            uiStage.EndInputEvents();

            SynthesizerFrontend.EndInputEvents();
        }

        public bool KeyDown(Keys key, bool isRepeat, float holdTime)
        {
            if (uiStage.KeyDown(key, isRepeat, holdTime))
            {
                return true;
            }

            if (SynthesizerFrontend.KeyDown(key, isRepeat, holdTime))
            {
                return true;
            }

            return false;
        }

        public bool KeyUp(Keys key)
        {
            if (uiStage.KeyUp(key))
            {
                return true;
            }

            if (SynthesizerFrontend.KeyUp(key))
            {
                return true;
            }

            return false;
        }

        public bool MouseDown(float x, float y, MouseStates.Button button)
        {
            if (uiStage.MouseDown(x, y, button))
            {
                return true;
            }

            if (SynthesizerFrontend.MouseDown(x, y, button))
            {
                return true;
            }

            return false;
        }

        public bool MouseUp(float x, float y, MouseStates.Button button)
        {
            if (uiStage.MouseUp(x, y, button))
            {
                return true;
            }

            if (SynthesizerFrontend.MouseUp(x, y, button))
            {
                return true;
            }

            return false;
        }

        public bool MouseMoved(float previousX, float previousY, float x, float y)
        {
            if (uiStage.MouseMoved(previousX, previousY, x, y))
            {
                return true;
            }

            if (SynthesizerFrontend.MouseMoved(previousX, previousY, x, y))
            {
                return true;
            }

            return false;
        }

        public bool MouseDragged(float previousX, float previousY, float x, float y, MouseStates.Button button)
        {
            if (uiStage.MouseDragged(previousX, previousY, x, y, button))
            {
                return true;
            }

            if (SynthesizerFrontend.MouseDragged(previousX, previousY, x, y, button))
            {
                return true;
            }

            return false;
        }

        public bool MouseScrolled(float x, float y, int verticalAmount, int horizontalAmount)
        {
            if (uiStage.MouseScrolled(x, y, verticalAmount, horizontalAmount))
            {
                return true;
            }

            if (SynthesizerFrontend.MouseScrolled(x, y, verticalAmount, horizontalAmount))
            {
                return true;
            }

            return false;
        }

        public bool GamePadDown(Buttons button, bool isRepeat, float holdTime)
        {
            if (SynthesizerFrontend.GamePadDown(button, isRepeat, holdTime))
            {
                return true;
            }

            return false;
        }

        public bool GamePadUp(Buttons button)
        {
            if (SynthesizerFrontend.GamePadUp(button))
            {
                return true;
            }

            return false;
        }

        public bool GamePadSticks(bool leftRepeat, bool rightRepeat, float lx, float ly, float rx, float ry)
        {
            if (SynthesizerFrontend.GamePadSticks(leftRepeat, rightRepeat, lx, ly, rx, ry))
            {
                return true;
            }

            return false;
        }

        public bool GamePadTriggers(bool leftRepeat, bool rightRepeat, float left, float right)
        {
            if (SynthesizerFrontend.GamePadTriggers(leftRepeat, rightRepeat, left, right))
            {
                return true;
            }

            return false;
        }

        public override void Resize(int width, int height)
        {
            if (!isInitializedInternally)
            {
                return;
            }

            Vec2i previousSize = Geo.Display.PreviousWindowSize;
            Vec2i newSize = new Vec2i(width, height);

            InitGraphics(newSize.X, newSize.Y);

            GenerateNewFontAndSetStageSize(previousSize, newSize);

            SynthesizerFrontend.WindowResized(width, height);

            GC.Collect();// when resized, slight hitches may already be expected; might as well collect here to minimize memory usage
        }

        private void InitGraphics(int width, int height)
        {
            SetPrimitiveProjectionMatrix(width, height);

            renderer.PrimitiveBatch.Effect.Projection = primitiveProjectionMatrix;

            InitRenderTargets(width, height);
            InitUIManagerUIScalar(width, height);
        }

        private void SetPrimitiveProjectionMatrix(float width, float height)
        {
            primitiveProjectionMatrix = Matrix.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
        }

        // This will scale the size of the stage.
        // It will also determine if a differently-sized font needs to be created or used.
        // If the accumulated window scale is not large enough to use a differently-sized font,
        // it will just let the text widgets scale the text up/down.
        private void GenerateNewFontAndSetStageSize(Vec2i previousSize, Vec2i newSize)
        {
            Vec2f relativeWindowScale = (Vec2f)newSize / (Vec2f)previousSize;

            if (fontManager.GenerateNewFont(previousSize, newSize, out DynamicSpriteFont newFont))
            {
                UIManager.SetFontOfGroup(uiStage.Root, newFont);
            }

            uiStage.Size *= relativeWindowScale;
        }

        public DynamicSpriteFont FindOrCreateFont(float scale)
        {
            return fontManager.FindOrCreateFont(scale);
        }

        private void InitRenderTargets(int width, int height)
        {
            if (uiRenderTarget is not null)
            {
                uiRenderTarget.Dispose();
            }

            if (finalRenderTarget is not null)
            {
                finalRenderTarget.Dispose();
            }

            uiRenderTarget = new RenderTarget2D(Geo.GraphicsDevice, width, height);
            finalRenderTarget = new RenderTarget2D(Geo.GraphicsDevice, width, height);
        }

        private void InitUIManagerUIScalar(int width, int height)
        {
            UIManager.UIScalar = new Vec2f(width, height) / ((Vec2f)TargetDisplayResolution);

            //UIManager.UIScalar = new Vec2f(width, height) / ((Vec2f)TargetWindowSize);
        }

        // This will always handle removing and disposing UI plugins.
        // If there is something specific to a UI plugin that should be unloaded, do it through IUIPugin::UnloadUI
        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            uiRenderTarget.Dispose();
            finalRenderTarget.Dispose();

            uiStage.Dispose();

            audioBackend.Dispose();
        }

        public override void Activate()
        {

        }

        public override void Deactivate()
        {

        }

        public static string GetTabs(int count)
        {
            if (count <= 0)
            {
                return Utils.EmptyString;
            }

            if (count == 1)
            {
                return Tab;
            }

            if (count == 2)
            {
                return DoubleTab;
            }

            PoolableStringBuilder poolable = Pools.Common.StringBuilders.Get();

            for (int index = 0; index != count; index++)
            {
                poolable.Append(Tab);
            }

            string value = poolable.ToString();

            Pools.Common.StringBuilders.Return(poolable);

            return value;
        }

        public static void AppendNewLines(StringBuilder builder, int newLineCount)
        {
            if (newLineCount <= 0)
            {
                return;
            }

            if (newLineCount == 1)
            {
                builder.Append(NewLine);
            }

            if (newLineCount == 2)
            {
                builder.Append(DoubleNewLine);
            }

            for (int index = 0; index != newLineCount; index++)
            {
                builder.Append(NewLine);
            }
        }
    }
}
