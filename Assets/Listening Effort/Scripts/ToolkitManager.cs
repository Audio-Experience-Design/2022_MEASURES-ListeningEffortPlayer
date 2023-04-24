using API_3DTI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolkitManager : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        Spatializer spatializer = GetComponent<Spatializer>();
        string reverbModel = PlayerPrefs.GetString("reverbModel");
        if (reverbModel == SpatializerResourceChecker.customReverbModelName)
        {
            spatializer.GetSampleRate(out TSampleRateEnum sampleRate);
            spatializer.SetBinaryResourcePath(BinaryResourceRole.ReverbBRIR, sampleRate, SpatializerResourceChecker.findCustomReverb().path);
        }
        else
        {
            foreach (TSampleRateEnum sampleRate in System.Enum.GetValues(typeof(TSampleRateEnum)))
            {
                string sampleRateLabel =
                    sampleRate == TSampleRateEnum.K44 ? "44100"
                    : sampleRate == TSampleRateEnum.K48 ? "48000"
                    : sampleRate == TSampleRateEnum.K96 ? "96000"
                    : throw new System.Exception("Invalid sample rate");
                spatializer.SetBinaryResourcePath(BinaryResourceRole.ReverbBRIR, sampleRate, $"Data/Reverb/BRIR/{reverbModel}_{sampleRateLabel}Hz.3dti-brir.bytes");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
