using API_3DTI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class SpatializerResourceChecker : MonoBehaviour
{
    public static string[] defaultReverbModelNames = new string[]
    {
        "3DTI_BRIR_large",
        "3DTI_BRIR_medium",
        "3DTI_BRIR_small",
    };
    public static string reverbModelDirectory => $"{Application.persistentDataPath}/Data/Reverb/BRIR";

    public static string sampleRateLabel {
        get { 
            string sampleRateLabel = AudioSettings.outputSampleRate.ToString();
            if (!(new List<string> { "44100", "48000", "96000" }).Contains(sampleRateLabel))
            {
                Debug.LogError($"Unsupported sample rate: {sampleRateLabel}");
            }
            return sampleRateLabel;
        }
    }
    public static string customReverbSuffix => $"_{sampleRateLabel}Hz.3dti-brir";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public (string name, string path) findCustomReverb()
    {
        string[] paths = System.IO.Directory.GetFiles(reverbModelDirectory);
        var defaultReverbPaths = defaultReverbModelNames.
            Select(modelName => $"{modelName}_{sampleRateLabel}Hz.3dti-brir");
        foreach (string path in paths)
        {
            if (path.EndsWith(customReverbSuffix) && defaultReverbPaths.All(p => !path.EndsWith(p)))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                Debug.Log($"Found custom reverb model {name} at {path}");
                return (name, path);
            }
        }
        Debug.Log("No custom reverb model found");
        return ("","");
    }
}
