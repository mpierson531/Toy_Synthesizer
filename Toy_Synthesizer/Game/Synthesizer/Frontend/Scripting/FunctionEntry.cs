using System;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Scripting
{
    public readonly struct FunctionEntry
    {
        public readonly FunctionMetaData MetaData;
        public readonly Delegate Value;

        public FunctionEntry(FunctionMetaData metaData, Delegate value)
        {
            MetaData = metaData;
            Value = value;
        }
    }
}
