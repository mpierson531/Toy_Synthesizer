using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoGraphics.UI.Actions;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Microsoft.Xna.Framework;

using Toy_Synthesizer.Game.UI;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.DigitalSignalProcessing;
using Toy_Synthesizer.Game.DigitalSignalProcessing.BuiltinAudioSources;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Widgets
{
    public class RecordingControlGroup : GroupWidget
    {
        private UIManager uiManager;

        private DSP dsp;

        private readonly RecordedAudioSource recordedAudioSource;

        private bool isPlaying;
        private bool isRecording;

        private ImageButton playPauseButton;
        private ImageButton recordButton;
        private ImageButton trashButton;

        private Label durationLabel;

        private readonly UpdateDurationLabelAction updateDurationLabelAction;

        public RecordingControlGroup(Vec2f position, Vec2f size, UIManager uiManager)
            : base(position, size,
                   style: uiManager.PropertyGroupStyle,
                   positionChildren: false,
                   sizeChildren: false)
        {
            this.uiManager = uiManager;

            dsp = uiManager.Game.DSP;

            recordedAudioSource = new RecordedAudioSource(dsp);

            isPlaying = false;

            Adapters.Add(new PreciseGroupLayoutAdapter());

            string uiXml = GetUIXml();

            UIXmlParser.Parse(uiManager.Game, uiXml, rootParent: this);

            InitWidgets();

            updateDurationLabelAction = new UpdateDurationLabelAction(this);
        }

        private void InitWidgets()
        {
            playPauseButton = FindAsByNameDeepSearch<ImageButton>(PLAY_BUTTON_NAME);
            recordButton = FindAsByNameDeepSearch<ImageButton>(RECORD_BUTTON_NAME);
            trashButton = FindAsByNameDeepSearch<ImageButton>(TRASH_BUTTON_NAME);
            durationLabel = FindAsByNameDeepSearch<Label>(DURATION_LABEL_NAME);

            playPauseButton.OnClick += PlayPauseButton_OnClick;
            recordButton.OnClick += RecordButton_OnClick;
            trashButton.OnClick += TrashButton_OnClick;

            playPauseButton.Disable();
            trashButton.Disable();
        }

        private void PlayPauseButton_OnClick()
        {
            Utils.Assert(!dsp.IsRecordingAudio);

            if (isPlaying)
            {
                dsp.RemoveAudioSource(recordedAudioSource);

                isPlaying = false;

                playPauseButton.Style = uiManager.GetStyle<ImageButton.ImageButtonStyle>("PlayButtonStyle");

                RemoveAction(updateDurationLabelAction);
                updateDurationLabelAction.Reset();
            }
            else
            {
                dsp.AddAudioSource(recordedAudioSource);

                isPlaying = true;

                playPauseButton.Style = uiManager.GetStyle<ImageButton.ImageButtonStyle>("PauseButtonStyle");

                AddAction(updateDurationLabelAction);
            }
        }

        private void RecordButton_OnClick()
        {
            if (dsp.IsRecordingAudio)
            {
                dsp.StopRecordingAudio();

                isRecording = false;

                recordButton.Style = uiManager.GetStyle<ImageButton.ImageButtonStyle>("RecordButtonStyle");

                trashButton.Enable();
                playPauseButton.Enable();

                RemoveAction(updateDurationLabelAction);
                updateDurationLabelAction.Reset();
            }
            else
            {
                dsp.BeginRecordingAudio();

                isRecording = true;

                recordButton.Style = uiManager.GetStyle<ImageButton.ImageButtonStyle>("StopButtonStyle");

                trashButton.Disable();
                playPauseButton.Disable();

                AddAction(updateDurationLabelAction);
            }
        }

        private void TrashButton_OnClick()
        {
            if (dsp.IsRecordingAudio)
            {
                Utils.Assert(!isPlaying);

                recordButton.Click();
            }
            else if (isPlaying)
            {
                playPauseButton.Click();
            }

            dsp.RemoveAudioSource(recordedAudioSource);

            dsp.ClearRecordedAudio();

            playPauseButton.Disable();
            trashButton.Disable();

            durationLabel.Text = "Duration: 0.0";
        }

        private string GetUIXml()
        {
            // This mostly assumes that Size.X is greater than Size.Y.

            return
            $@"<Layout>

                <ImageButton
                 Position=""(22.5%, 7.5%)""
                 Size=""(87.5%)""
                 SizeMode=""Min""
                 Style=""TrashButtonStyle""
                 ScaleImageOnScale=""true""
                 Name=""{TRASH_BUTTON_NAME}""/>

                <ImageButton
                 Position=""(45%, 7.5%)""
                 Size=""(87.5%)""
                 SizeMode=""Min""
                 Style=""RecordButtonStyle""
                 ScaleImageOnScale=""true""
                 ImagePosition=""0.125""
                 ImageSize=""0.75""
                 Name=""{RECORD_BUTTON_NAME}""/>

                <Label
                 Position=""(0.5%, 70%)""
                 Size=""(17.5%, 25%)""
                 Text=""Duration: 0.0""
                 Style=""BackgroundedPropertyLabelStyle""
                 Name=""{DURATION_LABEL_NAME}""/>
                
                <ImageButton
                 Position=""(67.5%, 7.5%)""
                 Size=""(87.5%)""
                 SizeMode=""Min""
                 Style=""PlayButtonStyle""
                 ScaleImageOnScale=""true""
                 Name=""{PLAY_BUTTON_NAME}""/>

            </Layout>";
        }

        private const string PLAY_BUTTON_NAME = "PlayPauseButton";
        private const string RECORD_BUTTON_NAME = "RecordButton";
        private const string TRASH_BUTTON_NAME = "TrashButton";
        private const string DURATION_LABEL_NAME = "DurationLabel";

        private sealed class UpdateDurationLabelAction : ActorAction
        {
            private readonly RecordingControlGroup recordingControlGroup;
            private readonly RecordedAudioSource recordedAudioSource;

            public UpdateDurationLabelAction(RecordingControlGroup recordingControlGroup)
            {
                this.recordingControlGroup = recordingControlGroup;
                recordedAudioSource = recordingControlGroup.recordedAudioSource;
            }

            public override bool Act(float delta)
            {
                if (!recordingControlGroup.isPlaying && !recordingControlGroup.isRecording)
                {
                    return true;
                }

                if (recordingControlGroup.isPlaying)
                {
                    recordingControlGroup.durationLabel.Text = $"{recordedAudioSource.PlaybackPositionSeconds} / {recordedAudioSource.Duration}";
                }
                else if (recordingControlGroup.isRecording)
                {
                    recordingControlGroup.durationLabel.Text = $"Duration: {recordedAudioSource.Duration}";
                }

                return false;
            }
        }
    }
}
