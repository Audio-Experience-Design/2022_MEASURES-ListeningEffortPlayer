using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Video;

public class ScriptedSessionController : MonoBehaviour
{
    public Session session { get; private set; }

    public VideoCatalogue videoCatalogue;
    public AudioRecorder audioRecorder;

    public VideoPlayer skyboxVideoPlayer;
    public VideoManager[] videoManagers;
    public GameObject[] babblePrefabs;

    public event EventHandler<State> stateChanged;
    /// current number (0 indexed), current label (1 indexed), total number
    public event EventHandler<(int current, string currentLabel, int total)> challengeNumberChanged;

    struct SessionEventLogEntry
    {
        public string Timestamp { get; set; }
        public string SessionTime { get; set; }
        public string EventName { get; set; }
        public string ChallengeNumber { get; set; }
        public string LeftVideo { get; set; }
        public string MiddleVideo { get; set; }
        public string RightVideo { get; set; }
        public string UserResponseAudioFile { get; set; }
        //public float headPositionEulerX { get; set; }
        //public float headPositionEulerY { get; set; }
        //public float headPositionEulerZ { get; set; }
        //public float leftPupilPositionX { get; set; }
        //public float leftPupilPositionY { get; set; }
        //public float rightPupilPositionX { get; set; }
        //public float rightPupilPositionY { get; set; } 
        //public float leftPupilDiameterMm { get; set; }
        //public float rightPupilDiameterMm { get; set; }

    }
    public enum State
    {
        Inactive,
        LoadingSession,
        WaitingForUserToStartChallenge,
        UserReadyToStartChallenge,
        PlayingVideo,
        RecordingUserResponse,
        AudioRecordingComplete,
        Completed,
    }
    private State _state = State.Inactive;
    public State state
    {
        get => _state; private set
        {
            Debug.Log($"State changed from {this._state} to {value}");
            _state = value;
            stateChanged?.Invoke(this, _state);
        }
    }
    private int numVideosPlaying = 0;

    void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            videoManagers[i].playbackFinished += (_, _) =>
            {
                if (state != State.Inactive)
                {
                    numVideosPlaying--;
                    Debug.Assert(0 <= numVideosPlaying || numVideosPlaying < 3);
                };
            };
        }
        audioRecorder.recordingFinished += (_, _) =>
        {
            Debug.Assert(state == State.RecordingUserResponse);
            advanceStateTo(State.AudioRecordingComplete);
        };
    }

    // yamlPath should be an absolute path including extension
    public void StartSession(string yamlPath)
    {
        session = Session.LoadFromYamlPath(yamlPath, videoCatalogue);
        Debug.Assert(session.IdleVideos.Count() == 3);
        Debug.Assert(videoManagers.Count() == 3);
        Debug.Log($"Loaded {yamlPath}.yaml");

        StartCoroutine(SessionCoroutine());
    }


    public void onUserReadyToContinue()
    {
        Debug.Assert(state == State.WaitingForUserToStartChallenge);
        advanceStateTo(State.UserReadyToStartChallenge);
    }

    public void onUserReadyToStopRecording()
    {
        Debug.Assert(state == State.RecordingUserResponse);
        Debug.Assert(audioRecorder.isRecording);
        audioRecorder.StopRecording();

    }

    private void advanceStateTo(State expectedNewState)
    {
        switch (state)
        {
            case State.LoadingSession:
                Debug.Assert(expectedNewState == State.WaitingForUserToStartChallenge || expectedNewState == State.Completed);
                state = expectedNewState; break;
            case State.WaitingForUserToStartChallenge:
                state = State.UserReadyToStartChallenge; break;
            case State.UserReadyToStartChallenge:
                state = State.PlayingVideo; break;
            case State.PlayingVideo:
                state = State.RecordingUserResponse; break;
            case State.RecordingUserResponse:
                state = State.AudioRecordingComplete; break;
            case State.AudioRecordingComplete:
                Debug.Assert(expectedNewState == State.WaitingForUserToStartChallenge || expectedNewState == State.Completed);
                state = expectedNewState; break;
            case State.Completed:
                throw new Exception($"Cannot advance state as session has completed.");
        }
        if (state != expectedNewState)
        {
            Debug.LogWarning($"Unexpected state change. Expected state {expectedNewState}. Actual state {state}.");
        }
    }



    private IEnumerator SessionCoroutine()
    {
        Debug.Log($"Starting automated trial session: {session.Name}");
        state = State.LoadingSession;

        DateTime sessionStartTimeUTC = DateTime.UtcNow;
        string localTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string subjectLabel = PlayerPrefs.GetString("subjectLabel", "");
        string sessionLabel = $"{localTimestamp}_{session.Name}{(subjectLabel != "" ? "_" : "")}{subjectLabel}";
        audioRecorder.subfolder = sessionLabel;
        string sessionFolder = Path.Join(Application.persistentDataPath, sessionLabel);
        Directory.CreateDirectory(sessionFolder);
        File.WriteAllText(Path.Join(sessionFolder, "session.yaml"), localTimestamp);

        using var sessionEventLogWriter = new StreamWriter(Path.Join(sessionFolder, $"{sessionLabel}.csv"), true, Encoding.UTF8);


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
            Debug.Assert(babblePrefabs[i].GetComponentsInChildren<AudioSource>().Count() == 1);
            babblePrefabs[i].GetComponentInChildren<AudioSource>().volume = session.Maskers[i].Amplitude;
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

        LogUtilities.writeCSVLine(sessionEventLogWriter, new SessionEventLogEntry
        {
            Timestamp = LogUtilities.localTimestamp(),
            SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
            EventName = "Trial started",
        });


        for (int i = 0; i < session.Maskers.Count(); i++)
        {
            advanceStateTo(State.WaitingForUserToStartChallenge);

            while (state == State.WaitingForUserToStartChallenge)
            {
                yield return null;
            }

            Debug.Assert(state == State.UserReadyToStartChallenge);

            string challengeLabel = (i + 1).ToString();
            challengeNumberChanged?.Invoke(this, (current: i, currentLabel: challengeLabel, total: session.Maskers.Count()));
            Debug.Assert(numVideosPlaying == 0);

            advanceStateTo(State.PlayingVideo);

            LogUtilities.writeCSVLine(sessionEventLogWriter, new SessionEventLogEntry
            {
                Timestamp = LogUtilities.localTimestamp(),
                SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
                EventName = "Playing videos",
                ChallengeNumber = challengeLabel,
                LeftVideo = session.Challenges[i][0],
                MiddleVideo = session.Challenges[i][1],
                RightVideo = session.Challenges[i][2],
            });
            for (int k = 0; k < 3; k++)
            {
                numVideosPlaying++;
                videoManagers[k].PlayVideo(session.Challenges[i][k]);
            }
            while (numVideosPlaying > 0)
            {
                yield return null;
            }

            advanceStateTo(State.RecordingUserResponse);

            string userResponseAudioFile = $"{sessionLabel}_{i:000}.wav";
            LogUtilities.writeCSVLine(sessionEventLogWriter, new SessionEventLogEntry
            {
                Timestamp = LogUtilities.localTimestamp(),
                SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
                EventName = "Recording response",
                ChallengeNumber = challengeLabel,
                LeftVideo = session.Challenges[i][0],
                MiddleVideo = session.Challenges[i][1],
                RightVideo = session.Challenges[i][2],
            });
            audioRecorder.StartRecording(userResponseAudioFile, session.MaximumRecordingDuration);
            while (state == State.RecordingUserResponse)
            {
                yield return null;
            }

            Debug.Assert(state == State.AudioRecordingComplete);

            LogUtilities.writeCSVLine(sessionEventLogWriter, new SessionEventLogEntry
            {
                Timestamp = LogUtilities.localTimestamp(),
                SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
                EventName = "Response received",
                ChallengeNumber = challengeLabel,
                LeftVideo = session.Challenges[i][0],
                MiddleVideo = session.Challenges[i][1],
                RightVideo = session.Challenges[i][2],
                UserResponseAudioFile = userResponseAudioFile,
            });
        }

        advanceStateTo(State.Completed);

        LogUtilities.writeCSVLine(sessionEventLogWriter, new SessionEventLogEntry
        {
            Timestamp = LogUtilities.localTimestamp(),
            SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
            EventName = "Trial completed",
        });
    }


}
