namespace Toy_Synthesizer.Game.Data
{
    public readonly struct PropertyRange
    {
        public readonly float Min;
        public readonly float Max;
        public readonly float Increment;

        public readonly object[] Values; // Mostly for Properties with multiple values, rather than a simple number range

        public static PropertyRange NumberRange(float min, float max, float increment)
        {
            return new PropertyRange(min, max, increment);
        }

        public static PropertyRange MultipleValues(object[] values)
        {
            return new PropertyRange(values);
        }

        private PropertyRange(float min, float max, float increment)
        {
            Min = min;
            Max = max;
            Increment = increment;
        }

        private PropertyRange(object[] values)
        {
            Values = values;

            Min = float.NaN;
            Max = float.NaN;
            Increment = float.NaN;
        }
    }
}
