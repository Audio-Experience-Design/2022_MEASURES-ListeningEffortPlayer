using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
//using ViveSR.anipal.Eye;
using UnityEngine.XR;
//using Tobii.XR;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Unity.XR.PXR;


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
	}
	[SerializeField]
	public bool logChanges = false;
	private static bool sLogChanges = false;

	private static bool isCallbackAdded;

	public static event EventHandler<Data> DataChanged;

	// Start is called before the first frame update
	void Start()
	{
		//TobiiXR.Start(GetComponent<TobiiXR_Settings>());
		
    }



    void Update()
	{
		sLogChanges = logChanges;

		// with licence we can activate advanced features for tobii:

		//var data = TobiiXR.Advanced.LatestData;
		//      // Get a timestamp from the host system clock
		//      var now = TobiiXR.Advanced.GetSystemTimestamp();

		//      // Calculate system latency in milliseconds and print it
		//      var latency = (now - data.SystemTimestamp) / 1000.0f;
		//      Debug.Log(string.Format("System latency was {0:0.00} ms", latency));

		// Get eye tracking data from the pico
		Data data = new Data();
		
		//PXR_EyeTracking.GetLeftEyePoseStatus(out uint leftEyePoseStatus);
		//data.isLeftPupilPositionValid = leftEyePoseStatus == 1;
		
		// this returns true even if the eye isn't being tracked.
		// but an untracked eye always returns 0.0
		PXR_EyeTracking.GetLeftEyePositionGuide(out Vector3 leftPosition);
		data.leftPupilPosition = new Vector2(leftPosition.x, leftPosition.y);
		data.isLeftPupilPositionValid = data.leftPupilPosition != Vector2.zero;

		// repeat for right eye
		//PXR_EyeTracking.GetRightEyePoseStatus(out uint rightEyePoseStatus);
		//data.isRightPupilPositionValid = rightEyePoseStatus == 1;
		PXR_EyeTracking.GetRightEyePositionGuide(out Vector3 rightPosition);
		data.rightPupilPosition = new Vector2(rightPosition.x, rightPosition.y);
		data.isRightPupilPositionValid= data.rightPupilPosition != Vector2.zero;

		//data.isRightPupilPositionValid = PXR_EyeTracking.GetRightEyePositionGuide(out Vector3 rightPosition);
		//data.rightPupilPosition = new Vector2(rightPosition.x, rightPosition.y);
		data.isLeftPupilDiameterValid = false;
		data.isRightPupilDiameterValid=false;
		data.leftPupilDiameterMm = 0;
		data.rightPupilDiameterMm = 0;
        if (sLogChanges)
		{
			Debug.Log("Pupilometry data: " + data);
		}
		DataChanged?.Invoke(this, data);
    }


}
