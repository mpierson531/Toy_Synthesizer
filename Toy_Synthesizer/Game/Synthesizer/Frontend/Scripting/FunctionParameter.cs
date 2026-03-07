using System;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Scripting
{
    public class FunctionParameter
    {
        public readonly string Name;
        public readonly Type Type;

        public FunctionParameter(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}
