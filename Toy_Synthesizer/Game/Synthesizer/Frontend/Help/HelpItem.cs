using System.Text;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Pooling;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.Data;
using Toy_Synthesizer.Game.Plugins.Builtin.Core.Help;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Help
{
    // A HelpItem is specific to the GeoSimConsole.
    // Parameters will be null if it is a property that is readonly, or it is a function without parameters.
    // ConstructorExamples will be null if it is a property or a function.
    // ChildItems will be null if it is a function, or otherwise doesn't have children.
    public class HelpItem
    {
        public static HelpItem TypeReferenceFromProperties<T, Source>(string displayName,
                                                         string typeName,
                                                         string description,
                                                         T properties,
                                                         string constructorExample = null) where T : IIndexable<Property<Source>>
        {
            ViewableList<HelpItem> propertyItems = new ViewableList<HelpItem>(properties.Count + 5);

            for (int index = 0; index < properties.Count; index++)
            {
                propertyItems.Add(GetHelpItem(properties[index]));
            }

            ImmutableArray<string> constructorExamples;

            if (constructorExample is not null)
            {
                constructorExamples = new ImmutableArray<string>(constructorExample);
            }
            else
            {
                constructorExamples = null;
            }

            return TypeReference(displayName: displayName,
                                 typeName: typeName,
                                 description: description,
                                 constructorExamples: constructorExamples,
                                 usageExamples: null,
                                 memberItems: propertyItems);
        }

        public static HelpItem GetHelpItem<Source>(Property<Source> property)
        {
            if (property.IsReadonly)
            {
                return NotReadonlyProperty(property.Name, property.DataTypeName, property.Description, property.Name);
            }

            return ReadonlyProperty(property.Name, property.DataTypeName, property.Description);
        }

        public static HelpItem FunctionOneParam(string name, string description, string paramName, string paramType,
                                                   params string[] examples)
        {
            return FunctionOneParam(name, description, paramName, paramType, ExamplesStringArrayToImmutable(examples));
        }

        public static HelpItem FunctionOneParam(string name, string description, string paramName, string paramType,
                                                   ImmutableArray<string> examples = null)
        {
            ImmutableArray<Parameter> parameters = new ImmutableArray<Parameter>(new Parameter(paramName, paramType));

            return new HelpItem(name, "Function", description, parameters,
                                   ImplementationType.Function,
                                   isReadonly: true,
                                   childItems: null,
                                   constructorExamples: null,
                                   usageExamples: examples);
        }

        public static HelpItem FunctionNoParams(string name, string description,
                                                   ImmutableArray<string> examples = null)
        {
            return new HelpItem(name, "Function", description, null,
                                   ImplementationType.Function,
                                   isReadonly: true,
                                   childItems: null,
                                   constructorExamples: null,
                                   usageExamples: examples);
        }

        public static HelpItem NotReadonlyProperty(string name, string type, string description, string paramName,
                                                      ViewableList<HelpItem> childItems = null,
                                                      params string[] examples)
        {
            return Property(name, type, description, paramName, isReadonly: false, childItems: childItems, examples: ExamplesStringArrayToImmutable(examples));
        }

        public static HelpItem ReadonlyProperty(string name, string type, string description,
                                                   ViewableList<HelpItem> childItems = null,
                                                   params string[] examples)
        {
            return Property(name, type, description, Utils.EmptyString, isReadonly: true, childItems: childItems, examples: ExamplesStringArrayToImmutable(examples));
        }

        public static HelpItem NotReadonlyProperty(string name, string type, string description, string paramName,
                                                      ViewableList<HelpItem> childItems = null,
                                                      ImmutableArray<string> examples = null)
        {
            return Property(name, type, description, paramName, isReadonly: false, childItems: childItems, examples: examples);
        }

        public static HelpItem ReadonlyProperty(string name, string type, string description,
                                                   ViewableList<HelpItem> childItems = null,
                                                   ImmutableArray<string> examples = null)
        {
            return Property(name, type, description, Utils.EmptyString, isReadonly: true, childItems: childItems, examples: examples);
        }

        private static HelpItem Property(string name, string type, string description, string paramName,
                                        bool isReadonly,
                                        ViewableList<HelpItem> childItems,
                                        ImmutableArray<string> examples = null)
        {
            ImmutableArray<Parameter> parameters;

            if (isReadonly)
            {
                parameters = null;
            }
            else
            {
                parameters = new ImmutableArray<Parameter>(new Parameter(paramName, type));
            }

            return new HelpItem(name, type, description, parameters,
                                   ImplementationType.Property,
                                   isReadonly,
                                   childItems,
                                   constructorExamples: null,
                                   usageExamples: examples);
        }

        public static HelpItem TypeReference(string displayName, string typeName, string description,
                                                 ImmutableArray<string> constructorExamples,
                                                 ImmutableArray<string> usageExamples,
                                                 ViewableList<HelpItem> memberItems)
        {
            return new HelpItem(displayName, typeName, description, parameters: null, implType: ImplementationType.Type,
                                   isReadonly: true,
                                   childItems: memberItems,
                                   constructorExamples: constructorExamples,
                                   usageExamples: usageExamples);
        }

        public string Name { get; private set; } // not readonly, so that the constructor can modify child items' names.
        public readonly string Type;
        public readonly string Description;
        public readonly ImmutableArray<Parameter> Parameters;
        public readonly ImplementationType ImplType;
        public readonly bool IsReadonly; // Only for properties/variables; always true for functions
        public readonly ViewableList<HelpItem> ChildItems; // May be used for members for type references
        internal readonly ImmutableArray<string> childItemNamesNotPrefixed;
        public readonly ImmutableArray<string> ConstructorExamples; // Only for constructors
        public readonly ImmutableArray<string> UsageExamples;

        private HelpItem(string name,
                            string type,
                            string description,
                            ImmutableArray<Parameter> parameters,
                            ImplementationType implType,
                            bool isReadonly,
                            ViewableList<HelpItem> childItems,
                            ImmutableArray<string> constructorExamples,
                            ImmutableArray<string> usageExamples)
        {
            Name = name;
            Type = type;
            Description = description;
            Parameters = parameters;
            ImplType = implType;
            IsReadonly = isReadonly;
            ChildItems = childItems;
            ConstructorExamples = constructorExamples;
            UsageExamples = usageExamples;
        }

        public void AppendToBuilder(StringBuilder builder, int tabs, int newLinesBefore, int newLinesAfter)
        {
            HelpItemHelpers.AppendToBuilder(this, builder, tabs, newLinesBefore, newLinesAfter);
        }

        public sealed override string ToString()
        {
            PoolableStringBuilder poolable = Pools.Common.StringBuilders.Get();

            AppendToBuilder(poolable.builder, 0, newLinesBefore: 1, newLinesAfter: 1);

            string value = poolable.ToString();

            Pools.Common.StringBuilders.Return(poolable);

            return value;
        }

        // HelpItem implementations

        // If value is null or a newline, this will return newline.
        // If value is not null or a newline, it will return value.
        private static string AddStringOrNewLine(string value)
        {
            return value is null || value == Game.NewLine ? Game.NewLine : value;
        }

        private static ImmutableArray<string> ExamplesStringArrayToImmutable(string[] array)
        {
            return array is null || array.Length == 0 ? null : new ImmutableArray<string>(array);
        }
    }
}
