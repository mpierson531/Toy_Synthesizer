using System;

using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;

namespace Toy_Synthesizer.Game.UI
{
    public class DropDownWidget : GroupWidget
    {
        public readonly DropDownAdapter DropDownAdapter;

        public GroupWidget DropDownGroup
        {
            get => DropDownAdapter.DropDownGroup;
        }

        public bool IsShowing
        {
            get => DropDownAdapter.IsShowing;
        }

        public Button CoverButton
        {
            get => DropDownAdapter.CoverButton;
        }

        public Action<Button, int> OnSelect
        {
            get => DropDownAdapter.OnSelect;
            set => DropDownAdapter.OnSelect = value;
        }

        public DropDownWidget(Vec2f position, Vec2f size, Func<DropDownWidget, DropDownAdapter> adapterProvider)
            : base(position, size)
        {
            DropDownAdapter = adapterProvider(this);

            AdapterInitialized(DropDownAdapter);
        }

        protected virtual void AdapterInitialized(DropDownAdapter adapter)
        {

        }

        public void Unfocus()

        {
            DropDownAdapter.Unfocus();
        }

        public void Show()
        {
            DropDownAdapter.Show();
        }

        public void Hide()
        {
            DropDownAdapter.Hide();
        }
    }
}