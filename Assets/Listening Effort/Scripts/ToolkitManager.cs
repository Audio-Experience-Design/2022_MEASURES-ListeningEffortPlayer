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
        spatializer.GetSampleRate(out TSampleRateEnum sampleRate);

        string reverbModel = PlayerPrefs.GetString("reverbModel");
        if (reverbModel == SpatializerResourceChecker.customReverbModelName)
        {
            spatializer.SetBinaryResourcePath(BinaryResourceRole.ReverbBRIR, sampleRate, SpatializerResourceChecker.findCustomReverb().path);
        }
        else
        {
            foreach (TSampleRateEnum sr in System.Enum.GetValues(typeof(TSampleRateEnum)))
            {
                string sampleRateLabel =
                    sr == TSampleRateEnum.K44 ? "44100"
                    : sr == TSampleRateEnum.K48 ? "48000"
                    : sr == TSampleRateEnum.K96 ? "96000"
                    : throw new System.Exception("Invalid sample rate");
                spatializer.SetBinaryResourcePath(BinaryResourceRole.ReverbBRIR, sr, $"Data/Reverb/BRIR/{reverbModel}_{sampleRateLabel}Hz.3dti-brir.bytes");
            }
        }

        // HRTF
        spatializer.SetBinaryResourcePath(BinaryResourceRole.HighQualityHRTF, sampleRate, PlayerPrefs.GetString("hrtf"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
