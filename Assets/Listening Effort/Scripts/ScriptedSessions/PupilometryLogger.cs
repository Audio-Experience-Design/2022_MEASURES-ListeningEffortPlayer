using System;
using System.IO;
using System.Text;
using UnityEngine;
using static ScriptedSessionController;
using PupilometryData = Tobii.XR.TobiiXR_AdvancedEyeTrackingData;

public sealed class PupilometryLogger : IDisposable
{
    public PupilometryLogger(string sessionFolder, string sessionLabel, string challengeLabel, string configurationName, DateTime sessionStartTimeUTC, Pupilometry pupilometry, TransformWatcher headTransform)
    {
        this.sessionStartTimeUTC = sessionStartTimeUTC;
        this.pupilometry = pupilometry;
        this.headTransform = headTransform;

        string challengeLabelPadded = challengeLabel;
        if (int.TryParse(challengeLabel, out int challengeNumber))
        {
            challengeLabelPadded = $"{challengeNumber:000}";
        }
        this.logWriter = new StreamWriter(Path.Join(sessionFolder, $"{sessionLabel}_pupilometry_{challengeLabelPadded}.csv"), true, Encoding.UTF8);

        pupilometry.DataChanged += pupilometryCallback;
        headTransform.TransformChanged += headTransformCallback;

        LogUtilities.writeCSVLine(logWriter, new ScriptedSessionController.SessionEventLogEntry
        {
            Timestamp = LogUtilities.localTimestamp(),
            SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
            Configuration = configurationName,
            EventName = "Started pupilometry log",
            ChallengeNumber = challengeLabel,
        });
    }

    ~PupilometryLogger()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            pupilometry.DataChanged -= pupilometryCallback;
            headTransform.TransformChanged -= headTransformCallback;
            LogUtilities.writeCSVLine(logWriter, new ScriptedSessionController.SessionEventLogEntry
            {
                Timestamp = LogUtilities.localTimestamp(),
                SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
                EventName = "Ended pupilometry log",
            });
            logWriter.Close();
            logWriter.Dispose();
        }
    }

    private void pupilometryCallback(object sender, PupilometryData data)
    {
        LogUtilities.writeCSVLine(logWriter, new ScriptedSessionController.SessionEventLogEntry
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
    }

    private void headTransformCallback(object sender, Transform data)
    {
        LogUtilities.writeCSVLine(logWriter, new SessionEventLogEntry
        {
            Timestamp = LogUtilities.localTimestamp(),
            SessionTime = (DateTime.UtcNow - sessionStartTimeUTC).TotalSeconds.ToString("F3"),
            EventName = "HeadRotation",
            HeadRotationEulerX = data.rotation.eulerAngles.x.ToString(),
            HeadRotationEulerY = data.rotation.eulerAngles.y.ToString(),
            HeadRotationEulerZ = data.rotation.eulerAngles.z.ToString(),
        });
    }

    private StreamWriter logWriter;
    private DateTime sessionStartTimeUTC;
    private Pupilometry pupilometry;
    private TransformWatcher headTransform;

    private bool isDisposed = false;
    
}