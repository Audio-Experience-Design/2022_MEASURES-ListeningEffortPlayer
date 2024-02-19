using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScriptedSessionUIController : MonoBehaviour
{
    public GameObject uiObject;
    private Manager manager;
    public Button button;
    private Text buttonLabel;
    public Text statusText;
    public Text challengeLabelText;
    public ScriptedSessionController sessionController;

    void Awake()
    {
        // run even when gameobject is inactive

        manager = FindObjectOfType<Manager>();
        manager.onStateChanged += (sender, state) =>
        {
            uiObject.SetActive(state == Manager.State.RunningAutomatedSession);
        };
        uiObject.SetActive(manager.state == Manager.State.RunningAutomatedSession);
    }

    private void Start()
    {
        buttonLabel = button.GetComponentInChildren<Text>();
        sessionController.challengeNumberChanged += (sender, args) => challengeLabelText.text = $"{args.currentLabel} of {args.total}";

        sessionController.stateChanged += (sender, state) =>
        {
            // assert nothing else is listening to this button before we wipe the listeners
            Debug.Assert(button.onClick.GetPersistentEventCount() <= 1);
            // set up new listener each time state changes
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => sessionController.onUserReadyToContinue());

            Dictionary<string, string> texts = sessionController.session.UserInterfaceTexts;
            switch (state)
            {
                case ScriptedSessionController.State.LoadingSession:
                    statusText.text = $"Loading session '{sessionController.session?.Name ?? "(null)"}'.";
                    button.gameObject.SetActive(false);
                    break;
                case ScriptedSessionController.State.WaitingForUserToStartBrightnessCalibration:
                    statusText.text = texts["PromptToStartBrightnessCalibration"];
                    button.gameObject.SetActive(true);
                    buttonLabel.text = $"Start";
                    break;
                case ScriptedSessionController.State.PerformingBrightnessCalibration:
                    statusText.text = "";
                    button.gameObject.SetActive(false);
                    break;
                case ScriptedSessionController.State.WaitingForUserToStartChallenges:
                    statusText.text = texts["PromptToStartChallenges"];
                    buttonLabel.text = $"Start";
                    button.gameObject.SetActive(true);
                    break;
                case ScriptedSessionController.State.UserReadyToStartChallenges:
                    button.gameObject.SetActive(false);
                    break;
                case ScriptedSessionController.State.DelayingBeforePlayingVideo:
                    statusText.text = texts["DelayBeforePlayingVideo"];
                    button.gameObject.SetActive(false);
                    break;
                case ScriptedSessionController.State.PlayingVideo:
                    statusText.text = texts["DuringVideoPlayback"];
                    button.gameObject.SetActive(false);
                    break;
                case ScriptedSessionController.State.DelayingAfterPlayingVideos:
                    statusText.text = texts["DelayAfterPlayingVideo"];
                    button.gameObject.SetActive(false);
                    break;
                case ScriptedSessionController.State.RecordingUserResponse:
                    statusText.text = texts["RecordingUserResponse"];
                    button.gameObject.SetActive(false);
                    break;
                case ScriptedSessionController.State.AudioRecordingComplete:
                    statusText.text = $"Processing recording. Please wait...";
                    button.gameObject.SetActive(false);
                    break;
                case ScriptedSessionController.State.Completed:
                    statusText.text = texts["SessionCompletion"];
                    button.gameObject.SetActive(false);
                    break;
            }
        };
    }

}
