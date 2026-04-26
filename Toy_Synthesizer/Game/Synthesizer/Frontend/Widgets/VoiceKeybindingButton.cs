using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoInput;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.Slicing;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Data;
using GeoLib.GeoGraphics.UI.Data.Generic;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using FontStashSharp;

using Toy_Synthesizer.Game.DigitalSignalProcessing;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.UI;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    // TODO: Implement tooltip
    public class VoiceKeybindingButton : TextButton
    {
        internal VoiceKeybindingGroup parentKeybindingGroup;
        internal int keybindingKeyIndex;

        private InputListener settingKeyListener;
        private Keys pendingKey;
        private string activatedKeybindingPreviousText;

        private Keys key;

        private bool previousWasMouseEnabled;
        private bool previousWasTabFocusingEnabled;

        internal Keys Key
        {
            get => key;
            set => key = value;
        }

        public VoiceKeybindingButton(Vec2f position, Vec2f size, 
                                     string text, 
                                     TextButtonStyle style,
                                     DynamicSpriteFont font, 
                                     Alignment alignment, 
                                     bool scaleTextOnScale, 
                                     bool tintText)
            : base(position: position, 
                   size: size, 
                   font: font, 
                   text: text,
                   style: style,
                   alignment: alignment, 
                   scaleTextOnScale: scaleTextOnScale, 
                   tintText: tintText)
        {
            InitUISettingKeyListener();

            OnClick += ActivateKeybindingSetting;
        }

        internal void UpdateFromVoice()
        {
            Text = GetKeybindingUIButtonTextFromKey(Key);
        }

        private void ActivateKeybindingSetting()
        {
            previousWasMouseEnabled = Geo.Instance.Input.MouseEnabled;
            previousWasTabFocusingEnabled = Stage.CanTabFocus;

            Geo.Instance.Input.MouseEnabled = false;
            Stage.CanTabFocus = false;

            AddCaptureListener(settingKeyListener);

            activatedKeybindingPreviousText = Text;

            string newButtonText = "Press a key";

            Text = newButtonText;
        }

        private void DeactivateAndSetKeybinding()
        {
            GeoDebug.Assert(pendingKey != Keys.None && !VoiceFrontend.InvalidVoiceKeybindingKeys.Contains(pendingKey));

            key = pendingKey;

            parentKeybindingGroup.SetKey(key);

            Text = GetKeybindingUIButtonTextFromKey(key);

            ResetKeybindingActivation(setButtonPreviousText: false);
        }

        private void ResetKeybindingActivation(bool setButtonPreviousText)
        {
            if (setButtonPreviousText)
            {
                Text = activatedKeybindingPreviousText;
            }

            activatedKeybindingPreviousText = null;

            RemoveCaptureListener(settingKeyListener);

            pendingKey = Keys.None;

            Geo.Instance.Input.MouseEnabled = previousWasMouseEnabled;
            Stage.CanTabFocus = previousWasTabFocusingEnabled;
        }

        private void InitUISettingKeyListener()
        {
            settingKeyListener = new InputListener
            {
                KeyEnter = delegate (InputEvent e, Keys key)
                {
                    e.HandleAndStop();
                },

                KeyDown = delegate (InputEvent e, Keys key)
                {
                    e.HandleAndStop();

                    if (e.Keyboard.IsRepeat)
                    {
                        return;
                    }

                    if (key == Keys.Escape)
                    {
                        ResetKeybindingActivation(setButtonPreviousText: true);

                        return;
                    }

                    if (key == Keys.Back)
                    {
                        pendingKey = Keys.None;

                        DeactivateAndSetKeybinding();

                        return;
                    }

                    if (VoiceFrontend.InvalidVoiceKeybindingKeys.Contains(key))
                    {
                        return;
                    }

                    pendingKey = key;

                    DeactivateAndSetKeybinding();
                },

                KeyUp = delegate (InputEvent e, Keys key)
                {
                    e.HandleAndStop();
                },

                MouseDown = delegate (InputEvent e, float x, float y, MouseStates.Button button)
                {
                    e.HandleAndStop();
                },

                MouseUp = delegate (InputEvent e, float x, float y, MouseStates.Button button)
                {
                    e.HandleAndStop();
                },

                MouseDragged = delegate (InputEvent e, float previousX, float previousY, float x, float y, MouseStates.Button button)
                {
                    e.HandleAndStop();
                }
            };
        }

        private static string GetKeybindingUIButtonTextFromKey(Keys key)
        {
            if (key == Keys.None)
            {
                return VoiceGroup.EMPTY_KEYBINDING_DISPLAY_STRING;
            }

            return key.ToString();
        }
    }
}
