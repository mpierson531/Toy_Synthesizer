using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Xna.Framework.Input;

using GeoLib;
using GeoLib.GeoInput;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    public class VoiceKeybindingGroup : GroupWidget
    {
        private VoiceGroup parentVoiceGroup;

        private VoiceKeybindingButton keybindingButton;

        private Button removeButton;
        private LabelTooltip removeButtonTooltip;

        private KeyBinding.Key previousKey;
        private KeyBinding.Key key;

        internal int keybindingIndex;

        // Should only be assigned from VoiceGroup, when updating from a new voice or when an empty key is newly added or otherwise updated internally.
        internal KeyBinding.Key Key
        {
            get => key;
            
            set
            {
                key = value;

                previousKey = key;

                keybindingButton.Key = key.key;
            }
        }

        internal KeyBinding.Key PreviousKey
        {
            get => previousKey;
        }

        public VoiceKeybindingGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   positionChildren: false,
                   sizeChildren: false)
        {
            Adapters.Add(new PreciseGroupLayoutAdapter());

            InitWidgets(uiManager);
        }

        // Should only be called by a VoiceKeybindingButton.
        internal void SetKey(Keys key)
        {
            this.previousKey = this.key;

            this.key = new KeyBinding.Key(key, this.key.mode, this.key.KeyComboMode);

            parentVoiceGroup.SetKey(this);
        }

        internal void UpdateFromVoice()
        {
            keybindingButton.UpdateFromVoice();
        }

        private void InitWidgets(UIManager uiManager)
        {
            UIXmlParser uiXmlParser = new UIXmlParser(uiManager.Game);
            uiXmlParser.AddTypeFactory(new VoiceKeybindingButtonFactory());

            string uiXml = GetUIXml();

            uiXmlParser.Parse(uiXml, rootParent: this);

            keybindingButton = FindAsByNameDeepSearch<VoiceKeybindingButton>(KeybindingButtonName);
            removeButton = FindAsByNameDeepSearch<Button>(RemoveButtonName);

            keybindingButton.parentKeybindingGroup = this;

            removeButton.OnClick += RemoveButton_OnClick;

            removeButtonTooltip = uiManager.AddTextTooltip(removeButton, "Click to remove this keybinding");
        }

        private void RemoveButton_OnClick()
        {
            parentVoiceGroup.RemoveKeyFromKeybinding(this);

            if (removeButtonTooltip?.IsShowing ?? false)
            {
                removeButtonTooltip.Hide();
            }
        }

        protected override void ParentChanged(GroupWidget previousParent, GroupWidget newParent)
        {
            base.ParentChanged(previousParent, newParent);

            parentVoiceGroup = VoiceGroup.FindParentVoiceGroup(this);

            if (newParent is not null && parentVoiceGroup is null)
            {
                throw new InvalidOperationException("No parent voice group found in hierarchy.");
            }
        }

        private string GetUIXml()
        {
            return
            $@"<Layout>

                <VoiceKeybindingButton
                 Position=""(2.5%, 16.67%)""
                 Size=""(512.5%, 66.66%)""
                 Text=""{VoiceGroup.EMPTY_KEYBINDING_DISPLAY_STRING}""
                 Alignment=""Center""
                 SizeMode=""Min""
                 FitText=""false""
                 Name=""{KeybindingButtonName}""/>

                <TextButton
                 Position=""(85%, 24.975%)""
                 Size=""(66.4335%, 49.95%)""
                 Text=""-""
                 Alignment=""Center""
                 SizeMode=""Min""
                 FitText=""false""
                 Name=""{RemoveButtonName}""/>

                

            </Layout>";
        }

        private const string KeybindingButtonName = "KeybindingButton";
        private const string RemoveButtonName = "RemoveButton";

        private class VoiceKeybindingButtonFactory : UIXmlParser.TypeFactory
        {
            public VoiceKeybindingButtonFactory() : base("VoiceKeybindingButton")
            {

            }

            public override Widget Create(Game game, UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
            {
                UIXmlParser.TryGetString(attributes, "text", out string text);
                UIXmlParser.TryGetAlignment(attributes, out Alignment alignment);

                TextButton.TextButtonStyle style = Copyables.Cast<TextButton.TextButtonStyle>(uiManager.TextButtonStyle, deepCopy: true);

                VoiceKeybindingButton button = new VoiceKeybindingButton(position, size, text, style, uiManager.MainFont, alignment,
                                                                         scaleTextOnScale: false,
                                                                         tintText: false);

                uiManager.ApplyButtonUXData(button, uiManager.DefaultButtonUXData);

                return button;
            }
        }

    }
}
