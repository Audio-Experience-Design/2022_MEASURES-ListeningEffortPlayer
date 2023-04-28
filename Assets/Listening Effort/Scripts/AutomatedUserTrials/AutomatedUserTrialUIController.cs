using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutomatedUserTrialUIController : MonoBehaviour
{
    public Button startRecordingButton, stopRecordingButton;
    public Text statusText;

    public AudioRecorder audioRecorder;
    public string fileName = "recordedAudio.wav";

    void Start()
    {
        //var startRecordingButton = GetComponent<UIDocument>().rootVisualElement.Q<Button>("startRecordingButton");
        //var stopRecordingButton = GetComponent<UIDocument>().rootVisualElement.Q<Button>("stopRecordingButton");

        startRecordingButton.onClick.AddListener(() => audioRecorder.StartRecording(fileName, 5));
        stopRecordingButton.onClick.AddListener(audioRecorder.StopRecording);
        audioRecorder.onRecordingFinished += (sender, filename) => statusText.text = $"{DateTime.Now.ToString()}: Finished recording to {fileName}";
    }
}
