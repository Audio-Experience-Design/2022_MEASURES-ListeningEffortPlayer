using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Video;
using Whisper;
using Debug = UnityEngine.Debug;
using PupilometryData = Tobii.XR.TobiiXR_AdvancedEyeTrackingData;


public class ScriptedSessionController : MonoBehaviour
{
    public Session session { get; private set; }

    public VideoCatalogue videoCatalogue;
    public AudioRecorder audioRecorder;
    public Pupilometry pupilometry;
    public TransformWatcher headTransform;
    public ColorCalibrationSphere brightnessCalibrationSphere;

    public VideoPlayer skyboxVideoPlayer;
    public VideoManager[] videoManagers;
    public GameObject[] babblePrefabs;
    public WhisperManager whisperManager;

    public event EventHandler<State> stateChanged;
    /// current number (0 indexed), current label (1 indexed), total number
    public event EventHandler<(int current, string currentLabel, int total)> challengeNumberChanged;

    public struct SessionEventLogEntry
    {
        // Set automatically by Log function
        public string Timestamp { get; set; }
        // Set automatically by Log function
        public string SessionTime { get; set; }
        // Set automatically by Log function
        public string Configuration { get; set; }
        public string EventName { get; set; }
        public string ChallengeNumber { get; set; }
        public string LeftVideo { get; set; }
        public string MiddleVideo { get; set; }
        public string RightVideo { get; set; }
        public string UserResponseAudioFile { get; set; }
        public string UserResponseTranscription {get; set;}
        public string UserResponseTranscriptionProcessingDurationInSeconds {get; set;}

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
        WaitingForUserToStartBrightnessCalibration,
        PerformingBrightnessCalibration,
        WaitingForUserToStartChallenges,
        UserReadyToStartChallenges,
        DelayingBeforePlayingVideo,
        PlayingVideo,
        DelayingAfterPlayingVideos,
        RecordingUserResponse,
        AudioRecordingComplete,
        Completed,
    }
    private State _state = State.Inactive;
    public State state
    {
        get => _state; private set
        {
            Debug.Log($"Changing state from {_state} to {value}");
            Debug.Assert(AllowedTransitions[_state].Contains(value));
            _state = value;
            stateChanged?.Invoke(this, _state);
        }
    }
    private (string path, AudioClip clip) lastAudioRecording = (null, null);

    static readonly Dictionary<State, State[]> AllowedTransitions = new Dictionary<State, State[]>
        {
            {State.Inactive, new State[]{ State.LoadingSession } },
            {State.LoadingSession, new State[]{ State.WaitingForUserToStartBrightnessCalibration, State.WaitingForUserToStartChallenges, State.Completed } },
            {State.WaitingForUserToStartBrightnessCalibration, new State[]{ State.PerformingBrightnessCalibration } },
            {State.PerformingBrightnessCalibration, new State[]{ State.WaitingForUserToStartChallenges } },
            {State.WaitingForUserToStartChallenges, new State[]{ State.UserReadyToStartChallenges } },
            {State.UserReadyToStartChallenges, new State[]{ State.DelayingBeforePlayingVideo } },
            {State.DelayingBeforePlayingVideo, new State[]{ State.PlayingVideo } },
            {State.PlayingVideo, new State[]{ State.DelayingAfterPlayingVideos } },
            {State.DelayingAfterPlayingVideos, new State[]{ State.RecordingUserResponse } },
            {State.RecordingUserResponse, new State[]{ State.AudioRecordingComplete } },
            {State.AudioRecordingComplete, new State[]{ State.DelayingBeforePlayingVideo, State.Completed } },
            {State.Completed, new State[]{ } },
        };

    private int numVideosPlaying = 0;

    // Details for logwriter
    private StreamWriter sessionEventLogWriter;
    private DateTime sessionStartTimeUTC;
    private string sessionLabel;
    private string challengeLabel;
    private string challengeLabelPadded
    {
        get
        {
            if (int.TryParse(challengeLabel, out int challengeNumber))
            {
                return $"{challengeNumber:000}";
            }
            else
            {
                return challengeLabel;
            }
        }
    }

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
        audioRecorder.recordingFinished += (object sender, (string path, AudioClip clip) args) =>
        {
            lastAudioRecording = args;
            Debug.Assert(state == State.RecordingUserResponse);
            state = State.AudioRecordingComplete;
        };
    }

    // yamlPath should be an absolute path including extension
    public void StartSession(string yamlPath)
    {
        session = Session.LoadFromYamlPath(yamlPath, videoCatalogue);
        Debug.Assert(session.VideoScreens.Count() == 3);
        Debug.Assert(videoManagers.Count() == 3);
        Debug.Log($"Loaded {yamlPath}");

        StartCoroutine(SessionCoroutine());
    }


    public void onUserReadyToContinue()
    {
        if (state == State.WaitingForUserToStartBrightnessCalibration)
        {
            state = State.PerformingBrightnessCalibration;
        }
        else if (state == State.WaitingForUserToStartChallenges)
        {
            state = State.UserReadyToStartChallenges;
        }
        else
        {
            Debug.Assert(false);
        }
    }

    public void onUserReadyToStopRecording()
    {
        Debug.Assert(state == State.RecordingUserResponse);
        Debug.Assert(audioRecorder.isRecording);
        audioRecorder.StopRecording();
    }

    private IEnumerator SessionCoroutine()
    {
        Debug.Log($"Starting automated trial session: {session.Name}");
        state = State.LoadingSession;

        sessionStartTimeUTC = DateTime.UtcNow;
        string localTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string subjectLabel = PlayerPrefs.GetString("subjectLabel", "");
        sessionLabel = $"{localTimestamp}_{session.Name}{(subjectLabel != "" ? "_" : "")}{subjectLabel}";
        string sessionFolder = Path.Join(Path.Join(Application.persistentDataPath, "RecordedSessions"), sessionLabel);
        audioRecorder.saveDirectory = sessionFolder;
        Directory.CreateDirectory(sessionFolder);
        File.WriteAllText(Path.Join(sessionFolder, session.Name != "" ? session.Name + ".yaml" : "session.yaml"), session.yaml);

        // Speaker Amplitude
        videoManagers.ToList().ForEach(vm => vm.audioSource.volume = session.SpeakerAmplitude);

        // MaskingVideo
        videoCatalogue.SetPlayerSource(skyboxVideoPlayer, session.MaskingVideo);
        skyboxVideoPlayer.Play();

        // Setup Maskers
        {
            if (session.Maskers.Count() > babblePrefabs.Count())
            {
                throw new System.Exception($"There are {session.Maskers.Count()} maskers defined in YAML but only {babblePrefabs.Count()} babble sources available.");
            }
            for (int i = 0; i < session.Maskers.Count(); i++)
            {
                Debug.Assert(babblePrefabs[i].GetComponentsInChildren<AudioSource>().Count() == 1);
                babblePrefabs[i].GetComponentInChildren<AudioSource>().volume = session.Maskers[i].Amplitude;
                babblePrefabs[i].transform.localRotation = Quaternion.Euler(0, session.Maskers[i].Rotation, 0);
                babblePrefabs[i].GetComponentInChildren<AudioSource>().Play();
                if (!session.PlayMaskersContinuously)
                {
                    babblePrefabs[i].GetComponentInChildren<AudioSource>().Pause();
                }
                Debug.Log($"Set masker {i} to {session.Maskers[i].Amplitude} amplitude and {session.Maskers[i].Rotation} rotation.");
            }
            for (int i = session.Maskers.Count(); i < babblePrefabs.Count(); i++)
            {
                babblePrefabs[i].SetActive(false);
                Debug.Log($"Deactivated masker {i} as not set in session YAML.");
            }

            // Setup screens
            for (int i = 0; i < 3; i++)
            {
                videoManagers[i].idleVideoName = session.VideoScreens[i].IdleVideo;
                var s = session.VideoScreens[i];
                videoManagers[i].SetPosition(s.Inclination, s.Azimuth, s.Twist, s.RotationOnXAxis, s.RotationOnYAxis, s.ScaleWidth, s.ScaleHeight);
            }
        }

        // Setup logging
        {
            sessionEventLogWriter = new StreamWriter(Path.Join(sessionFolder, $"{sessionLabel}_events.csv"), true, Encoding.UTF8);
            Log(new SessionEventLogEntry { EventName = "Trial started" });
        }

        // Perform Brightness Calibration
        if (session.BrightnessCalibrationDurationFromBlackToWhite > 0.0001 && session.BrightnessCalibrationDurationToHoldOnWhite > 0.0001)
        {
            state = State.WaitingForUserToStartBrightnessCalibration;
            yield return new WaitUntil(() => state != State.WaitingForUserToStartBrightnessCalibration);
            Debug.Assert(state == State.PerformingBrightnessCalibration);
            challengeLabel = "brightness_calibration";

            using var pupilometryLogger = new PupilometryLogger(sessionFolder, sessionLabel, challengeLabel, session.Name, sessionStartTimeUTC, pupilometry, headTransform);
            Log(new SessionEventLogEntry
            {
                EventName = "Brightness calibration started",
            });


            brightnessCalibrationSphere.gameObject.SetActive(true);
            brightnessCalibrationSphere.brightness = 0.0f;
            float startTime = Time.time;
            while (brightnessCalibrationSphere.brightness < 1.0f)
            {
                float t = Time.time - startTime;
                brightnessCalibrationSphere.brightness = Math.Min(1.0f, t / session.BrightnessCalibrationDurationFromBlackToWhite);
                yield return null;
            }
            Log(new SessionEventLogEntry
            {
                EventName = "Brightness calibration reached full brightness",
            });

            yield return new WaitForSecondsRealtime(session.BrightnessCalibrationDurationToHoldOnWhite);

            brightnessCalibrationSphere.gameObject.SetActive(false);
            Log(new SessionEventLogEntry
            {
                EventName = "Brightness calibration finished",
            });
        }

        // Wait for user to start first challenge
        {
            state = State.WaitingForUserToStartChallenges;
            yield return new WaitUntil(() => state != State.WaitingForUserToStartChallenges);
        }

        // Start challenges
        {
            Debug.Assert(state == State.UserReadyToStartChallenges);
            for (int i = 0; i < 3; i++)
            {
                videoManagers[i].StartIdleVideo();
            }
        }

        // Cycle through challenges
        for (int i = 0; i < session.Challenges.Count(); i++)
        {
            state = State.DelayingBeforePlayingVideo;
            Log(new SessionEventLogEntry
            {
                EventName = "Delaying before playing videos",
            });

            // ## Prepare challenge

            if (!session.PlayMaskersContinuously)
            {
                foreach (GameObject babblePrefab in babblePrefabs)
                {
                    babblePrefab.GetComponentInChildren<AudioSource>().UnPause();
                }
            }

            challengeLabel = (i + 1).ToString();
            challengeNumberChanged?.Invoke(this, (current: i, currentLabel: challengeLabel, total: session.Challenges.Count()));
            string userResponseAudioFile = $"{sessionLabel}_response_{challengeLabelPadded:000}.wav";

            // Record a separate CSV for pupilometry and head rotation for each challenge
            // This will stop logging at the end of scope of this loop iteration
            using var pupilometryLogger = new PupilometryLogger(sessionFolder, sessionLabel, challengeLabel, session.Name, sessionStartTimeUTC, pupilometry, headTransform);

            yield return new WaitForSeconds(session.DelayBeforePlayingVideos);
            state = State.PlayingVideo;
            Debug.Assert(numVideosPlaying == 0);

            // ## Play Videos
            {
                Log(new SessionEventLogEntry
                {
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
                int maximumRecordingDuration = (int)Math.Ceiling(session.RecordingDuration + expectedPlaybackDuration + session.DelayAfterPlayingVideos);
                Debug.Log($"Starting off recorder. Max duration (including playback): {maximumRecordingDuration}");
                audioRecorder.StartRecording(userResponseAudioFile, maximumRecordingDuration);
            }

            // ## Wait for videos to finish playing
            {
                while (numVideosPlaying > 0)
                {
                    yield return null;
                }
                state = State.DelayingAfterPlayingVideos;
                Log(new SessionEventLogEntry
                {
                    EventName = "Delaying after playing videos",
                });
                yield return new WaitForSeconds(session.DelayAfterPlayingVideos);
                if (!session.PlayMaskersContinuously)
                {
                    foreach (GameObject babblePrefab in babblePrefabs)
                    {
                        babblePrefab.GetComponentInChildren<AudioSource>().Pause();
                    }
                }
            }

            // ## Record User Audio
            {
                state = State.RecordingUserResponse;
                audioRecorder.MarkRecordingInPoint();
                Log(new SessionEventLogEntry
                {
                    EventName = "Recording response",
                    ChallengeNumber = challengeLabel,
                    LeftVideo = session.Challenges[i][0],
                    MiddleVideo = session.Challenges[i][1],
                    RightVideo = session.Challenges[i][2],
                });

                yield return new WaitUntil(() => state != State.RecordingUserResponse);

                Debug.Assert(state == State.AudioRecordingComplete);
            }

            string transcription;
            double transcriptionProcessingDuration;
            // ## Transcribe audio recording
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Debug.Assert(Path.GetFileName(lastAudioRecording.path) == userResponseAudioFile);
                var task = whisperManager.GetTextAsync(lastAudioRecording.clip);
                lastAudioRecording = (null, null);
                yield return new WaitUntil(() => task.IsCompleted);
                stopwatch.Stop();
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError($"Failed to transcribe audio: {task.Exception}");
                    transcription = "[ERROR]";
                }
                else
                {
                    transcription = task.Result.Result;
                }
                transcriptionProcessingDuration = stopwatch.ElapsedMilliseconds * 0.001;
                Debug.Log($"Transcription: {transcription}\nProcessing duration: {transcriptionProcessingDuration:F3} seconds");
            }

            Log(new SessionEventLogEntry
            {
                EventName = "Response received",
                ChallengeNumber = challengeLabel,
                LeftVideo = session.Challenges[i][0],
                MiddleVideo = session.Challenges[i][1],
                RightVideo = session.Challenges[i][2],
                UserResponseAudioFile = userResponseAudioFile,
                UserResponseTranscription = transcription,
                UserResponseTranscriptionProcessingDurationInSeconds = transcriptionProcessingDuration.ToString("F3"),
            });
        }

        state = State.Completed;


        Log(new SessionEventLogEntry
        {
            EventName = "Trial completed",
        });

        sessionEventLogWriter.Close();
    }


    private void Log(SessionEventLogEntry entry)
    {
        Debug.Assert(entry.Timestamp == null && entry.SessionTime == null && entry.Configuration == null);
        entry.Timestamp = LogUtilities.localTimestamp();
        entry.SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3");
        entry.EventName = "Trial completed";
        LogUtilities.writeCSVLine(sessionEventLogWriter, entry);
    }


}
