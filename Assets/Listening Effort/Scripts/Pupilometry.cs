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
		//      var devices = new List<InputDevice>();
		//      InputDevices.GetDevicesAtXRNode(XRNode.LeftEye, devices);

		//int i = 0;
		//      foreach (var device in devices)
		//      {
		//          //device = devices[0];
		//          Debug.Log(string.Format($"{++i}: Device name '{0}' with role '{1}'", device.name, device.characteristics.ToString()));

		//	Eyes eyes;
		//	if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
		//	{
		//		//eyes.leftEyePosition;
		//	}
		//      }
		//if (devices.Count == 0)
		//{
		//	Debug.Log("No left eye devices found");
		//}

		TobiiXR.Start(GetComponent<TobiiXR_Settings>());
    }



    void Update()
	{
		sLogChanges = logChanges;

        //if (!isCallbackAdded && (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING || SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT))
        //{
        //	SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
        //	isCallbackAdded = true;

        //}

        //TobiiXR_EyeTrackingData eyeTrackingData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.Local);
		//eyeTrackingData.

		// with licence we can activate advanced features for tobii:

		//var data = TobiiXR.Advanced.LatestData;
  //      // Get a timestamp from the host system clock
  //      var now = TobiiXR.Advanced.GetSystemTimestamp();

  //      // Calculate system latency in milliseconds and print it
  //      var latency = (now - data.SystemTimestamp) / 1000.0f;
  //      Debug.Log(string.Format("System latency was {0:0.00} ms", latency));
    }

	//private void EyeCallback(ref EyeData_v2 eye_data)
	//{
	//	Data data = new Data
	//	{
	//		hasUser = !eye_data.no_user,
	//		leftPupilDiameterMm = eye_data.verbose_data.left.pupil_diameter_mm,
	//		rightPupilDiameterMm = eye_data.verbose_data.right.pupil_diameter_mm,
	//		isLeftPupilDiameterValid = eye_data.verbose_data.left.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_DIAMETER_VALIDITY),
	//		isRightPupilDiameterValid = eye_data.verbose_data.right.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_DIAMETER_VALIDITY),
	//		leftPupilPosition = eye_data.verbose_data.left.pupil_position_in_sensor_area,
	//		rightPupilPosition = eye_data.verbose_data.right.pupil_position_in_sensor_area,
	//		isLeftPupilPositionValid = eye_data.verbose_data.left.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_POSITION_IN_SENSOR_AREA_VALIDITY),
	//		isRightPupilPositionValid = eye_data.verbose_data.right.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_POSITION_IN_SENSOR_AREA_VALIDITY),
	//	};

	//	if (sLogChanges)
	//	{
	//		Debug.Log("Pupilometry data: " + data);
	//	}

	//	DataChanged?.Invoke(this, data);
	//}


}
