using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Pooling;
using GeoLib.GeoUtils.Collections;
using GeoLib.GeoUtils.Converters;

namespace Toy_Synthesizer.Game
{
    public static class BuiltinColors
    {
        public static readonly ImmutableArray<Color> Colors;
        private static readonly Dictionary<Color, string> namesMap;
        private static readonly Dictionary<string, string> stringifiedMap;
        private static readonly ToStringNameConverter toNameConverter;

        static BuiltinColors()
        {
            namesMap = new Dictionary<Color, string>
            {
                // Basic colors, attempting to group by similarity

                { Color.White, "White" },
                { Color.Gray, "Gray" },
                { Color.Black, "Black" },
                { DarkWhite, "Dark White" },
                { DarkerWhite, "Darker White" },

                { Color.Red, "Red" },
                { LightRed, "Light Red" },
                { Color.DarkRed, "Dark Red" },
                { Color.Orange, "Orange" },
                { Color.MonoGameOrange, "MonoGame Orange" },
                { Color.Yellow, "Yellow" },
                { Color.LightYellow, "Light Yellow" },

                { Color.Green, "Green" },
                { Color.LightGreen, "Light Green" },
                { Color.DarkGreen, "Dark Green" },

                { Color.Blue, "Blue" },
                { Color.SkyBlue, "Sky Blue" },
                { Color.LightBlue, "Light Blue" },
                { Color.CornflowerBlue, "Cornflower Blue" },
                { Color.DarkBlue, "Dark Blue" },
                { Color.DarkSlateBlue, "Dark Slate Blue" },
                { Color.Purple, "Purple" },
                

                // In-depth colors (colors that don't have a basic name like "white", "gray", "black", "red", "green", "blue", etc.)

                { Sepia, "Sepia" },


                { Color.Teal, "Teal" },

                { Color.Beige, "Beige" },
                { Color.Chocolate, "Chocolate" },
                { Color.Indigo, "Indigo" }
            };

            stringifiedMap = new Dictionary<string, string>(namesMap.Count);

            Color[] builtinColors = new Color[namesMap.Count];
            int colorIndex = 0;

            foreach (KeyValuePair<Color, string> pair in namesMap)
            {
                builtinColors[colorIndex++] = pair.Key;
                stringifiedMap.Add(pair.Key.ToString(), pair.Value);
            }

            Colors = new ImmutableArray<Color>(builtinColors);

            toNameConverter = new ToStringNameConverter();
        }

        /// <summary>
        /// { 230, 230, 230, 255 }
        /// </summary>
        public static Color DarkWhite => new Color(230, 230, 230, 255);
        /// <summary>
        /// { 205, 205, 205, 255 }
        /// </summary>
        public static Color DarkerWhite => new Color(205, 205, 205, 255);
        /// <summary>
        /// { 255, 240, 130, 255 }
        /// </summary>
        public static Color Sepia => new Color(255, 240, 130, 255);

        /// <summary>
        /// { 255, 50, 0, 255 }
        /// </summary>
        public static Color LightRed => new Color(255, 50, 0, 255);

        public static ToStringNameConverter ToNameConverter
        {
            get => toNameConverter;
        }

        public static string ToName(Color color)
        {
            if (!namesMap.TryGetValue(color, out string name))
            {
                return color.ToString();
            }

            return name;
        }

        public static string ToName(int index)
        {
            Color color = Colors[index];

            return ToName(color);
        }

        // Right now, this is only used for dropdown widgets with colors.
        public static string ToName(object value)
        {
            if (value is not Color color)
            {
                throw new InvalidOperationException("value was not a Color.");
            }

            return ToName(color);
        }

        // This will attempt to replace all strings matching the format Color uses when calling ToString with matching names
        public static string ToName(string toStringed)
        {
            if (toStringed is null || toStringed.Length == 0)
            {
                return TextUtils.EmptyString;
            }

            if (!toStringed.Contains("{R:"))
            {
                return toStringed;
            }

            PoolableStringBuilder formatBuilder = Pools.Common.StringBuilders.Get();
            PoolableStringBuilder fullBuilder = Pools.Common.StringBuilders.Get();
            fullBuilder.Append(toStringed);

            for (int index = 0; index != toStringed.Length; index++)
            {
                if (toStringed[index] == '{')
                {
                    if (index + 1 < toStringed.Length && toStringed[index + 1] == 'R' && index + 2 < toStringed.Length && toStringed[index + 2] == ':') // Color format
                    {
                        int formatStart = index;

                        index++;

                        TextUtils.AppendWhileNotTerminator(formatBuilder.builder, toStringed, ref index, toStringed.Length, '}');

                        formatBuilder.Insert(0, '{');
                        formatBuilder.Append('}');

                        string formatted = formatBuilder.ToString();

                        formatBuilder.Clear();

                        if (!stringifiedMap.TryGetValue(formatted, out string name))
                        {
                            continue;
                        }

                        fullBuilder.builder.Replace(formatted, name, formatStart, formatted.Length);
                    }
                }
            }

            string replaced = fullBuilder.ToString();

            Pools.Common.StringBuilders.Return(formatBuilder);
            Pools.Common.StringBuilders.Return(fullBuilder);

            return replaced;
        }

        public sealed class ToStringNameConverter : ToStringConverter
        {
            public override string Convert(object value)
            {
                return ToName(value);
            }

            public override bool IsValid(object value)
            {
                return value is Color;
            }
        }
    }
}
