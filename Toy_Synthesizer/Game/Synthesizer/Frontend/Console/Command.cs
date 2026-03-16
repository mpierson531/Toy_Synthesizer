using System.Text;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Pooling;
using GeoLib.GeoUtils.Collections;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Help;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Console
{
    // A command is specific to the GeoSimConsole.
    public class Command
    {
        public static readonly ImmutableArray<Parameter> EmptyCommandParameters;

        static Command()
        {
            EmptyCommandParameters = ImmutableArray<Parameter>.Empty;
        }

        public static Command GetNewCls()
        {
            return FunctionNoParams("cls", "Clears the console", alias: "Clear");
        }

        public static Command GetNewClear()
        {
            return FunctionNoParams("clear", "Clears the console");
        }

        public static Command GetNewResetTint()
        {
            return FunctionNoParams("reset_tint", "Resets the console's tints");
        }

        public static Command GetNewOuputTint()
        {
            return Property("output_tint", "Sets the tint of the console's output area", "tint", "Color");
        }

        public static Command GetNewOuputTextTint()
        {
            return Property("output_text_tint", "Sets the tint of text in the console output", "tint", "Color");
        }

        public static Command GetNewInputTint()
        {
            return Property("input_tint", "Sets the tint of the console's input area", "tint", "Color");
        }

        public static Command GetNewInputTextTint()
        {
            return Property("input_text_tint", "Sets the tint of text in the console input", "tint", "Color");
        }

        public static Command GetNewHelp()
        {
            return FunctionNoParams("help", "Lists commands and functions");
        }

        public static Command GetNewIndividualHelp()
        {
            return FunctionOneParam("help", "Help message for individual console items", "item", "ConsoleItem", takesConsoleItems: true);
        }

        // If you need a special alias, pass it for alias.
        // If alias is null, these will automatically build an alias from the name.
        public static Command FunctionOneParam(string name, string description, string paramName, string paramType,
                                               string alias = null, bool takesConsoleItems = false)
        {
            CheckAlias(name, ref alias);

            ImmutableArray<Parameter> parameters = new ImmutableArray<Parameter>(new Parameter(paramName, paramType));

            return new Command(name, alias, description, parameters, isProperty: false, takesConsoleItems: takesConsoleItems);
        }

        public static Command FunctionNoParams(string name, string description, string alias = null)
        {
            CheckAlias(name, ref alias);

            return new Command(name, alias, description, EmptyCommandParameters, isProperty: false, takesConsoleItems: false);
        }

        public static Command Property(string name, string description, string paramName, string paramType, bool onlyGet = false, string alias = null)
        {
            CheckAlias(name, ref alias);

            ImmutableArray<Parameter> parameters;

            if (onlyGet)
            {
                parameters = EmptyCommandParameters;
            }
            else
            {
                parameters = new ImmutableArray<Parameter>(new Parameter(paramName, paramType));
            }

            return new Command(name, alias, description, parameters, isProperty: true, takesConsoleItems: false);
        }

        public readonly string Name;
        public readonly string Alias;
        public readonly string Description;
        public readonly ImmutableArray<Parameter> Parameters;
        public readonly bool IsProperty;
        public readonly bool TakesConsoleItems;

        private Command(string name, string alias, string description, ImmutableArray<Parameter> parameters, bool isProperty, bool takesConsoleItems)
        {
            Name = name;
            Alias = alias;
            Description = description;
            Parameters = parameters;
            IsProperty = isProperty;
            TakesConsoleItems = takesConsoleItems;
        }

        private static void CheckAlias(string name, ref string alias)
        {
            if (alias is null)
            {
                alias = BuildAlias(name);
            }
        }

        private static string BuildAlias(string name)
        {
            StringBuilder alias = new StringBuilder(name.Length);

            alias.Append(char.ToUpper(name[0]));

            int index = 1;

            while (index < name.Length)
            {
                if (name[index] == '_')
                {
                    alias.Append(char.ToUpper(name[index + 1]));

                    index += 2;
                }
                else
                {
                    alias.Append(name[index++]);
                }
            }

            return alias.ToString();
        }

        public void AppendToBuilder(StringBuilder builder, int tabs, int newLinesFirst, int newLinesAfter)
        {
            string tab = Game.Tab;
            string fullTab = Utils.EmptyString;

            if (tabs != 0)
            {
                fullTab = Game.GetTabs(tabs) + tab;
            }

            if (newLinesFirst > 0)
            {
                Game.AppendNewLines(builder, newLinesFirst);
            }

            // Probably exaggerated, but easier than trying to be precise.
            builder.EnsureCapacity(500 + newLinesFirst * Game.NewLineLength + newLinesAfter * Game.NewLineLength + tabs * Game.Tab.Length);

            builder.Append("Command");
            builder.Append(Game.NewLine);
            builder.Append(fullTab);
            builder.Append('{');
            builder.Append(Game.NewLine);
            builder.Append(fullTab);
            builder.Append(tab);

            builder.Append("Name: ");
            builder.Append(Name);

            builder.Append(',');

            builder.Append(Game.DoubleNewLine);
            builder.Append(fullTab);
            builder.Append(tab);

            builder.Append("Alias: ");
            builder.Append(Alias);

            builder.Append(',');

            builder.Append(Game.DoubleNewLine);
            builder.Append(fullTab);
            builder.Append(tab);

            builder.Append("Description: ");
            builder.Append(Description);

            builder.Append(',');

            builder.Append(Game.DoubleNewLine);
            builder.Append(fullTab);
            builder.Append(tab);

            if (Parameters.Count != 0)
            {
                builder.Append("Parameters");
                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);

                builder.Append('{');
                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);
                builder.Append(tab);

                for (int index = 0; index != Parameters.Count; index++)
                {
                    Parameters[index].AppendToBuilder(builder, ensureCapacity: false);

                    if (index != Parameters.Count - 1)
                    {
                        builder.Append(Game.DoubleNewLine);
                        builder.Append(fullTab);
                        builder.Append(tab);
                        builder.Append(tab);
                    }
                }

                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);

                builder.Append('}');

                builder.Append(',');

                builder.Append(Game.DoubleNewLine);
                builder.Append(fullTab);
                builder.Append(tab);
            }

            builder.Append("IsProperty: ");
            builder.Append(IsProperty);

            builder.Append(", ");

            builder.Append(Game.NewLine);
            builder.Append(fullTab);
            builder.Append(tab);

            builder.Append("Takes console items: ");
            builder.Append(TakesConsoleItems);

            builder.Append(Game.NewLine);
            builder.Append(fullTab);

            builder.Append('}');

            if (newLinesAfter > 0)
            {
                Game.AppendNewLines(builder, newLinesAfter);
            }
        }

        public sealed override string ToString()
        {
            PoolableStringBuilder poolable = Pools.Common.StringBuilders.Get();

            AppendToBuilder(poolable.builder, 0, newLinesFirst: 1, newLinesAfter: 1);

            string value = poolable.ToString();

            Pools.Common.StringBuilders.Return(poolable);

            return value;
        }
    }
}
