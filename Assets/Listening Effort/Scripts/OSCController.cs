using Pico.Platform;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using UnityOSC;

public class OSCController : MonoBehaviour
{
    private OSCReceiver osc = new OSCReceiver();
    public static readonly int listenPort = 7000;
    public bool logReceivedMessages;
    // This is active when we're running an OSC session. It is inactive in a menu or during an automated session
    public bool inOSCSessionMode = false;
    //public string videoDirectory;

    public VideoPlayer[] videoPlayers;
    //private Transform[] videoPlayerPivotTransforms;
    //private Transform[] videoPlayerQuadTransforms;
    [SerializeField] private ColorCalibrationSphere colorCalibrationSphere;
    public AudioSource[] maskingAudioSources;
    public AudioSource[] speechAudioSources;

    public OSCSender oscSender;

    /// Container for the cameraObject that we can rotate manually
    public GameObject cameraRigObject;
    /// What the VR system rotates for the headset's point of view
    public GameObject cameraObject;

    public event EventHandler<(string ip, int port)> onClientConnected;
    public bool isClientConnected { get; private set; } = false;

    class MessageSpecification
    {
        public string address;
        public (System.Type type, string description)[] arguments = { };
        public bool onlyPermittedInOSCSession = false;
    }

    static private readonly (System.Type, string)[] videoMessageArguments = {
        (typeof(int), "Video player ID (0 for background)"),
        (typeof(string), "Absolute path to video file")

    };

    private readonly MessageSpecification videoPlayMessageSpecification = new MessageSpecification
    {
        address = "/video/play",
        arguments = videoMessageArguments,
        onlyPermittedInOSCSession = true,
    };

    private readonly MessageSpecification setIdleVideoMessageSpecification = new MessageSpecification
    {
        address = "/video/set_idle",
        arguments = new (System.Type, string)[]
        {
            (typeof(int), "Video player ID (1-3)"),
            (typeof(string), "Absolute path to idle video file"),
        },
        onlyPermittedInOSCSession = true,
    };

    private readonly MessageSpecification videoStopMessageSpecification = new MessageSpecification
    {
        address = "/video/stop",
        arguments = new (System.Type, string)[]
        {
            (typeof(int), "Video Player ID (1-3)"),
        },
    };

    private readonly MessageSpecification startIdleVideoMessageSpecification = new MessageSpecification
    {
        address = "/video/start_idle",
        arguments = new (System.Type, string)[]
        {
            (typeof(int), "Video Player ID (1-3)"),
        },
        onlyPermittedInOSCSession = true,
    };

    private readonly MessageSpecification videoPositionMessageSpecification = new MessageSpecification
    {
        address = "/video/position",
        arguments = new (System.Type, string)[]
        {
            (typeof(int), "Video player ID (1-3)"),
            (typeof(float), "Azimuth (degrees)"),
            (typeof(float), "Inclination (degrees)"),
            (typeof(float), "Twist (degrees)"),
            (typeof(float), "Rotation around X axis (degrees)"),
            (typeof(float), "Rotation around Y axis (degrees)"),
            (typeof(float), "Width (scale)"),
            (typeof(float), "Height (scale)"),
        },
    };

    private readonly MessageSpecification setClientAddressMessageSpecification = new MessageSpecification
    {
        address = "/set_client_address",
        arguments = new (System.Type, string)[] {
                (typeof(string), "Client IP"),
                (typeof(int), "Client port"),
        },
    };

    private readonly MessageSpecification resetOrientationMessageSpecification = new MessageSpecification
    {
        address = "/reset_orientation",
    };

    private readonly MessageSpecification setOrientationMessageSpecification = new MessageSpecification
    {
        address = "/set_orientation",
        arguments = new (System.Type, string)[]
        {
            (typeof(float), "Target Euler angle X"),
            (typeof(float), "Target Euler angle Y"),
            (typeof(float), "Target Euler angle Z"),
        }
    };

    private readonly MessageSpecification showSolidBrightnessMessageSpecification = new MessageSpecification
    {
        address = "/brightness_calibration_view",
        arguments = new (System.Type, string)[]
        {
			// NB max only sends ints, not bools
			(typeof(int), "Enable display of solid brightness for calibration (0=off, 1=on)"),
            (typeof(float), "Brightness intensity to show"),
        }
    };

    private readonly MessageSpecification sendVideoNamesMessageSpecification = new MessageSpecification
    {
        address = "/send_video_names",
        onlyPermittedInOSCSession = true,
    };

    private readonly MessageSpecification speechAudioLevelMessageSpecification = new MessageSpecification
    {
        address = "/audio/level/speech",
        arguments = new (System.Type, string)[]
        {
            (typeof(float), "Level of speech audio (0.0 - 1.0)"),
        },
        onlyPermittedInOSCSession = true,
    };

    private readonly MessageSpecification maskingAudioLevelMessageSpecification = new MessageSpecification
    {
        address = "/audio/level/masking",
        arguments = new (System.Type, string)[]
        {
            (typeof(float), "Level of masking audio (0.0 - 1.0)"),
        },
        onlyPermittedInOSCSession = true,
    };

    // This is used by OSCSender
    public int GetIDForVideoPlayer(VideoPlayer player)
    {
        //Debug.Assert(videoPlayers.Length == videoMessageSpecifications.Length);
        for (int i = 0; i < videoPlayers.Length; i++)
        {
            if (videoPlayers[i] == player)
            {
                return i;
            }
        }
        Debug.LogError($"VideoPlayer {player} is not registered with this OSCController.");
        return -404;
    }

    void Start()
    {
        Debug.Assert(colorCalibrationSphere != null);

        oscSender = GetComponent<OSCSender>();

        // player 0 is skybox so has no cached position
        for (int i = 1; i < videoPlayers.Length; i++)
        {
            string cachedPosition = PlayerPrefs.GetString($"videoPosition[{i}]", "");
            if (cachedPosition != "")
            {
                OSCMessage positionMessage = new OSCMessage(videoPositionMessageSpecification.address);
                positionMessage.Append(i);
                cachedPosition.Split(',').Skip(1).ToList().ForEach(x => positionMessage.Append(float.Parse(x)));
                Debug.Assert(isMatch(positionMessage, videoPositionMessageSpecification));
                Debug.Log($"Restoring position for video player {i}");
                ProcessMessage(positionMessage);
            }
        }
    }

    void OnEnable()
    {
        Debug.Assert(videoPlayers.Length == 4);
        Debug.Log($"Opening OSC server on port {listenPort}.");
        osc.Open(listenPort);
        // Print to log after 1 second

    }

    void OnDisable()
    {
        osc.Close();
    }

    //void OnDisable()
    //{
    //    Debug.Log($"Closing OSC server.");
    //    osc.Close();
    //}

    private void playVideo(VideoPlayer videoPlayer, string absolutePath)
    {
        videoPlayer.Stop();
        videoPlayer.url = absolutePath;
        videoPlayer.Play();
    }


    // If address matches but not arguments then will return false and print a warning
    private bool isMatch(OSCMessage message, MessageSpecification specification)
    {

        if (message.Address != specification.address)
        {
            return false;
        }

        bool isError = message.Data.Count != specification.arguments.Length;
        if (!isError)
        {
            for (int i = 0; i < message.Data.Count; i++)
            {
                if (message.Data[i].GetType() != specification.arguments[i].type)
                {
                    isError = true;
                }
            }
        }
        if (isError)
        {
            string correctFormat = "";
            foreach ((System.Type type, string description) in specification.arguments)
            {
                correctFormat += $"<{type}> ({description}), ";
            }
            string receivedFormat = "";
            foreach (object o in message.Data)
            {
                receivedFormat += $"<{o.GetType()}> ({o.ToString()}), ";
            }
            Debug.LogWarning($"Received OSC message with address {message.Address} of incorrect format.\nCorrect format: {correctFormat}\nReceived format: {receivedFormat}");
            return false;
        }

        if (specification.onlyPermittedInOSCSession && !inOSCSessionMode)
        {
            Debug.LogWarning($"OSC message '{message.Address}' ignored as it is only permitted in an OSC Session.");
            return false;
        }
        return true;
    }

    private void ProcessMessage(OSCMessage message)
    {
        if (isMatch(message, videoPlayMessageSpecification))
        {
            Debug.Assert(message.Data.Count >= 2);
            int i = (int)message.Data[0];
            string videoName = (string)message.Data[1];
            if (i < 0 || videoPlayers.Length < i)
            {
                Debug.LogError($"{message.Address} message received for video player ID {i}. Valid video player IDs are at least 0 and at most {videoPlayers.Length - 1}");
            }
            else if (!oscSender.VideoCatalogue.Contains(videoName))
            {
                Debug.LogError($"{message.Address} message received with unrecognised video name: {name}");
            }
            else
            {
                if (i == 0)
                {
                    videoPlayers[i].GetComponent<VideoSkyboxManager>().PlayVideo(videoName);
                }
                else
                {
                    videoPlayers[i].GetComponent<VideoManager>().PlayVideo(videoName);
                }
                Debug.Log($"{message.Address} set video player {i} to {(string)message.Data[1]}");
            }
        }

        else if (isMatch(message, setIdleVideoMessageSpecification))
        {
            Debug.Assert(message.Data.Count >= 2);
            int i = (int)message.Data[0];
            if (i <= 0 || videoPlayers.Length < i)
            {
                Debug.LogError($"{message.Address} message received for video player ID {i}. Valid video player  IDs (that can receive an idle video message) are at least 1 and at most {videoPlayers.Length - 1}");
            }
            else if (!oscSender.VideoCatalogue.Contains((string)message.Data[1]))
            {
                Debug.LogError($"{message.Address} message received with unrecognised video name: {(string)message.Data[1]}");
            }
            else
            {
                var videoManager = videoPlayers[i].GetComponent<VideoManager>();
                var catalogue = oscSender.VideoCatalogue;
                string idleVideoName = (string)message.Data[1];
                if (catalogue.Contains(idleVideoName))
                {
                    videoManager.idleVideoName = idleVideoName;
                    videoManager.StartIdleVideo();
                }
                else
                {
                    Debug.LogError($"{message.Address} message received with unrecognised video name: {idleVideoName}");
                }
            }
        }

        else if (isMatch(message, videoStopMessageSpecification))
        {
            Debug.Assert(message.Data.Count >= 1);
            int i = (int)message.Data[0];
            if (i < 0 || videoPlayers.Length < i)
            {
                Debug.LogError($"{message.Address} message received for video player ID {i}. Valid video player  IDs (that can receive an idle video message) are at least 1 and at most {videoPlayers.Length - 1}");
            }
            else
            {
                var videoManager = videoPlayers[i].GetComponent<VideoManager>();
                videoManager.StopVideo();
            }
        }

        else if (isMatch(message, startIdleVideoMessageSpecification))
        {
            Debug.Assert(message.Data.Count >= 1);
            int i = (int)message.Data[0];
            if (i <= 0 || videoPlayers.Length < i)
            {
                Debug.LogError($"{message.Address} message received for video player ID {i}. Valid video player  IDs (that can receive an idle video message) are at least 1 and at most {videoPlayers.Length - 1}");
            }
            else
            {
                var videoManager = videoPlayers[i].GetComponent<VideoManager>();
                videoManager.StartIdleVideo();
            }
        }

        else if (isMatch(message, setClientAddressMessageSpecification))
        {
            string ip = (string)message.Data[0];
            int port = (int)message.Data[1];
            if (port < 0 || port > 65535)
            {
                Debug.LogWarning($"Invalid port number received in {message.Address} message: {port}");
            }
            else if (!isClientConnected || oscSender.ClientIP != ip || oscSender.Port != port)
            {
                oscSender.ClientIP = ip;
                oscSender.Port = port;
                isClientConnected = true;
                onClientConnected?.Invoke(this, (ip, port));
            }
            // player 0 is the skybox and has no position
            oscSender.SendVideoPositions(videoPlayers.Skip(1).Select(x => x.GetComponent<VideoManager>()).ToArray());
        }

        else if (isMatch(message, videoPositionMessageSpecification))
        {
            int i = (int)message.Data[0];
            // player 0 is the skybox and has no position
            if (i < 1 || i >= videoPlayers.Length)
            {
                Debug.LogWarning($"Cannot set video position for video player {i}");
            }
            else
            {
                videoPlayers[i].GetComponent<VideoManager>().SetPosition(
                    // pivot inc, azi, twist
                    (float)message.Data[1], (float)message.Data[2], (float)message.Data[3],
                    // quad rot x and y
                    (float)message.Data[4], (float)message.Data[5],
                    // quad scale x and y
                    (float)message.Data[6], (float)message.Data[7]
                    );
                Debug.Log($"Set position of video player {i}");
                PlayerPrefs.SetString($"videoPosition[{i}]", string.Join(",", message.Data.Select(x => x.ToString())));
                PlayerPrefs.Save();
            }
        }

        else if (isMatch(message, resetOrientationMessageSpecification))
        {
            cameraRigObject.transform.rotation = Quaternion.identity;
        }

        else if (isMatch(message, setOrientationMessageSpecification))
        {
            Vector3 targetEulerAngles = new Vector3((float)message.Data[0], (float)message.Data[1], (float)message.Data[2]);
            Quaternion target = Quaternion.Euler(targetEulerAngles);
            cameraRigObject.transform.rotation = target * Quaternion.Inverse(cameraObject.transform.localRotation);
            //cameraRigObject.transform.rotation *= Quaternion.Inverse(cameraObject.transform.localRotation);
        }

        else if (isMatch(message, showSolidBrightnessMessageSpecification))
        {
            if (colorCalibrationSphere == null)
            {
                Debug.LogError("Color Calibration Sphere reference was not set.");
            }
            else
            {
                colorCalibrationSphere.gameObject.SetActive((int)message.Data[0] != 0);
                colorCalibrationSphere.SetBrightness((float)message.Data[1]);
            }
        }

        else if (isMatch(message, sendVideoNamesMessageSpecification))
        {
            oscSender.SendVideoNames();
        }

        else if (isMatch(message, maskingAudioLevelMessageSpecification))
        {
            foreach (AudioSource source in maskingAudioSources)
            {
                source.volume = Math.Clamp((float)message.Data[0], 0.0f, 1.0f);
            }
        }

        else if (isMatch(message, speechAudioLevelMessageSpecification))
        {
            foreach (AudioSource source in speechAudioSources)
            {
                source.volume = Math.Clamp((float)message.Data[0], 0.00f, 1.0f);
            }
        }

        else
        {
            Debug.Log($"OSC Message {message.ToString()} not processed. Either address unrecognised or not allowed in the current mode.");
        }
    }


    // Update is called once per frame
    void Update()
    {
        while (osc.hasWaitingMessages())
        {
            ProcessMessage(osc.getNextMessage());
        }
    }
}
