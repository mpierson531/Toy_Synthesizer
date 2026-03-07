using System;

using GeoLib.GeoUtils.Collections;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Scripting
{
    public class FunctionMetaData
    {
        public readonly string Name;
        public readonly ImmutableArray<FunctionParameter> Parameters;
        public readonly Type ReturnType;

        public FunctionMetaData(string name, ImmutableArray<FunctionParameter> parameters, Type returnType)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returnType;
        }
    }
}
