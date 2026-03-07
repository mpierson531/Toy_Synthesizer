using System;

using Microsoft.Xna.Framework;

namespace Toy_Synthesizer.Game.Data
{
    public static class PropertyTypeConverter
    {
        private static class Numbers
        {
            internal static bool TryConvert<From, To>(From from, PropertyDataType fromType, out object converted)
            {
                converted = null;

                if (fromType == PropertyDataType.Float)
                {
                    float value = float.NaN;

                    if (from is byte b)
                    {
                        value = Convert.ToSingle(b);
                    }

                    if (from is ushort us)
                    {
                        value = Convert.ToSingle(us);
                    }

                    if (from is uint ui)
                    {
                        value = Convert.ToSingle(ui);
                    }

                    if (from is ulong ul)
                    {
                        value = Convert.ToSingle(ul);
                    }

                    if (from is sbyte sb)
                    {
                        value = Convert.ToSingle(sb);
                    }

                    if (from is short s)
                    {
                        value = Convert.ToSingle(s);
                    }

                    if (from is int i)
                    {
                        value = Convert.ToSingle(i);
                    }

                    if (from is long l)
                    {
                        value = Convert.ToSingle(l);
                    }

                    if (from is float f)
                    {
                        value = f;
                    }

                    if (from is double d)
                    {
                        value = Convert.ToSingle(d);
                    }

                    if (!float.IsNaN(value))
                    {
                        converted = value;
                    }
                }

                return converted is not null;
            }
        }

        public static bool TryConvert<From, To>(From from, PropertyDataType fromType, out object converted)
        {
            if (from is To to)
            {
                converted = to;
                return true;
            }

            if (typeof(To) == typeof(string))
            {
                if (fromType == PropertyDataType.String && from is string s)
                {
                    converted = s;
                }
                else
                {
                    converted = Convert.ToString(from);
                }

                return true;
            }

            if (fromType == PropertyDataType.Bool && from is bool b)
            {
                converted = b;
                return true;
            }

            if (fromType == PropertyDataType.Color && from is uint packedValue)
            {
                converted = new Color(packedValue);
                return true;
            }

            if (Numbers.TryConvert<From, To>(from, fromType, out converted))
            {
                return true;
            }

            converted = null;
            return false;
        }
    }
}
