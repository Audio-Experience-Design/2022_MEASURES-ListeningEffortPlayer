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


using PupilometryData = Tobii.XR.TobiiXR_AdvancedEyeTrackingData;


// Some strange stuff happens to the instance of this class with the callback being
// called on a null instance. It might be to do with the Marshal function pointer.
// Anyway, to avoid it, many things are kept as static.
public class Pupilometry : MonoBehaviour
{
    private long lastTimestamp = -4;

    public event EventHandler<PupilometryData> DataChanged;
    private bool hasWarningBeenIssuedForNoData = false;

    // Start is called before the first frame update
    void Start()
    {
        //TobiiXR.Start(GetComponent<TobiiXR_Settings>());

    }



    void Update()
    {
        if (!TobiiXR.IsAdvancedInitialized)
        {
            if (!hasWarningBeenIssuedForNoData)
            {
                Debug.LogError($"Unable to get pupilometry data from TobiiXR SDK (cannot access Advanced Data - likely due to licensing issue).");
                hasWarningBeenIssuedForNoData = true;
            }
        }
        else
        {
            TobiiXR_AdvancedEyeTrackingData data = TobiiXR.Advanced.LatestData;
            hasWarningBeenIssuedForNoData = false;
            Debug.Assert(data.DeviceTimestamp >= lastTimestamp);
            if (data.DeviceTimestamp > lastTimestamp)
            {
                DataChanged?.Invoke(this, data);
                lastTimestamp = data.DeviceTimestamp;
            }
        }
    }
}
