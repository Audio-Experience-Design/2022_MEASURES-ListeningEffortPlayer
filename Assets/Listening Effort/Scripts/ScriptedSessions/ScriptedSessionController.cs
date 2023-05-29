using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Video;

using PupilometryData = Tobii.XR.TobiiXR_AdvancedEyeTrackingData;


public class ScriptedSessionController : MonoBehaviour
{
    public Session session { get; private set; }

    public VideoCatalogue videoCatalogue;
    public AudioRecorder audioRecorder;
    public Pupilometry pupilometry;
    public TransformWatcher headTransform;

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

        public float HeadRotationEulerX { get; set; }
        public float HeadRotationEulerY { get; set; }
        public float HeadRotationEulerZ { get; set; }

        public long PupilometrySystemTimestamp { get; set; }
        public long PupilometryDeviceTimestamp { get; set; }
        public bool LeftIsBlinking { get; set; }
        public bool RightIsBlinking { get; set; }
        public bool LeftPupilDiameterValid { get; set; }
        public float LeftPupilDiameter { get; set; }
        public bool RightPupilDiameterValid { get; set; }
        public float RightPupilDiameter { get; set; }
        public bool LeftPositionGuideValid { get; set; }
        public float LeftPositionGuideX { get; set; }
        public float LeftPositionGuideY { get; set; }
        public bool RightPositionGuideValid { get; set; }
        public float RightPositionGuideX { get; set; }
        public float RightPositionGuideY { get; set; }
        public bool LeftGazeRayIsValid { get; set; }
        public float LeftGazeRayOriginX { get; set; }
        public float LeftGazeRayOriginY { get; set; }
        public float LeftGazeRayOriginZ { get; set; }
        public float LeftGazeRayDirectionX { get; set; }
        public float LeftGazeRayDirectionY { get; set; }
        public float LeftGazeRayDirectionZ { get; set; }
        public bool RightGazeRayIsValid { get; set; }
        public float RightGazeRayOriginX { get; set; }
        public float RightGazeRayOriginY { get; set; }
        public float RightGazeRayOriginZ { get; set; }
        public float RightGazeRayDirectionX { get; set; }
        public float RightGazeRayDirectionY { get; set; }
        public float RightGazeRayDirectionZ { get; set; }
        public bool ConvergenceDistanceIsValid { get; set; }
        public float ConvergenceDistance { get; set; }
        public bool GazeRayIsValid { get; set; }
        public float GazeRayOriginX { get; set; }
        public float GazeRayOriginY { get; set; }
        public float GazeRayOriginZ { get; set; }
        public float GazeRayDirectionX { get; set; }
        public float GazeRayDirectionY { get; set; }
        public float GazeRayDirectionZ { get; set; }

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
    private StreamWriter sessionEventLogWriter;

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

        EventHandler<PupilometryData> pupilometryCallback = (object sender, PupilometryData data) =>
        {
            LogUtilities.writeCSVLine(sessionEventLogWriter, new SessionEventLogEntry
            {
                Timestamp = LogUtilities.localTimestamp(),
                SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
                EventName = "Pupilometry",
                PupilometrySystemTimestamp = data.SystemTimestamp,
                PupilometryDeviceTimestamp = data.DeviceTimestamp,
                LeftIsBlinking = data.Left.IsBlinking,
                RightIsBlinking = data.Right.IsBlinking,
                LeftPupilDiameterValid = data.Left.PupilDiameterValid,
                LeftPupilDiameter = data.Left.PupilDiameter,
                RightPupilDiameterValid = data.Right.PupilDiameterValid,
                RightPupilDiameter = data.Right.PupilDiameter,
                LeftPositionGuideValid = data.Left.PositionGuideValid,
                LeftPositionGuideX = data.Left.PositionGuide.x,
                LeftPositionGuideY = data.Left.PositionGuide.y,
                RightPositionGuideValid = data.Right.PositionGuideValid,
                RightPositionGuideX = data.Right.PositionGuide.x,
                RightPositionGuideY = data.Right.PositionGuide.y,
                LeftGazeRayIsValid = data.Left.GazeRay.IsValid,
                LeftGazeRayOriginX = data.Left.GazeRay.Origin.x,
                LeftGazeRayOriginY = data.Left.GazeRay.Origin.y,
                LeftGazeRayOriginZ = data.Left.GazeRay.Origin.z,
                LeftGazeRayDirectionX = data.Left.GazeRay.Direction.x,
                LeftGazeRayDirectionY = data.Left.GazeRay.Direction.y,
                LeftGazeRayDirectionZ = data.Left.GazeRay.Direction.z,
                RightGazeRayIsValid = data.Right.GazeRay.IsValid,
                RightGazeRayOriginX = data.Right.GazeRay.Origin.x,
                RightGazeRayOriginY = data.Right.GazeRay.Origin.y,
                RightGazeRayOriginZ = data.Right.GazeRay.Origin.z,
                RightGazeRayDirectionX = data.Right.GazeRay.Direction.x,
                RightGazeRayDirectionY = data.Right.GazeRay.Direction.y,
                RightGazeRayDirectionZ = data.Right.GazeRay.Direction.z,
                ConvergenceDistanceIsValid = data.ConvergenceDistanceIsValid,
                ConvergenceDistance = data.ConvergenceDistance,
                GazeRayIsValid = data.GazeRay.IsValid,
                GazeRayOriginX = data.GazeRay.Origin.x,
                GazeRayOriginY = data.GazeRay.Origin.y,
                GazeRayOriginZ = data.GazeRay.Origin.z,
                GazeRayDirectionX = data.GazeRay.Direction.x,
                GazeRayDirectionY = data.GazeRay.Direction.y,
                GazeRayDirectionZ = data.GazeRay.Direction.z,
            });
        };
        pupilometry.DataChanged += pupilometryCallback;

        EventHandler<Transform> headTransformCallback = (object sender, Transform data) =>
        {
            LogUtilities.writeCSVLine(sessionEventLogWriter, new SessionEventLogEntry
            {
                Timestamp = LogUtilities.localTimestamp(),
                SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
                EventName = "HeadRotation",
                HeadRotationEulerX = data.rotation.eulerAngles.x,
                HeadRotationEulerY = data.rotation.eulerAngles.y,
                HeadRotationEulerZ = data.rotation.eulerAngles.z,
            });
        };
        headTransform.TransformChanged += headTransformCallback;


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

        pupilometry.DataChanged -= pupilometryCallback;
        headTransform.TransformChanged -= headTransformCallback;

        LogUtilities.writeCSVLine(sessionEventLogWriter, new SessionEventLogEntry
        {
            Timestamp = LogUtilities.localTimestamp(),
            SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
            EventName = "Trial completed",
        });

        sessionEventLogWriter.Close();
    }


}
