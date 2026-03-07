using System.Collections.Generic;

using Microsoft.Xna.Framework;

using GeoLib;
using GeoLib.GeoMaths;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.UI;
using Toy_Synthesizer.Game.Data;
using Toy_Synthesizer.Game.Data.Generic;
using Toy_Synthesizer.Game.CommonUtils;

namespace Toy_Synthesizer.Game.Settings
{
    public sealed class BasicSettings : IIndexable<Property<Game>>
    {
        public const int SettingsCount = 6;
        public const int UITintIndex = 2;

        private readonly Game game;
        private readonly Property<Game>[] settings;

        int IIndexable<Property<Game>>.Count
        {
            get => SettingsCount;
        }

        public Property<Game> this[int index]
        {
            get => settings[index];
        }

        public BasicSettings(Game game)
        {
            this.game = game;

            settings = InitSettings();
        }

        private static Property<Game>[] InitSettings()
        {
            object[] builtinColors = BuiltinColors.Colors.ToArrayOfType<object>();
            Color defaultTint = Color.White;
            int defaultFramerateLimit = CoreConfig.EngineConfig.DefaultFramerateLimit;
            float defaultTransparency = 0f;

            return new Property<Game>[SettingsCount]
            {
                    new Property<Game, bool>
                    (
                    name: "Fullscreen",

                    dataType: PropertyDataType.Bool,
                    uiData: new PropertyUIData(PropertyWidgetType.Checkbox),

                    getter: FullscreenGetter,
                    setter: FullscreenSetter,

                    defaultValue: false
                    ),

                    new Property<Game, float>
                    (
                        name: "Framerate Limit",

                        dataType: PropertyDataType.Float,
                        uiData: new PropertyUIData(PropertyWidgetType.Slider),

                        getter: FramerateLimitGetter,
                        setter: FramerateLimitSetter,

                        range: PropertyRange.NumberRange(10, 500, 5),

                        defaultValue: defaultFramerateLimit
                    ),

                    new Property<Game, Color>
                    (
                        name: "UI Tint",

                        dataType: PropertyDataType.Color,
                        uiData: new PropertyUIData(PropertyWidgetType.DropDown),

                        getter: GetUITint,
                        setter: SetUITint,

                        range: PropertyRange.MultipleValues(builtinColors),

                        defaultValue: defaultTint,

                        shouldSetImmediately: true,

                        toStringConverter: BuiltinColors.ToNameConverter
                    ),

                    new Property<Game, float>
                    (
                        name: "UI Brightness",

                        dataType: PropertyDataType.Float,
                        uiData: new PropertyUIData(PropertyWidgetType.Slider),

                        getter: UIBrightnessGetter,
                        setter: UIBrightnessSetter,

                        range: PropertyRange.NumberRange(CoreConfig.GameConfig.MinBrightness * 100f, 100f, 1f),

                        defaultValue: 100f,

                        shouldSetImmediately: true
                    ),

                    new Property<Game, float>
                    (
                        name: "UI Transparency",

                        dataType: PropertyDataType.Float,
                        uiData: new PropertyUIData(PropertyWidgetType.Slider),

                        getter: UITransparencyGetter,
                        setter: UITransparencySetter,

                        range: PropertyRange.NumberRange(0f, 100f, 1f),

                        defaultValue: defaultTransparency,

                        shouldSetImmediately: true
                    ),

                    new Property<Game, float>
                    (
                        name: "Character Spacing",

                        dataType: PropertyDataType.Float,
                        uiData: new PropertyUIData(PropertyWidgetType.Spinner),

                        getter: GlobalCharacterSpacingGetter,
                        setter: GlobalCharacterSpacingSetter,

                        range: PropertyRange.NumberRange(-5f, 25f, 1f),

                        defaultValue: 0f,

                        shouldSetImmediately: false
                    )
            };
        }

        // A note about borderless fullscreen in DesktopGL:
        // Borderless fullscreen does not appear to work correctly. When it should go into borderless fullscreen, it instead goes into "true"/"exclusive" fullscreen mode.
        // In Nvidia Control Panel, setting the "Vulkan/OpenGL present method" setting for this app to "Prefer Layered On DXGI Swapchain" fixes this

        static bool FullscreenGetter(Game game)
        {
            return game.Config.Engine.Fullscreen;
        }

        static void FullscreenSetter(bool value, Game game)
        {
            if (value && !game.Geo.Window.IsBorderless)
            {
                game.Geo.Display.Graphics.ToggleFullScreen();
            }
            else if (!value && game.Geo.Window.IsBorderless)
            {
                Vec2i previousWindowSize = game.Geo.Display.PreviousWindowSize;
                game.Geo.Display.Graphics.ToggleFullScreen();
                game.Geo.Display.Resize(previousWindowSize);
            }

            game.Config.Engine.Fullscreen = value;
        }

        static float FramerateLimitGetter(Game game)
        {
            return game.Config.Engine.FramerateLimit;
        }

        static void FramerateLimitSetter(float value, Game game)
        {
            game.Config.Engine.FramerateLimit = (int)value;

            game.Geo.TargetFramerate = (int)value;
        }

        static float UIBrightnessGetter(Game game)
        {
            return game.Config.Game.UIBrightness * 100f;
        }

        static void UIBrightnessSetter(float value, Game game)
        {
            value = value * 0.01f;

            game.Config.Game.UIBrightness = value;
        }

        static float UITransparencyGetter(Game game)
        {
            return (1f - Colors.ToFloat(game.Config.Game.UITint.A)) * 100f;
        }

        static void UITransparencySetter(float value, Game game)
        {
            Color tint = game.Config.Game.UITint;

            tint.A = Colors.ToByte(1f - (value * 0.01f));

            game.Config.Game.UITint = tint;
        }

        static float GlobalCharacterSpacingGetter(Game game)
        {
            return game.Config.Game.GlobalCharacterSpacing;
        }

        static void GlobalCharacterSpacingSetter(float value, Game game)
        {
            game.Config.Game.GlobalCharacterSpacing = (int)value;

            void SetCharacterSpacing(Stage stage)
            {
                stage.Root.ForEachOfType<ITextWidget>(widget => widget.CharacterSpacing = value);
            }

            game.ForStage(SetCharacterSpacing);
        }

        private static Color GetUITint(Game game)
        {
            return game.Config.Game.UITint;
        }

        public static void SetUITint(Color color, Game game)
        {
            game.Config.Game.UITint = color.CopyIgnoreAlpha(game.Config.Game.UITint.A);
        }

        public void SyncUIWithSource(GroupWidget group)
        {
            PropertyUtils.SyncUIWithSource(game, group);
        }

        public void SetSourceFromConfig()
        {
            uint count = SettingsCount;

            for (int index = 0; (uint)index < count; index++)
            {
                this[index].SetValueRaw<object>(game, this[index].GetValue<object>(game));
            }
        }

        public bool SetFromUI(GroupWidget group)
        {
            return PropertyUtils.SetSourceFromUI(game, group);
        }

        // If group is *not* null, this also sets the UI to default values.
        // Only writes to the storage device if any property.Reset call returns true.
        public void ResetAndSave(GroupWidget group)
        {
            bool anyChanged = Reset(group);

            if (anyChanged)
            {
                game.Config.Save();
            }
        }

        // If group is *not* null, this also sets the UI to default values.
        // Only writes to the storage device if any property.Reset call returns true.
        public bool Reset(GroupWidget group)
        {
            bool anyChanged;

            if (group is not null)
            {
                anyChanged = PropertyUtils.ResetSourceAndUI(game, group);
            }
            else
            {
                anyChanged = PropertyUtils.ResetProperties(game, in settings);
            }

            return anyChanged;
        }

        public int IndexOf(string name)
        {
            for (int index = 0; index != SettingsCount; index++)
            {
                if (settings[index].Name == name)
                {
                    return index;
                }
            }

            return -1;
        }

        public void PopulateSettingsUI(UIManager uiManager, GroupWidget group, Vec2f labelPosition, Vec2f groupSize, float horizontalSpacing, float verticalSpacing)
        {
            Game sourceGetter()
            {
                return game;
            }

            PropertyUtils.CreateWidgets(this, uiManager, group, ref labelPosition, groupSize, horizontalSpacing, verticalSpacing, null, sourceGetter);
        }

        public IEnumerator<Property<Game>> GetEnumerator()
        {
            for (int index = 0; index != SettingsCount; index++)
            {
                yield return settings[index];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
