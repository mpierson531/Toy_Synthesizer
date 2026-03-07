using System;
using System.DirectoryServices.ActiveDirectory;

using GeoLib.GeoMaths;
using GeoLib.GeoSerialization;
using GeoLib.GeoShapes;

namespace Toy_Synthesizer.Game
{
    [GenerateFormatter(AddToSerializationFormatterCache = true)]
    public class WorldUnits
    {
        private float pixelsPerMeter;
        private float metersPerPixel;

        [SerializableProperty]
        public float PixelsPerMeter
        {
            get => pixelsPerMeter;

            set
            {
                if (value <= 0f)
                {
                    throw new ArgumentException("Cannot be less than or equal to 0.");
                }

                pixelsPerMeter = value;
                metersPerPixel = 1f / pixelsPerMeter;
            }
        }

        public WorldUnits(float pixelsPerMeter)
        {
            PixelsPerMeter = pixelsPerMeter;
        }

        public float MetersPerPixel
        {
            get => metersPerPixel;

            set
            {
                if (value <= 0f)
                {
                    throw new ArgumentException("Cannot be less than or equal to 0.");
                }

                metersPerPixel = value;
                pixelsPerMeter = 1f / metersPerPixel;
            }
        }

        public Vec2f ScaleToWorld(float x, float y)
        {
            return new Vec2f(ScaleToWorld(x), ScaleToWorld(y));
        }

        public Vec2f ScaleToWorld(Vec2f value)
        {
            return ScaleToWorld(value.X, value.Y);
        }

        public Vec2f ScaleToRender(float x, float y)
        {
            return new Vec2f(ScaleToRender(x), ScaleToRender(y));
        }

        public Vec2f ScaleToRender(Vec2f value)
        {
            return ScaleToRender(value.X, value.Y);
        }

        public float ScaleToWorld(float value)
        {
            return value * MetersPerPixel;
        }

        public float ScaleToRender(float value)
        {
            return value * PixelsPerMeter;
        }

        public AABB ScaleToWorld(AABB value)
        {
            return new AABB(ScaleToWorld(value.Position), ScaleToWorld(value.Size));
        }

        public AABB ScaleToRender(AABB value)
        {
            return new AABB(ScaleToRender(value.Position), ScaleToRender(value.Size));
        }

        public WorldUnits Copy()
        {
            return new WorldUnits(PixelsPerMeter);
        }
    }
}