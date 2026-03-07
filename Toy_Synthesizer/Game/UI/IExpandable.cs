namespace Toy_Synthesizer.Game.UI
{
    public interface IExpandable
    {
        bool IsExpanded { get; }

        void Expand();
        void Collapse();
    }
}
