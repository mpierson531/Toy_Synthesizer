using GeoLib.GeoUtils;

using System.Text;

using Toy_Synthesizer.Game.Synthesizer.Frontend.Help;

namespace Toy_Synthesizer.Game.Plugins.Builtin.Core.Help
{
    public static class HelpItemHelpers
    {
        public static void AppendToBuilder(HelpItem item, StringBuilder builder, int tabs, int newLinesBefore, int newLinesAfter)
        {
            string tab = Game.Tab;
            string fullTab = Utils.EmptyString;

            if (tabs != 0)
            {
                fullTab = Game.GetTabs(tabs) + tab;
            }

            if (newLinesBefore > 0)
            {
                Game.AppendNewLines(builder, newLinesBefore);
            }

            // Probably exaggerated, but easier than trying to be precise.
            builder.EnsureCapacity(500 + (newLinesBefore * Game.NewLineLength) + (newLinesAfter * Game.NewLineLength) + (tabs * Game.Tab.Length));

            builder.Append("HelpItem");
            builder.Append(Game.NewLine);
            builder.Append(fullTab);
            builder.Append('{');
            builder.Append(Game.NewLine);
            builder.Append(fullTab);
            builder.Append(tab);

            builder.Append("Name: ");
            builder.Append(item.Name);

            builder.Append(',');

            builder.Append(Game.NewLine);
            builder.Append(fullTab);
            builder.Append(tab);

            builder.Append("Type: ");
            builder.Append(item.Type);

            builder.Append(',');

            builder.Append(Game.NewLine);
            builder.Append(fullTab);
            builder.Append(tab);

            builder.Append("Description: ");
            builder.Append(item.Description);

            builder.Append(',');

            builder.Append(Game.NewLine);
            builder.Append(fullTab);
            builder.Append(tab);

            if (item.Parameters is not null && item.Parameters.Count != 0)
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

                for (int index = 0; index != item.Parameters.Count; index++)
                {
                    item.Parameters[index].AppendToBuilder(builder, ensureCapacity: false);

                    if (index != item.Parameters.Count - 1)
                    {
                        builder.Append(Game.NewLine);
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

                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);
            }

            builder.Append("Implementation Type: ");
            builder.Append(item.ImplType.ToString());

            if (item.ImplType != ImplementationType.Function && item.ImplType != ImplementationType.Type)
            {
                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);

                builder.Append("Is Readonly: ");
                builder.Append(item.IsReadonly);
            }

            if (item.ImplType != ImplementationType.Function) // A function will never not be readonly, and will never have child items
            {
                if (item.ChildItems is not null && item.ChildItems.Count != 0)
                {
                    builder.Append(Game.NewLine);
                    builder.Append(fullTab);
                    builder.Append(tab);

                    builder.Append("ChildItems: [");

                    for (int childIndex = 0; childIndex != item.ChildItems.Count - 1; childIndex++)
                    {
                        builder.Append(item.ChildItems[childIndex].Name);
                        builder.Append(", ");
                    }

                    builder.Append(item.ChildItems[item.ChildItems.Count - 1].Name);
                    builder.Append(']');
                }

                /*if (ChildItems is not null)
                {
                    builder.Append(tab);

                    for (int childIndex = 0; childIndex != ChildItems.Count; childIndex++)
                    {
                        ChildItems[childIndex].AppendToBuilder(builder, tabs, newLinesFirst, newLinesAfter);
                    }
                }*/
            }

            if (item.ImplType == ImplementationType.Type && item.ConstructorExamples is not null && item.ConstructorExamples.Count != 0)
            {
                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);

                builder.Append("Constructor Examples:");
                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);
                builder.Append('{');
                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);
                builder.Append(tab);

                for (int index = 0; index != item.ConstructorExamples.Count; index++)
                {
                    builder.Append(item.ConstructorExamples[index]);

                    if (index != item.ConstructorExamples.Count - 1)
                    {
                        builder.Append(Game.NewLine);
                        builder.Append(fullTab);
                        builder.Append(tab);
                        builder.Append(tab);
                    }
                }

                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);
                builder.Append('}');
            }

            if (item.UsageExamples is not null && item.UsageExamples.Count != 0)
            {
                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);

                builder.Append("Usage Examples:");
                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);
                builder.Append('{');
                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);
                builder.Append(tab);

                for (int index = 0; index != item.UsageExamples.Count; index++)
                {
                    builder.Append(item.UsageExamples[index]);

                    if (index != item.UsageExamples.Count - 1)
                    {
                        builder.Append(Game.NewLine);
                        builder.Append(fullTab);
                        builder.Append(tab);
                        builder.Append(tab);
                    }
                }

                builder.Append(Game.NewLine);
                builder.Append(fullTab);
                builder.Append(tab);
                builder.Append('}');
            }

            builder.Append(Game.NewLine);
            builder.Append(fullTab);

            builder.Append('}');

            if (newLinesAfter > 0)
            {
                Game.AppendNewLines(builder, newLinesAfter);
            }
        }
    }
}
