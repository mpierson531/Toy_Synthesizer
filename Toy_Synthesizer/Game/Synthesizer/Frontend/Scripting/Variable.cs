namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Scripting
{
    public readonly struct Variable
    {
        public readonly string Name;
        public readonly object Value;

        public Variable(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
