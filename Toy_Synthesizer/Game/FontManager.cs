using System;
using System.Collections.Generic;

using FontStashSharp;

using GeoLib;
using GeoLib.GeoMaths;

namespace Toy_Synthesizer.Game
{
    public class FontManager : Disposable
    {
        private Game game;

        private Dictionary<Vec2i, DynamicSpriteFont> fontCache;

        private FontSystem fontSystem;

        private DynamicSpriteFont currentFont;

        // Only call during/after initialization of Game.
        public FontManager(Game game, int fontStepCount)
        {
            if (fontStepCount <= 0 || fontStepCount % 2 != 0)
            {
                throw new ArgumentException("fontStepCount must be even and must be greater than zero.");
            }

            this.game = game;

            InitFontSystemAndFontCache(fontStepCount);
        }

        internal bool GenerateNewFont(Vec2i previousSize, Vec2i newSize, out DynamicSpriteFont newFont)
        {
            Vec2f closestWindowSize = (Vec2f)FindClosestWindowSize(previousSize, newSize);

            currentFont = FindOrCreateFont(closestWindowSize.ToVec2i());

            game.UIManager.MainFont = currentFont;

            newFont = currentFont;

            return true;
        }

        private Vec2i FindClosestWindowSize(Vec2i previousSize, Vec2i newSize)
        {
            Vec2i closestSize = previousSize;
            float closestDiff = float.MaxValue;

            foreach (Vec2i size in fontCache.Keys)
            {
                float diff = ((Vec2f)size - (Vec2f)newSize).Length();

                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestSize = size;
                }
            }

            return closestSize;
        }

        // Prototype for weighing aspect ratio more.
        /*private Vec2i FindClosestWindowSize(Vec2i previousSize, Vec2i newSize)
        {
            Vec2i closestSize = previousSize;
            float closestScore = float.MaxValue;

            float targetRatio = (float)newSize.X / newSize.Y;

            foreach (Vec2i size in fontCache.Keys)
            {
                float diff = ((Vec2f)size - (Vec2f)newSize).Length();

                float ratio = (float)size.X / size.Y;
                float ratioDiff = Math.Abs(ratio - targetRatio);

                float score = diff + ratioDiff * 500f;

                if (score < closestScore)
                {
                    closestScore = score;
                    closestSize = size;
                }
            }

            return closestSize;
        }*/

        public DynamicSpriteFont FindOrCreateFont(float scale)
        {
            Vec2f targetWindowSize = game.TargetDisplayResolution * scale;

            return FindOrCreateFont(targetWindowSize.ToVec2i());
        }

        public DynamicSpriteFont FindOrCreateFont(Vec2i windowSize)
        {
            return FindOrCreateFont(windowSize.X, windowSize.Y);
        }

        public DynamicSpriteFont FindOrCreateFont(int width, int height)
        {
            float scale = (new Vec2f(width, height) / (Vec2f)game.TargetDisplayResolution).Min();

            if (!TryFontCache(new Vec2i(width, height), out DynamicSpriteFont font))
            {
                font = CreateFontInternal(scale);
            }

            return font;
        }

        private DynamicSpriteFont CreateFontInternal(float scale)
        {
            Vec2i sizeKey = (game.TargetDisplayResolution * scale).ToVec2i();

            DynamicSpriteFont font;

            // Checking here again ensures that scaled and rounded values aren't added multiple times.
            if (!TryFontCache(sizeKey, out font))
            {
                font = fontSystem.GetFont(Game.BASE_FONT_SIZE * scale);

                fontCache.Add((game.TargetDisplayResolution * scale).ToVec2i(), font);
            }

            return font;
        }

        private bool TryFontCache(Vec2i size, out DynamicSpriteFont font)
        {
            return fontCache.TryGetValue(size, out font);
        }

        private void InitFontSystemAndFontCache(int fontStepCount)
        {
            FontSystemDefaults.TextStyleLineHeight = 1;

            FontSystemSettings fontSystemSettings = new FontSystemSettings
            {
                FontResolutionFactor = 2,

                KernelWidth = 2,
                KernelHeight = 2,

                TextureWidth = FontSystemDefaults.TextureWidth * 2,
                TextureHeight = FontSystemDefaults.TextureHeight * 2
            };
            fontSystem = new FontSystem(fontSystemSettings);
            fontSystem.AddFont(game.Geo.AssetLoaders.Bytes.Load("Content", "Fonts", "Open Sauce One", "OpenSauceOne-Regular.ttf"));

            BakeFonts(fontStepCount);

            // If for some reason the font cache wasn't initialized with the target window size, generate that font too.
            if (!TryFontCache(game.TargetWindowSize, out DynamicSpriteFont value))
            {
                value = FindOrCreateFont(game.TargetWindowSize.X, game.TargetWindowSize.Y);
            }

            currentFont = value;
            game.UIManager.MainFont = currentFont;
        }

        private void BakeFonts(int fontStepCount)
        {
            float deviceScale = ((Vec2f)game.DisplayDeviceSize / (Vec2f)game.TargetDisplayResolution).Min();
            int fontStepScale = (int)MathF.Ceiling(deviceScale);

            fontStepCount *= fontStepScale;

            Vec2f increment = (Vec2f)game.DisplayDeviceSize / (float)fontStepCount;

            // Increment has to be at least (1, 1).
            // Realistically, it should always be greater than or equal to one,
            // unless fontStepCount is very large and/or the display's resolution is very small.
            increment = Vec2f.Max(Vec2f.One, increment);

            fontCache = new Dictionary<Vec2i, DynamicSpriteFont>(fontStepCount);

            Vec2f currentSize = increment; // Increment is also the first size.

            for (int index = 0; index < fontStepCount; index++)
            {
                Vec2i sizeInts = currentSize.ToVec2i();

                // Ensure no duplicates due to rounding.
                if (fontCache.ContainsKey(sizeInts))
                {
                    continue;
                }

                FindOrCreateFont(sizeInts);

                currentSize += increment;
            }
        }

        protected override void DisposeInternal(bool fromFinalizer)
        {
            base.DisposeInternal(fromFinalizer);

            fontSystem.Dispose();

            game = null;
            fontSystem = null;
            fontCache = null;
            currentFont = null;
        }
    }
}
