using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using UnityEngine.Video;

public class SessionController : MonoBehaviour
{
    Session session;

    public VideoCatalogue videoCatalogue;
    public AudioRecorder audioRecorder;

    public VideoPlayer skyboxVideoPlayer;
    public VideoManager[] videoManagers;
    public GameObject[] babblePrefabs;

    public Text statusText;
    public Button startButton;
    public Button recordResponseButton;
    public Button continueButton;

    public event EventHandler<State> stateChanged;
    /// current number (0 indexed), total number
    public event EventHandler<(int current, int total)> challengeNumberChanged;

    public enum State
    {
        NotYetLoaded,
        WaitingToStart,
        PlayingVideo,
        RecordingUserResponse,
        WaitingForUserToContinue,
        UserRequestedToContinue,
        Completed,
    }
    private State state = State.NotYetLoaded;
    private int numVideosPlaying = 0;

    private void Start()
    {
        string yamlText = Resources.Load<TextAsset>("SampleAutomatedSession").text;
        session = Session.LoadFromYaml(yamlText, videoCatalogue);
        Debug.Assert(session.IdleVideos.Count() == 3);
        Debug.Assert(videoManagers.Count() == 3);
        Debug.Log($"Loaded SampleAutomatedSession.yaml");

        for (int i=0; i<3; i++)
        {
            videoManagers[i].playbackFinished += (_, _) =>
            {
                numVideosPlaying--;
                Debug.Assert(0 <= numVideosPlaying || numVideosPlaying < 3);
            };
        }
        audioRecorder.recordingFinished => (_,_) =>
        {
            Debug.Assert(state == State.RecordingUserResponse);
            advanceState(State.WaitingForUserToContinue);
        }
    }

    private void setState(State state)
    {
        this.state = state;
        Debug.Log($"State changed from {this.state} to {state}");
        stateChanged?.Invoke(this, state);
    }

    public void advanceState(State expectedState)
    {
        switch (state)
        {
            case State.NotYetLoaded:
                throw new Exception($"advanceState called before SessionController loaded");
            case State.WaitingToStart:
                state = State.PlayingVideo; break;
            case State.PlayingVideo:
                state = State.RecordingUserResponse; break;
            case State.RecordingUserResponse:
                state = State.WaitingForUserToContinue; break;
            case State.UserRequestedToContinue:
                state = State.WaitingForUserToContinue;
                break;
            case State.Completed:
                throw new Exception($"Cannot advance state as session has completed.");
        }
        if (state != expectedState)
        {
            throw new Exception($"Unexpected state change. Expected state {expectedState}. Actual state {state}.");
        }
    }

    public IEnumerator StartSession()
    {
        Debug.Log($"Starting automated trial session: {session.Name}");

        string timestamp = localDateTime.ToString("yyyy-MM-dd_HH-mm-ss");
        audioRecorder.subfolder = $"{timestamp} {session.Name}";

        // Speaker Amplitude
        videoManagers.ToList().ForEach(vm => vm.audioSource.volume = session.SpeakerAmplitude);

        // MaskingVideo
        videoCatalogue.SetPlayerSource(skyboxVideoPlayer, session.MaskingVideo);
        skyboxVideoPlayer.Play();

        // Maskers
        if (session.Maskers.Count() > babblePrefabs.Count())
        {
            throw new System.Exception($"There are {session.Maskers.Count()} maskers defined in YAML but only {babblePrefabs.Count()} babble sources available.");
        }
        for (int i = 0; i < session.Maskers.Count(); i++)
        {
            babblePrefabs[i].GetComponent<AudioSource>().volume = session.Maskers[i].Amplitude;
            babblePrefabs[i].transform.localRotation = Quaternion.Euler(0, session.Maskers[i].Rotation, 0);
        }
        for (int i = session.Maskers.Count(); i < babblePrefabs.Count(); i++)
        {
            babblePrefabs[i].SetActive(false);
        }

        // Idle videos
        for (int i = 0; i < 3; i++)
        {
            videoManagers[i].idleVideoName = session.IdleVideos[i];
        }

        setState(State.WaitingToStart);

        while (state == State.WaitingToStart)
        {
            yield return null;
        }

        for (int i = 0; i < session.Maskers.Count(); i++)
        {
            string challengeLabel = (i + 1).ToString();
            Debug.Assert(state == State.PlayingVideo);
            Debug.Assert(numVideosPlaying == 0);
            for (int k=0; k<3; k++)
            {
                numVideosPlaying++;
                videoManagers[k].PlayVideo(session.Challenges[i][k]);
            }
            while (numVideosPlaying > 0)
            {
                yield return null;
            }
            audioRecorder.StartRecording($"{timestamp}_{i}.wav", session.MaximumRecordingDuration);
            setState(State.RecordingUserResponse);
            while (state == State.RecordingUserResponse)
            {
                yield return null;
            }

            // CONTINUE FROM HERE: LOG THE DATA, CARRY ON. THEN HOOK UP THE UI
        }



    }
}
