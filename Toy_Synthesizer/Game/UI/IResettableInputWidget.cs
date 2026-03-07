namespace Toy_Synthesizer.Game.UI
{
    public interface IResettableInputWidget
    {
        public bool IsActive { get; }
        public bool IsChangedByUser { get; }

        public void ResetInput();
    }
}
