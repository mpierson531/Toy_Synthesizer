using System;

using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Converters;

namespace Toy_Synthesizer.Game.Data.Generic
{
    // The concrete, generic implementation of Property<Source>, with delegates for getting/setting.
    public sealed class Property<Source, ValueType> : Property<Source>
    {
        private readonly Func<Source, ValueType> get;
        private readonly Action<ValueType, Source> set;
        public readonly ValueType DefaultValue;

        // This may be null.
        // If it is null, Convert.ToString will be used.
        public readonly ToStringConverter ToStringConverter;

        public Property(string name,
                        PropertyDataType dataType, PropertyUIData uiData,
                        Func<Source, ValueType> getter, Action<ValueType, Source> setter,
                        ValueType defaultValue,
                        PropertyRange? range = null,
                        string description = null,
                        bool shouldSetImmediately = false,
                        bool isUpdateable = false,
                        bool isReadonly = false,
                        bool isUserfacing = true,
                        ToStringConverter toStringConverter = null)
                          : base(name, dataType, uiData, range, description,
                                 shouldSetImmediately: shouldSetImmediately,
                                 isUpdateable: isUpdateable,
                                 isReadonly: isReadonly,
                                 isUserfacing: isUserfacing)
        {
            get = getter;
            set = setter;
            this.DefaultValue = defaultValue;
            this.ToStringConverter = toStringConverter;
        }

        public ValueType GetValue(Source source)
        {
            return get(source);
        }

        // Checks for equality first.
        public bool SetValue(Source source, ValueType value)
        {
            if (set is null)
            {
                return false;
            }

            if (object.Equals(value, GetValue(source)))
            {
                return false;
            }

            set(value, source);

            return true;
        }

        private void SetValueRaw(Source source, ValueType value)
        {
            set(value, source);
        }

        public sealed override T GetValue<T>(Source source)
        {
            ValueType value = GetValue(source);

            if (value is not T typeChecked)
            {
                ValidateConverting<T>(value, out typeChecked);
            }

            return typeChecked;
        }

        public sealed override void SetValue<T>(Source source, T value)
        {
            if (value is not ValueType typeChecked)
            {
                ValidateConverting<ValueType>(value, out typeChecked);
            }

            SetValue(source, typeChecked);
        }

        public sealed override void SetValueRaw<T>(Source source, T value)
        {
            if (value is not ValueType typeChecked)
            {
                ValidateConverting<ValueType>(value, out typeChecked);
            }

            SetValueRaw(source, typeChecked);
        }

        public sealed override bool Reset(Source source)
        {
            return SetValue(source, DefaultValue);
        }

        public string ToString(Source source)
        {
            if (object.Equals(source, default))
            {
                return $"{{ Name = {Name}, Type = {DataTypeName} }}";
            }

            ValueType value = GetValue(source);

            string stringValue;

            if (!ToStringConverter.TryConvert(ToStringConverter, value, out stringValue))
            {
                if (value is null)
                {
                    stringValue = "null";
                }
                else
                {
                    stringValue = Convert.ToString(value);
                }
            }

            ValueConverter<object, string>.Convert(ToStringConverter, value);

            return $"{{ Name = {Name}, Type = {DataTypeName}, Current Value = {stringValue} }}";
        }

        public override string ToString()
        {
            return ToString(source: default);
        }
    }
}
