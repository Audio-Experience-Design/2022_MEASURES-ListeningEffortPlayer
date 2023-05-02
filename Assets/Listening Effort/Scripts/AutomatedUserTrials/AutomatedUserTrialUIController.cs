using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutomatedUserTrialUIController : MonoBehaviour
{
    public GameObject uiObject;
    private Manager manager;
    public Button button;
    private Text buttonLabel;
    public Text statusText;
    public Text challengeLabelText;
    public AutomaticSessionController sessionController;

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

            switch (state)
            {
                case AutomaticSessionController.State.LoadingSession:
                    statusText.text = $"Loading session '{sessionController.session?.Name ?? "(null)"}'.";
                    button.gameObject.SetActive(false);
                    break;
                case AutomaticSessionController.State.WaitingForUserToStartChallenge:
                    statusText.text = $"";
                    buttonLabel.text = $"Start challenge";
                    button.gameObject.SetActive(true);
                    button.onClick.AddListener(() => sessionController.onUserReadyToContinue());
                    break;
                case AutomaticSessionController.State.UserReadyToStartChallenge:
                    button.gameObject.SetActive(false);
                    break;
                case AutomaticSessionController.State.PlayingVideo:
                    statusText.text = $"Listen...";
                    break;
                case AutomaticSessionController.State.RecordingUserResponse:
                    statusText.text = $"Recording your response...";
                    buttonLabel.text = $"Done";
                    button.gameObject.SetActive(true);
                    button.onClick.AddListener(() => sessionController.onUserReadyToStopRecording());
                    break;
                case AutomaticSessionController.State.AudioRecordingComplete:
                    statusText.text = $"Audio recording complete.";
                    button.gameObject.SetActive(false);
                    break;
                case AutomaticSessionController.State.Completed:
                    statusText.text = $"Session completed.";
                    button.gameObject.SetActive(false);
                    break;
            }
        };
    }

}
