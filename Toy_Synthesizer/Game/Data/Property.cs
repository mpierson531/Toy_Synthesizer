namespace Toy_Synthesizer.Game.Data
{
    public abstract class Property<Source>
    {
        public readonly string Name;
        public readonly string DataTypeName;
        public readonly PropertyDataType DataType;
        public readonly PropertyUIData UIData;
        public readonly PropertyRange? Range;
        public readonly string Description;
        public bool ShouldSetImmediately; // This should be used for determining if a Property should be set as soon as it is changed in UI, or only when an apply button is pressed (for example).
        public bool IsUpdateable; // Mostly used for ShapeProperties at the moment
        public bool IsReadonly;
        public bool IsUserfacing;

        public Property(string name,
                        PropertyDataType dataType, PropertyUIData uiData,
                        PropertyRange? range,
                        string description,
                        bool shouldSetImmediately,
                        bool isUpdateable,
                        bool isReadonly, 
                        bool isUserfacing)
        {
            Name = name;

            DataType = dataType;
            UIData = uiData;

            Range = range;

            Description = description;

            ShouldSetImmediately = shouldSetImmediately;
            IsUpdateable = isUpdateable;
            IsReadonly = isReadonly;

            DataTypeName = DataType.ToString();
            IsUserfacing = isUserfacing;
        }

        protected void ValidateConverting<T>(object value, out T typedValue)
        {
            if (!PropertyTypeConverter.TryConvert<object, T>(value, DataType, out object converted))
            {
                throw WrongTypeException<T>.WrongValue(value);
            }

            typedValue = (T)converted;
        }

        public object GetValueBoxed(Source source)
        {
            return GetValue<object>(source);
        }

        public abstract T GetValue<T>(Source source);

        public abstract void SetValue<T>(Source source, T value);
        public abstract void SetValueRaw<T>(Source source, T value);

        public abstract bool Reset(Source source);
    }
}