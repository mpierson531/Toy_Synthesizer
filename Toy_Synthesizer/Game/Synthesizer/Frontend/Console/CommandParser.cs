using System;
using System.Text;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Pooling;
using GeoLib.GeoUtils.Collections;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Help;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Console
{
    public static class CommandParser
    {
        public enum CommandResult
        {
            IsCommand,
            IsCommandButError,
            NoCommand,
            InputIsEmpty
        }

        // Will check if text is a command, and if so, will transform the text into the corresponding function with arguments.
        // Sets transformed to text in the event that it is not a command, or empty.
        // Sets transformed to an error message if it was a command but there was a problem parsing it.
        public static CommandResult Parse(string text, ImmutableArray<Command> commands, HelpItemDictionary items, out string transformed)
        {
            transformed = text;

            if (text is null || text.Length == 0)
            {
                return CommandResult.InputIsEmpty;
            }

            int length = text.Length;

            PoolableStringBuilder poolableBuilder = Pools.Common.StringBuilders.Get();
            poolableBuilder.builder.EnsureCapacity(length);

            int index = 0;

            TextUtils.ProgressWhileWhitespace(text, ref index, length);

            if (index == length) // All whitespace
            {
                Pools.Common.StringBuilders.Return(poolableBuilder);

                return CommandResult.InputIsEmpty;
            }

            TextUtils.AppendWhileNotWhitespace(poolableBuilder.builder, text, ref index, length);

            if (!IsCommand(poolableBuilder.builder, commands))
            {
                return CleanupParse(poolableBuilder, CommandResult.NoCommand);
            }

            TextUtils.ProgressWhileWhitespace(text, ref index, text.Length);

            CommandResult commandResult = TryParseCommand(poolableBuilder.builder, text, index, commands, items, ref transformed);

            return CleanupParse(poolableBuilder, commandResult);
        }

        // Should cleanup/finish parsing, then return the result.
        // At the moment, the only thing that needs cleaned up is the de-pooled string builder.
        private static CommandResult CleanupParse(PoolableStringBuilder builder, CommandResult result)
        {
            Pools.Common.StringBuilders.Return(builder);

            return result;
        }

        private static CommandResult TryParseCommand(StringBuilder builder, string text, int index, ImmutableArray<Command> commands, HelpItemDictionary items,
                                                     ref string transformed)
        {
            int length = text.Length;

            Command command;
            ImmutableArray<string> arguments;
            int matchingCommandNamesCount;

            if (index >= length) // No arguments
            {
                command = FindCommand(builder, commands, null, out matchingCommandNamesCount);
                arguments = null;
            }
            else
            {
                arguments = FindCommandArguments(text, index);
                command = FindCommand(builder, commands, (command) => command.Parameters.Count == arguments.Count, out matchingCommandNamesCount);
            }

            if (command is null) // No fully matching command found
            {
                transformed = $"{matchingCommandNamesCount} commands with the same name found, but none had the correct amount of parameters.";

                return CommandResult.IsCommandButError;
            }

            return TransformCommand(builder, arguments, items, command, ref transformed);
        }

        private static CommandResult TransformCommand(StringBuilder builder, ImmutableArray<string> arguments, HelpItemDictionary items, Command command,
                                                      ref string transformed)
        {
            bool isProperty = command.IsProperty;

            if (isProperty)
            {
                if (command.Parameters.Count > 1)
                {
                    throw new InvalidOperationException("Command is a property but has multiple parameters.");
                }

                if (arguments is not null && arguments.Count > 1)
                {
                    throw new InvalidOperationException("Command is a property but multiple arguments were supplied.");
                }

                if (command.TakesConsoleItems)
                {
                    throw new InvalidOperationException("A property command cannot take console items.");
                }
            }

            CommandResult result = CommandResult.IsCommand;

            builder.Clear();

            builder.Append("Console.");
            builder.Append(command.Alias);

            if (isProperty)
            {
                if (arguments is not null)
                {
                    builder.Append(" = ");
                    builder.Append(arguments[0]);
                }
            }
            else
            {
                builder.Append('(');

                if (arguments is not null)
                {
                    if (command.TakesConsoleItems)
                    {
                        result = AppendArgumentsAsConsoleItems(builder, arguments, items, ref transformed);
                    }
                    else
                    {
                        AppendArguments(builder, arguments);
                    }
                }

                builder.Append(')');
            }

            if (result == CommandResult.IsCommand)
            {
                transformed = builder.ToString();
            }

            return result;
        }

        private static CommandResult AppendArgumentsAsConsoleItems(StringBuilder builder, ImmutableArray<string> arguments, HelpItemDictionary items,
                                                                   ref string transformed)
        {
            int argsCount = arguments.Count;
            int index = 0;

            while (index < argsCount)
            {
                string argument = arguments[index];

                if (argument.Contains('.'))
                {
                    PoolableStringBuilder argBuilder = Pools.Common.StringBuilders.Get();

                    string[] split = argument.Split('.', StringSplitOptions.TrimEntries);
                    bool hasChildAccessors = split.Length > 1;

                    string baseItem = split[0];

                    if (!items.TryGetValue(baseItem, out HelpItem item))
                    {
                        Pools.Common.StringBuilders.Return(argBuilder);

                        transformed = $"{baseItem} is not a valid console item.";

                        return CommandResult.IsCommandButError;
                    }

                    argBuilder.Append($"Console.ConsoleItems[\"{baseItem}\"]");

                    if (hasChildAccessors)
                    {
                        ImmutableArray<string> currentChildItemNamesUnprefixed = item.childItemNamesNotPrefixed;
                        ViewableList<HelpItem> currentChildItems = item.ChildItems;

                        for (int splitIndex = 1; splitIndex != split.Length; splitIndex++)
                        {
                            argBuilder.Append('.');

                            string currentItem = split[splitIndex];

                            bool childFound = false;
                            int childIndex;

                            for (childIndex = 0; childIndex != currentChildItems.Count; childIndex++)
                            {
                                if (currentChildItemNamesUnprefixed[childIndex] == currentItem)
                                {
                                    childFound = true;

                                    currentChildItems = currentChildItems[childIndex].ChildItems;

                                    break;
                                }
                            }

                            if (!childFound)
                            {
                                PoolableStringBuilder currentChainBuilder = Pools.Common.StringBuilders.Get();

                                int lastValidIndex = splitIndex - 1;
                                splitIndex = 0;

                                while (splitIndex != lastValidIndex)
                                {
                                    currentChainBuilder.Append(split[splitIndex++]);
                                    currentChainBuilder.Append('.');
                                }

                                currentChainBuilder.Append(split[lastValidIndex]);

                                string currentChain = currentChainBuilder.ToString();

                                Pools.Common.StringBuilders.Return(currentChainBuilder);
                                Pools.Common.StringBuilders.Return(argBuilder);

                                transformed = $"\"{currentItem}\" is not a valid child item of \"{currentChain}\".";

                                return CommandResult.IsCommandButError;
                            }

                            argBuilder.Append($"ChildItems[{childIndex}]");
                        }
                    }

                    argument = argBuilder.builder.ToString();

                    Pools.Common.StringBuilders.Return(argBuilder);
                }
                else
                {
                    argument = $"Console.ConsoleItems[\"{argument}\"]";
                }

                builder.Append(argument);

                if (index != argsCount - 1)
                {
                    builder.Append(", ");
                }

                index++;
            }

            transformed = builder.ToString();

            return CommandResult.IsCommand;
        }

        private static void AppendArguments(StringBuilder builder, ImmutableArray<string> arguments)
        {
            int lastIndex = arguments.Count - 1;

            for (int index = 0; index != lastIndex; index++)
            {
                builder.Append(arguments[index]);
                builder.Append(", ");
            }

            builder.Append(arguments[lastIndex]);
        }

        // This will progress through the rest of text, starting from index.
        // If no arguments are found, just returns null.
        private static ImmutableArray<string> FindCommandArguments(string text, int index)
        {
            StringBuilder builder = new StringBuilder(100);

            ViewableList<string> arguments = new ViewableList<string>(10);

            while (index < text.Length)
            {
                if (text[index] == '\"')
                {
                    index++; // move past first '"'

                    TextUtils.AppendWhileNotTerminator(builder, text, ref index, text.Length, '\"');

                    index++; // move past terminating '"'
                }
                else
                {
                    TextUtils.AppendWhileNotWhitespace(builder, text, ref index, text.Length);
                }

                TextUtils.ProgressWhileWhitespace(text, ref index, text.Length);

                arguments.Add(builder.ToString());

                builder.Clear();
            }

            if (arguments.IsEmpty)
            {
                return null;
            }

            return arguments.ToImmutableArray();
        }

        // First checks if builder is equal to a command's name. If true and predicate is not null, does additional checking through predicate.
        // If predicate is null, only checks the name.
        // This will return 
        private static Command FindCommand(StringBuilder builder, ImmutableArray<Command> commands, Predicate<Command> predicate, out int matchingNamesCount)
        {
            matchingNamesCount = 0;

            if (predicate is not null)
            {
                for (int commandIndex = 0; commandIndex != commands.Count; commandIndex++)
                {
                    Command command = commands[commandIndex];

                    if (builder.Equals(command.Name))
                    {
                        matchingNamesCount++;

                        if (predicate(command))
                        {
                            return command;
                        }
                    }
                }
            }
            else
            {
                for (int commandIndex = 0; commandIndex != commands.Count; commandIndex++)
                {
                    Command command = commands[commandIndex];

                    if (builder.Equals(command.Name))
                    {
                        matchingNamesCount++;

                        return command;
                    }
                }
            }

            return null;
        }

        private static bool IsCommand(StringBuilder builder, ImmutableArray<Command> commands)
        {
            for (int commandIndex = 0; commandIndex != commands.Count; commandIndex++)
            {
                if (builder.Equals(commands[commandIndex].Name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
