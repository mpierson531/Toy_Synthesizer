using System;
using System.IO;

using GeoLib;
using GeoLib.GeoSerialization;

using Microsoft.Xna.Framework;

namespace Toy_Synthesizer.Game
{
    [GenerateFormatter(AddToSerializationFormatterCache = true)]
    public sealed class CoreConfig
    {
        public static readonly string ConfigRoot = "CoreConfig";
        public static readonly string ConfigFileName = "core_config.txt";
        public static readonly string ConfigPath = Path.Combine(ConfigRoot, ConfigFileName);

        public static CoreConfig LoadOrDefault(Game game)
        {
            CoreConfig config;

            void OnFileOrDirectoryNotFound()
            {
                game.LogManager.Info("No core config file found.");
            }

            static string GetErrorMessagePrefix()
            {
                return "Error while loading core config: ";
            }

            game.Geo.LogManager.Info("Loading core config.");

            if (game.TryDeserializeFromConfigRoot(ConfigPath, SerializationFormatMode.Readable, out config,
                                                  onFileNotFound: OnFileOrDirectoryNotFound,
                                                  getErrorMessagePrefix: GetErrorMessagePrefix))
            {
                config.game = game;

                game.Geo.LogManager.Info("Core config loaded.");
            }
            else
            {
                config = new CoreConfig();
                config.game = game;

                game.Geo.LogManager.Info("Using default core config.");

                config.Save();
            }

            return config;
        }

        private Game game;

        [SerializableProperty]
        public EngineConfig Engine;
        [SerializableProperty]
        public GameConfig Game;

        public CoreConfig()
        {
            Engine = new EngineConfig();
            Game = new GameConfig();
        }

        public CoreConfig(EngineConfig engine, GameConfig game)
        {
            this.Engine = engine;
            this.Game = game;
        }

        public void Save()
        {
            static string GetErrorMessagePrefix()
            {
                return "Error while saving core config: ";
            }

            game.TrySerializeToConfigRoot(ConfigRoot, ConfigPath, this, SerializationFormatMode.Readable,
                                          getErrorMessagePrefix: GetErrorMessagePrefix);
        }

        [GenerateFormatter(AddToSerializationFormatterCache = true)]
        public sealed class EngineConfig
        {
            public static readonly int DefaultFramerateLimit = 60;
            public static readonly bool DefaultFullscreen = false;

            private int framerateLimit;
            private bool fullscreen;
            [SerializableProperty]
            public int FramerateLimit
            {
                get => framerateLimit;

                set
                {
                    if (OnFramerateLimitChanged is not null)
                    {
                        int previous = framerateLimit;
                        framerateLimit = value;

                        OnFramerateLimitChanged(previous, value);
                    }
                    else
                    {
                        framerateLimit = value;
                    }
                }
            }

            [SerializableProperty]
            public bool Fullscreen
            {
                get => fullscreen;

                set
                {
                    if (OnFullscreenChanged is not null)
                    {
                        bool previous = fullscreen;
                        fullscreen = value;

                        OnFullscreenChanged(previous, fullscreen);
                    }
                    else
                    {
                        fullscreen = value;
                    }
                }
            }

            public Action<int, int> OnFramerateLimitChanged { get; set; }
            public Action<bool, bool> OnFullscreenChanged { get; set; }

            public EngineConfig()
            {
                framerateLimit = DefaultFramerateLimit;
            }

            public EngineConfig(int framerateLimit, bool fullScreen)
            {
                this.framerateLimit = framerateLimit;
                this.fullscreen = fullScreen;
            }
        }

        [GenerateFormatter(AddToSerializationFormatterCache = true)]
        public sealed class GameConfig
        {
            public static readonly Color DefaultGraphicsTint = Color.White;
            public static readonly float DefaultGraphicsBrightness = 1f;
            public static readonly Color DefaultUITint = Color.White;
            public static readonly float DefaultUIBrightness = 1f;
            public static readonly int DefaultCharacterSpacing = 0;

            public static readonly float MinBrightness = 0.2f;
            public static readonly Color MinBrightnessColor = new Color(MinBrightness, MinBrightness, MinBrightness, 1f);

            private Color graphicsTint;
            private Color baseGraphicsTint;
            private float graphicsBrightness;

            private Color uiTint;
            private Color baseUITint;
            private float uiBrightness;

            private int globalCharacterSpacing;

            [SerializableProperty]
            public Color GraphicsTint
            {
                get => graphicsTint;

                set
                {
                    if (OnGraphicsTintChanged is not null)
                    {
                        Color previous = graphicsTint;

                        graphicsTint = value;
                        baseGraphicsTint = value;

                        SetGraphicsTintFromBrightness(revertFirst: false);

                        OnGraphicsTintChanged(previous, value);
                    }
                    else
                    {
                        graphicsTint = value;
                        baseGraphicsTint = value;

                        SetGraphicsTintFromBrightness(revertFirst: false);
                    }
                }
            }

            [SerializableProperty]
            public float GraphicsBrightness
            {
                get => graphicsBrightness;

                set
                {
                    if (OnGraphicsBrightnessChanged is not null)
                    {
                        float previous = graphicsBrightness;
                        graphicsBrightness = value;

                        SetGraphicsTintFromBrightness(revertFirst: true);

                        OnGraphicsBrightnessChanged(previous, value);
                    }
                    else
                    {
                        graphicsBrightness = value;

                        SetGraphicsTintFromBrightness(revertFirst: true);
                    }
                }
            }

            [SerializableProperty]
            public Color UITint
            {
                get => uiTint;

                set
                {
                    if (OnUITintChanged is not null)
                    {
                        Color previous = uiTint;

                        uiTint = value;
                        baseUITint = value;

                        SetUITintFromBrightness(revertFirst: false);

                        OnUITintChanged(previous, value);
                    }
                    else
                    {
                        uiTint = value;
                        baseUITint = value;

                        SetUITintFromBrightness(revertFirst: false);
                    }
                }
            }

            [SerializableProperty]
            public float UIBrightness
            {
                get => uiBrightness;

                set
                {
                    if (OnUIBrightnessChanged is not null)
                    {
                        float previous = uiBrightness;
                        uiBrightness = value;

                        SetUITintFromBrightness(revertFirst: true);

                        OnUIBrightnessChanged(previous, value);
                    }
                    else
                    {
                        uiBrightness = value;

                        SetUITintFromBrightness(revertFirst: true);
                    }
                }
            }

            [SerializableProperty]
            public int GlobalCharacterSpacing
            {
                get => globalCharacterSpacing;

                set
                {
                    if (OnCharacterSpacingChanged is not null)
                    {
                        int previous = globalCharacterSpacing;
                        globalCharacterSpacing = value;

                        OnCharacterSpacingChanged(previous, globalCharacterSpacing);
                    }
                    else
                    {
                        globalCharacterSpacing = value;
                    }
                }
            }

            public Action<Color, Color> OnGraphicsTintChanged { get; set; }
            public Action<float, float> OnGraphicsBrightnessChanged { get; set; }
            public Action<Color, Color> OnUITintChanged { get; set; }
            public Action<float, float> OnUIBrightnessChanged { get; set; }
            public Action<int, int> OnCharacterSpacingChanged { get; set; }

            public GameConfig()
            {
                graphicsTint = DefaultGraphicsTint;
                graphicsBrightness= DefaultGraphicsBrightness;

                uiTint = DefaultUITint;
                uiBrightness = DefaultUIBrightness;

                globalCharacterSpacing = DefaultCharacterSpacing;
            }

            public GameConfig(Color graphicsTint, float graphicsBrightness,
                              Color uiTint, float uiBrightness,
                              int globalCharacterSpacing)
            {
                this.graphicsTint = graphicsTint;
                this.graphicsBrightness = graphicsBrightness;
                this.uiTint = uiTint;
                this.uiBrightness = uiBrightness;
                this.globalCharacterSpacing = globalCharacterSpacing;
            }

            private void SetGraphicsTintFromBrightness(bool revertFirst)
            {
                if (revertFirst)
                {
                    graphicsTint = baseGraphicsTint;
                }

                graphicsTint = uiTint.ScaleRGB(GraphicsBrightness);

                graphicsTint = Colors.MaxMaskRGB(graphicsTint, (byte)(255 * MinBrightness));
            }

            private void SetUITintFromBrightness(bool revertFirst)
            {
                if (revertFirst)
                {
                    uiTint = baseUITint;
                }

                uiTint = uiTint.ScaleRGB(UIBrightness);

                uiTint = Colors.MaxMaskRGB(uiTint, (byte)(255 * MinBrightness));
            }
        }
    }
}