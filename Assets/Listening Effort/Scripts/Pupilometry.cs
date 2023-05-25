using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
//using ViveSR.anipal.Eye;
using UnityEngine.XR;
using Tobii.XR;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
//using Unity.XR.PXR;


// Some strange stuff happens to the instance of this class with the callback being
// called on a null instance. It might be to do with the Marshal function pointer.
// Anyway, to avoid it, many things are kept as static.
public class Pupilometry : MonoBehaviour
{
    [Serializable]
    public struct Data
    {
        public bool hasUser;
        public float leftPupilDiameterMm, rightPupilDiameterMm;
        public bool isLeftPupilDiameterValid, isRightPupilDiameterValid;
        ///  Normalized position in sensor area, in range [0,1]
        public Vector2 leftPupilPosition, rightPupilPosition;
        public bool isLeftPupilPositionValid, isRightPupilPositionValid;
        public bool isLeftEyeBlinking, isRightEyeBlinking;
    }
    [SerializeField]
    public bool logChanges = false;
    //private static bool sLogChanges = false;

    private long lastTimestamp = -4;

    //private static bool isCallbackAdded;
    public event EventHandler<Data> DataChanged;


    // Start is called before the first frame update
    void Start()
    {
        //TobiiXR.Start(GetComponent<TobiiXR_Settings>());

    }



    void FixedUpdate()
    {
        // with licence we can activate advanced features for tobii:

        var data = TobiiXR.Advanced.LatestData;

        Debug.Assert(data.DeviceTimestamp >= lastTimestamp);
        if (data.DeviceTimestamp > lastTimestamp)
        {
            // Get eye tracking data from the pico
            Data d = new Data();
            d.leftPupilDiameterMm = data.Left.PupilDiameter;
            d.isLeftPupilDiameterValid = data.Left.PupilDiameterValid;
            d.leftPupilPosition = data.Left.PositionGuide;
            d.isLeftPupilPositionValid = data.Left.PositionGuideValid;
            d.isLeftEyeBlinking = data.Left.IsBlinking;

            d.rightPupilDiameterMm = data.Right.PupilDiameter;
            d.isRightPupilDiameterValid = data.Right.PupilDiameterValid;
            d.rightPupilPosition = data.Right.PositionGuide;
            d.isRightPupilDiameterValid = data.Right.PositionGuideValid;
            d.isRightEyeBlinking = data.Right.IsBlinking;





            // OLD Non-Tobii code

            //// this returns true even if the eye isn't being tracked.
            //// but an untracked eye always returns 0.0
            //bool leftSuccess = PXR_EyeTracking.GetLeftEyePositionGuide(out Vector3 leftPosition);
            //data.leftPupilPosition = new Vector2(leftPosition.x, leftPosition.y);
            //data.isLeftPupilPositionValid = leftSuccess && data.leftPupilPosition != Vector2.zero;

            //// repeat for right eye
            //bool rightSuccess = PXR_EyeTracking.GetRightEyePositionGuide(out Vector3 rightPosition);
            //data.rightPupilPosition = new Vector2(rightPosition.x, rightPosition.y);
            //data.isRightPupilPositionValid= rightSuccess && data.rightPupilPosition != Vector2.zero;

            //data.isLeftPupilDiameterValid = false;
            //data.isRightPupilDiameterValid=false;
            //data.leftPupilDiameterMm = 0;
            //data.rightPupilDiameterMm = 0;
            //      if (sLogChanges)
            //{
            //	Debug.Log("Pupilometry data: " + data);
            //}

            DataChanged?.Invoke(this, d);
        }
    }


}
