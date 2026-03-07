using System;

using FontStashSharp;

namespace Toy_Synthesizer.Game.CommonUtils
{
    public static class FontUtils
    {
        public static float MeasureLargestWidth(DynamicSpriteFont font, string[] strings)
        {
            float width = 0f;

            for (int index = 0; index != strings.Length; index++)
            {
                float currentWidth = font.MeasureString(strings[index]).X;

                if (currentWidth > width)
                {
                    width = currentWidth;
                }
            }

            return width;
        }

        public static float MeasureLargestWidth(DynamicSpriteFont font, int count, Func<int, string> supplier)
        {
            float width = 0f;

            for (int index = 0; index != count; index++)
            {
                float currentWidth = font.MeasureString(supplier(index)).X;

                if (currentWidth > width)
                {
                    width = currentWidth;
                }
            }

            return width;
        }
    }
}
