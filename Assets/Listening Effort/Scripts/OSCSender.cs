using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using UnityOSC;
using IPAddress = System.Net.IPAddress;

using PupilometryData = Tobii.XR.TobiiXR_AdvancedEyeTrackingData;

public class OSCSender : MonoBehaviour
{
    public TransformWatcher UserHeadTransform;
    public Pupilometry pupilometry;
    public bool LogSentOscMessages;
    public string ClientIP = "127.0.0.1";
    public int Port = 6789;
    public VideoCatalogue VideoCatalogue;

    // ClientIP that was used to set up the OSC Client, cached so we can detect change
    private string currentClientIP;

    private OSCClient oscClient;

    private OSCController oscController;
    private List<VideoPlayer> videoPlayersWithCallbacksRegistered = new List<VideoPlayer>();

    private int numSendErrors = 0;

    void Awake()
    {
        oscController = GetComponent<OSCController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        oscClient = new OSCClient(IPAddress.Parse(ClientIP), Port);
        currentClientIP = ClientIP;

    }

    void OnEnable()
    {
        pupilometry.DataChanged += OnPupilometryDataChanged;
        UserHeadTransform.TransformChanged += OnUserHeadTransformChanged;

        Debug.Assert(videoPlayersWithCallbacksRegistered.Count == 0);
        OSCController controller = GetComponent<OSCController>();
        for (int i = 0; i < controller.videoPlayers.Length; i++)
        {
            controller.videoPlayers[i].prepareCompleted += OnVideoPlayerPrepared;

            controller.videoPlayers[i].sendFrameReadyEvents = true;
            controller.videoPlayers[i].frameReady += OnVideoPlayerFrameReady;

            videoPlayersWithCallbacksRegistered.Add(controller.videoPlayers[i]);

        }


    }

    void OnDisable()
    {
        pupilometry.DataChanged -= OnPupilometryDataChanged;
        UserHeadTransform.TransformChanged -= OnUserHeadTransformChanged;

        foreach (VideoPlayer player in videoPlayersWithCallbacksRegistered)
        {
            player.prepareCompleted -= OnVideoPlayerPrepared;
            player.frameReady -= OnVideoPlayerFrameReady;
        }
        videoPlayersWithCallbacksRegistered.Clear();
    }

    private void OnVideoPlayerPrepared(VideoPlayer player)
    {
        bool isIdle = player.GetComponent<VideoManager>()?.IsIdleVideoPlaying == true;
        if (!isIdle)
        {
            int id = oscController.GetIDForVideoPlayer(player);
            Send($"/video/prepared", new ArrayList { id, player.url });
        }
    }

    private void OnVideoPlayerFrameReady(VideoPlayer player, long frameIndex)
    {
        bool isIdle = player.GetComponent<VideoManager>()?.IsIdleVideoPlaying == true;
        if (!isIdle && frameIndex == 0)
        {
            int id = oscController.GetIDForVideoPlayer(player);
            Send($"/video/first_frame", new ArrayList { id, player.url });
        }
    }


    private void UpdateClientAddress()
    {
        IPAddress ipAddress;
        if (IPAddress.TryParse(ClientIP, out ipAddress) && 0 <= Port && Port <= 65535)
        {
            if (oscClient != null)
            {
                oscClient.Close();
            }
            oscClient = new OSCClient(ipAddress, Port);
            // Formatting might have changed
            ClientIP = oscClient.ClientIPAddress.ToString();
            Debug.Log($"OSC Client address set to {ClientIP}:{Port}.");
            SendVideoNames();
            SendPupilometryHeaders();
        }
        else
        {
            Debug.LogWarning($"Unable to set OSC client address to invalid IP/port: {ClientIP}:{Port}. OSC is still being sent to {oscClient.ClientIPAddress}:{oscClient.Port}.");
        }
        currentClientIP = ClientIP;
    }


    private void OnUserHeadTransformChanged(object sender, Transform UserHeadTransform)
    {
        Send("/head_rotation", new ArrayList{
                UserHeadTransform.rotation.eulerAngles.x,
                UserHeadTransform.rotation.eulerAngles.y,
                UserHeadTransform.rotation.eulerAngles.z,
            });
    }

    private void OnPupilometryDataChanged(object sender, PupilometryData data)
    {
        Send("/pupilometry", new ArrayList
        {
            (Int64) data.SystemTimestamp,
            (Int64) data.DeviceTimestamp,
            Convert.ToInt32(data.Left.IsBlinking),
            Convert.ToInt32(data.Right.IsBlinking),
            Convert.ToInt32(data.Left.PupilDiameterValid),
            data.Left.PupilDiameter,
            Convert.ToInt32(data.Right.PupilDiameterValid),
            data.Right.PupilDiameter,
            Convert.ToInt32(data.Left.PositionGuideValid),
            data.Left.PositionGuide.x,
            data.Left.PositionGuide.y,
            Convert.ToInt32(data.Right.PositionGuideValid),
            data.Right.PositionGuide.x,
            data.Right.PositionGuide.y,
            Convert.ToInt32(data.Left.GazeRay.IsValid),
            data.Left.GazeRay.Origin.x,
            data.Left.GazeRay.Origin.y,
            data.Left.GazeRay.Origin.z,
            data.Left.GazeRay.Direction.x,
            data.Left.GazeRay.Direction.y,
            data.Left.GazeRay.Direction.z,
            Convert.ToInt32(data.Right.GazeRay.IsValid),
            data.Right.GazeRay.Origin.x,
            data.Right.GazeRay.Origin.y,
            data.Right.GazeRay.Origin.z,
            data.Left.GazeRay.Direction.x,
            data.Left.GazeRay.Direction.y,
            data.Left.GazeRay.Direction.z,
            Convert.ToInt32(data.ConvergenceDistanceIsValid),
            data.ConvergenceDistance,
            Convert.ToInt32(data.GazeRay.IsValid),
            data.GazeRay.Origin.x,
            data.GazeRay.Origin.y,
            data.GazeRay.Origin.z,
            data.GazeRay.Direction.x,
            data.GazeRay.Direction.y,
            data.GazeRay.Direction.z,
        }
        );
    }

    private void SendPupilometryHeaders()
    {
        Send("/pupilometryLabels", new ArrayList
        {
            "SystemTimestamp",
            "DeviceTimestamp",
            "Left.IsBlinking",
            "Right.IsBlinking",
            "Left.PupilDiameterValid",
            "Left.PupilDiameter",
            "Right.PupilDiameterValid",
            "Right.PupilDiameter",
            "Left.PositionGuideValid",
            "Left.PositionGuide.x",
            "Left.PositionGuide.y",
            "Right.PositionGuideValid",
            "Right.PositionGuide.x",
            "Right.PositionGuide.y",
            "Left.GazeRay.IsValid",
            "Left.GazeRay.Origin.x",
            "Left.GazeRay.Origin.y",
            "Left.GazeRay.Origin.z",
            "Left.GazeRay.Direction.x",
            "Left.GazeRay.Direction.y",
            "Left.GazeRay.Direction.z",
            "Right.GazeRay.IsValid",
            "Right.GazeRay.Origin.x",
            "Right.GazeRay.Origin.y",
            "Right.GazeRay.Origin.z",
            "Left.GazeRay.Direction.x",
            "Left.GazeRay.Direction.y",
            "Left.GazeRay.Direction.z",
            "ConvergenceDistanceIsValid",
            "ConvergenceDistance",
            "GazeRay.IsValid",
            "GazeRay.Origin.x",
            "GazeRay.Origin.y",
            "GazeRay.Origin.z",
            "GazeRay.Direction.x",
            "GazeRay.Direction.y",
            "GazeRay.Direction.z",
            }
        );
    }


    // Update is called once per frame
    void Update()
    {
        if (ClientIP != currentClientIP || Port != oscClient.Port)
        {
            UpdateClientAddress();
        }

    }

    public void SendVideoPositions(Transform[] pivotTransforms, Transform[] quadTransforms)
    {
        Debug.Assert(pivotTransforms.Length == quadTransforms.Length);
        for (int i = 0; i < pivotTransforms.Length; i++)
        {
            if (pivotTransforms[i] != null && quadTransforms[i] != null)
            {
                //(typeof(float), "Azimuth (degrees)"),
                //(typeof(float), "Inclination (degrees)"),
                //(typeof(float), "Twist (degrees)"),
                //(typeof(float), "Rotation around X axis(degrees)"),
                //(typeof(float), "Rotation around Y axis (degrees)"),
                //(typeof(float), "Width (scale)"),
                //(typeof(float), "Height (scale)"),

                Send("/video/position", new ArrayList
                {
                    i,
                    pivotTransforms[i].localEulerAngles.x,
                    pivotTransforms[i].localEulerAngles.y,
                    pivotTransforms[i].localEulerAngles.z,
                    quadTransforms[i].localEulerAngles.x,
                    quadTransforms[i].localEulerAngles.y,
                    quadTransforms[i].localScale.x,
                    quadTransforms[i].localScale.y,
                });
            }
        }
    }


    void Send<Collection>(string address, Collection arguments) where Collection : IEnumerable
    {
        OSCMessage m = new OSCMessage(address);
        foreach (object argument in arguments)
        {
            m.Append(argument);
        }
        // Send but catch an exception and if we're logging then print it to log
        try
        {
            oscClient.Send(m);
        }
        catch (System.Exception e)
        {
            numSendErrors++;
            if (LogSentOscMessages || numSendErrors < 5)
            {
                Debug.LogWarning(e);
            }
            else if (!LogSentOscMessages && numSendErrors == 5)
            {
                Debug.LogWarning($"No further OSC send errors will be logged.");
            }
        }


        if (LogSentOscMessages)
        {
            Debug.Log($"Sent OSC to {currentClientIP}:{oscClient.Port}: {m.ToString()}");
        }
    }

    public void SendVideoNames()
    {
        foreach (var (type, names) in VideoCatalogue.GetVideoNames())
        {
            Send($"/video/names/{type}", names);
        }
    }

}
