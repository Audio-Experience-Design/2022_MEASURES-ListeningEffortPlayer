using System;
using UnityEngine;
using UnityEngine.Video;
using UnityOSC;

public class OSCController : MonoBehaviour
{
	private OSCReceiver osc = new OSCReceiver();
	public int listenPort = 7100;
	public bool logReceivedMessages;
	//public string videoDirectory;

	public VideoPlayer[] videoPlayers;
	private Transform[] videoPlayerPivotTransforms;
	private Transform[] videoPlayerQuadTransforms;
	[SerializeField] private ColorCalibrationSphere colorCalibrationSphere;

	public OSCSender oscSender;

	/// Container for the cameraObject that we can rotate manually
	public GameObject cameraRigObject;
	/// What the VR system rotates for the headset's point of view
	public GameObject cameraObject;

	class MessageSpecification
	{
		public string address;
		public (System.Type type, string description)[] arguments = { };
	}

	static private readonly (System.Type, string)[] videoMessageArguments = {
		(typeof(int), "Video player ID (0 for background)"),
		(typeof(string), "Absolute path to video file")

	};

	private readonly MessageSpecification videoPlayMessageSpecification = new MessageSpecification
	{
		address = "/video/play",
		arguments = videoMessageArguments
	};

	private readonly MessageSpecification setIdleVideoMessageSpecification = new MessageSpecification
	{
		address = "/video/set_idle",
		arguments = new (System.Type, string)[]
		{
			(typeof(int), "Video player ID (1-3)"),
			(typeof(string), "Absolute path to idle video file"),
		}
	};

    private readonly MessageSpecification startIdleVideoMessageSpecification = new MessageSpecification
    {
        address = "/video/start_idle",
        arguments = new (System.Type, string)[]
        {
            (typeof(int), "Video Player ID (1-3)"),
        }
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
		}
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
		address = "/reset_orientation"
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

	void Awake()
	{
		Debug.Assert(colorCalibrationSphere != null);

		oscSender = GetComponent<OSCSender>();

		videoPlayerPivotTransforms = new Transform[videoPlayers.Length];
		videoPlayerQuadTransforms = new Transform[videoPlayers.Length];

		for (int i = 0; i < videoPlayers.Length; i++)
		{
			if (videoPlayers[i].GetComponentInChildren<MeshFilter>() == null)
			{
				videoPlayerPivotTransforms[i] = null;
				videoPlayerQuadTransforms[i] = null;
			}
			else
			{
				videoPlayerPivotTransforms[i] = videoPlayers[i].GetComponent<Transform>();
				videoPlayerQuadTransforms[i] = videoPlayers[i].GetComponentInChildren<MeshFilter>().GetComponent<Transform>();
				Debug.Assert(videoPlayerPivotTransforms[i] != null);
				Debug.Assert(videoPlayerQuadTransforms[i] != null);
				Debug.Assert(videoPlayerPivotTransforms[i] != videoPlayerQuadTransforms[i]);
			}
		}
	}

	void Start()
	{
		Debug.Assert(videoPlayers.Length == 4);
		Debug.Log($"Opening OSC server on port {listenPort}.");
		osc.Open(listenPort);
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
		else
		{
			return true;
		}
	}

	private void ProcessMessage(OSCMessage message)
	{
		if (isMatch(message, videoPlayMessageSpecification))
		{
			Debug.Assert(message.Data.Count >= 2);
			int i = (int)message.Data[0];
			if (i < 0 || videoPlayers.Length < i)
			{
				Debug.LogError($"{message.Address} message received for video player ID {i}. Valid video player IDs are at least 0 and at most {videoPlayers.Length - 1}");
			}
			else
			{
				videoPlayers[i].Stop();
				videoPlayers[i].url = (string)message.Data[1];
				videoPlayers[i].isLooping = false;
				videoPlayers[i].Prepare();
				//videoPlayers[i].Play();
				// videoPlayers[i] will play automatically due to VideoController
				Debug.Log($"{message.Address} set video player {i} to {(string)message.Data[1]}");
			}
		}

		else if (isMatch(message, setIdleVideoMessageSpecification))
		{
			Debug.Assert(message.Data.Count >= 2);
			int i = (int)message.Data[0];
			if (i <= 0 || videoPlayers.Length < i)
			{
				Debug.LogError($"{message.Address} message received for video player ID {i}. Valid video player  IDs (that can receive an idle video message) are at least 1 and at most { videoPlayers.Length - 1}");
			}
			else
			{
				var videoManager = videoPlayers[i].GetComponent<VideoManager>();
				videoManager.IdleVideoPath = (string)message.Data[1];
				if (!videoPlayers[i].isPlaying)
				{
					videoManager.StartIdleVideo();
				}
			}
		}

        else if (isMatch(message, startIdleVideoMessageSpecification))
        {
            Debug.Assert(message.Data.Count >= 1);
            int i = (int)message.Data[0];
            if (i <= 0 || videoPlayers.Length < i)
            {
                Debug.LogError($"{message.Address} message received for video player ID {i}. Valid video player  IDs (that can receive an idle video message) are at least 1 and at most { videoPlayers.Length - 1}");
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
			else
			{
				GetComponent<OSCSender>().ClientIP = ip;
				GetComponent<OSCSender>().Port = port;
			}
			oscSender.SendVideoPositions(videoPlayerPivotTransforms, videoPlayerQuadTransforms);
		}

		else if (isMatch(message, videoPositionMessageSpecification))
		{
			int i = (int)message.Data[0];
			if (i < 0 || i >= videoPlayerPivotTransforms.Length || videoPlayerPivotTransforms[i] == null || videoPlayerQuadTransforms[i] == null)
			{
				Debug.LogWarning($"Cannot set video position for video player {i}");
			}
			else
			{
				videoPlayerPivotTransforms[i].localEulerAngles = new Vector3((float)message.Data[1], (float)message.Data[2], (float)message.Data[3]);
				videoPlayerQuadTransforms[i].localEulerAngles = new Vector3((float)message.Data[4], (float)message.Data[5], videoPlayerQuadTransforms[i].localEulerAngles.z);
				videoPlayerQuadTransforms[i].localScale = new Vector3((float)message.Data[6], (float)message.Data[7], videoPlayerQuadTransforms[i].localScale.z);
				Debug.Log($"Set position of video player {i}");
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

		else
		{
			Debug.Log($"OSC Message with unrecognised address received: {message.ToString()}");
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
