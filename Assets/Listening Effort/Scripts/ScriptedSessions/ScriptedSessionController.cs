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

        public string HeadRotationEulerX { get; set; }
        public string HeadRotationEulerY { get; set; }
        public string HeadRotationEulerZ { get; set; }

        public string PupilometrySystemTimestamp { get; set; }
        public string PupilometryDeviceTimestamp { get; set; }
        public string LeftIsBlinking { get; set; }
        public string RightIsBlinking { get; set; }
        public string LeftPupilDiameterValid { get; set; }
        public string LeftPupilDiameter { get; set; }
        public string RightPupilDiameterValid { get; set; }
        public string RightPupilDiameter { get; set; }
        public string LeftPositionGuideValid { get; set; }
        public string LeftPositionGuideX { get; set; }
        public string LeftPositionGuideY { get; set; }
        public string RightPositionGuideValid { get; set; }
        public string RightPositionGuideX { get; set; }
        public string RightPositionGuideY { get; set; }
        public string LeftGazeRayIsValid { get; set; }
        public string LeftGazeRayOriginX { get; set; }
        public string LeftGazeRayOriginY { get; set; }
        public string LeftGazeRayOriginZ { get; set; }
        public string LeftGazeRayDirectionX { get; set; }
        public string LeftGazeRayDirectionY { get; set; }
        public string LeftGazeRayDirectionZ { get; set; }
        public string RightGazeRayIsValid { get; set; }
        public string RightGazeRayOriginX { get; set; }
        public string RightGazeRayOriginY { get; set; }
        public string RightGazeRayOriginZ { get; set; }
        public string RightGazeRayDirectionX { get; set; }
        public string RightGazeRayDirectionY { get; set; }
        public string RightGazeRayDirectionZ { get; set; }
        public string ConvergenceDistanceIsValid { get; set; }
        public string ConvergenceDistance { get; set; }
        public string GazeRayIsValid { get; set; }
        public string GazeRayOriginX { get; set; }
        public string GazeRayOriginY { get; set; }
        public string GazeRayOriginZ { get; set; }
        public string GazeRayDirectionX { get; set; }
        public string GazeRayDirectionY { get; set; }
        public string GazeRayDirectionZ { get; set; }

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
        string sessionFolder = Path.Join(Path.Join(Application.persistentDataPath, "RecordedSessions"), sessionLabel);
        audioRecorder.saveDirectory = sessionFolder;
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
                PupilometrySystemTimestamp = data.SystemTimestamp.ToString(),
                PupilometryDeviceTimestamp = data.DeviceTimestamp.ToString(),
                LeftIsBlinking = data.Left.IsBlinking.ToString(),
                RightIsBlinking = data.Right.IsBlinking.ToString(),
                LeftPupilDiameterValid = data.Left.PupilDiameterValid.ToString(),
                LeftPupilDiameter = data.Left.PupilDiameter.ToString(),
                RightPupilDiameterValid = data.Right.PupilDiameterValid.ToString(),
                RightPupilDiameter = data.Right.PupilDiameter.ToString(),
                LeftPositionGuideValid = data.Left.PositionGuideValid.ToString(),
                LeftPositionGuideX = data.Left.PositionGuide.x.ToString(),
                LeftPositionGuideY = data.Left.PositionGuide.y.ToString(),
                RightPositionGuideValid = data.Right.PositionGuideValid.ToString(),
                RightPositionGuideX = data.Right.PositionGuide.x.ToString(),
                RightPositionGuideY = data.Right.PositionGuide.y.ToString(),
                LeftGazeRayIsValid = data.Left.GazeRay.IsValid.ToString(),
                LeftGazeRayOriginX = data.Left.GazeRay.Origin.x.ToString(),
                LeftGazeRayOriginY = data.Left.GazeRay.Origin.y.ToString(),
                LeftGazeRayOriginZ = data.Left.GazeRay.Origin.z.ToString(),
                LeftGazeRayDirectionX = data.Left.GazeRay.Direction.x.ToString(),
                LeftGazeRayDirectionY = data.Left.GazeRay.Direction.y.ToString(),
                LeftGazeRayDirectionZ = data.Left.GazeRay.Direction.z.ToString(),
                RightGazeRayIsValid = data.Right.GazeRay.IsValid.ToString(),
                RightGazeRayOriginX = data.Right.GazeRay.Origin.x.ToString(),
                RightGazeRayOriginY = data.Right.GazeRay.Origin.y.ToString(),
                RightGazeRayOriginZ = data.Right.GazeRay.Origin.z.ToString(),
                RightGazeRayDirectionX = data.Right.GazeRay.Direction.x.ToString(),
                RightGazeRayDirectionY = data.Right.GazeRay.Direction.y.ToString(),
                RightGazeRayDirectionZ = data.Right.GazeRay.Direction.z.ToString(),
                ConvergenceDistanceIsValid = data.ConvergenceDistanceIsValid.ToString(),
                ConvergenceDistance = data.ConvergenceDistance.ToString(),
                GazeRayIsValid = data.GazeRay.IsValid.ToString(),
                GazeRayOriginX = data.GazeRay.Origin.x.ToString(),
                GazeRayOriginY = data.GazeRay.Origin.y.ToString(),
                GazeRayOriginZ = data.GazeRay.Origin.z.ToString(),
                GazeRayDirectionX = data.GazeRay.Direction.x.ToString(),
                GazeRayDirectionY = data.GazeRay.Direction.y.ToString(),
                GazeRayDirectionZ = data.GazeRay.Direction.z.ToString(),
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
                HeadRotationEulerX = data.rotation.eulerAngles.x.ToString(),
                HeadRotationEulerY = data.rotation.eulerAngles.y.ToString(),
                HeadRotationEulerZ = data.rotation.eulerAngles.z.ToString(),
            });
        };
        headTransform.TransformChanged += headTransformCallback;


        for (int i = 0; i < session.Maskers.Count(); i++)
        {
            advanceStateTo(State.WaitingForUserToStartChallenge);
            string userResponseAudioFile = $"{sessionLabel}_{i:000}.wav";


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
            // start recording now because pico loses the first few seconds
            double expectedPlaybackDuration = videoManagers.Aggregate(0.0, (acc, vm) => Math.Max(acc, vm.player.length));
            int maximumRecordingDuration = session.MaximumRecordingDuration + (int)Math.Ceiling(expectedPlaybackDuration);
            Debug.Log($"Starting off recorder. Max duration (including playback): {maximumRecordingDuration}");
            audioRecorder.StartRecording(userResponseAudioFile, maximumRecordingDuration);

            while (numVideosPlaying > 0)
            {
                yield return null;
            }

            advanceStateTo(State.RecordingUserResponse);

            audioRecorder.MarkRecordingInPoint();
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
