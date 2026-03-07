using System.Text;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Help
{
    public sealed class Parameter
    {
        public readonly string Name;
        public readonly string Type;

        public Parameter(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public void AppendToBuilder(StringBuilder builder, bool ensureCapacity)
        {
            if (ensureCapacity)
            {
                int additionalCapacity = Name.Length + 2 + Type.Length; // Plus 2 for ': '

                builder.EnsureCapacity(builder.Capacity + additionalCapacity);
            }

            builder.Append(Name);
            builder.Append(": ");
            builder.Append(Type);
        }

        public sealed override string ToString()
        {
            return $"{Name}: {Type}";
        }
    }
}
