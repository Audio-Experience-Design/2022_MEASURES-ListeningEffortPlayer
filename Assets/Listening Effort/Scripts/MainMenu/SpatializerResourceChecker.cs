using API_3DTI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public static class SpatializerResourceChecker
{
    public static string[] defaultReverbModelNames = new string[]
    {
        "3DTI_BRIR_large",
        "3DTI_BRIR_medium",
        "3DTI_BRIR_small",
    };
    public static string customReverbModelName = "CUSTOM_REVERB_MODEL";
    public static string reverbModelDirectory => $"{Application.persistentDataPath}/Data/Reverb/BRIR";
    public static string hrtfDirectory => $"{Application.persistentDataPath}/Data/HighQuality/HRTF";


    public static string customReverbSuffix => $"_{sampleRateLabel}Hz.3dti-brir";
    public static string hrtfSuffix => $"_{sampleRateLabel}Hz.3dti-hrtf";


    public static string sampleRateLabel
    {
        get
        {
            string sampleRateLabel = AudioSettings.outputSampleRate.ToString();
            if (!(new List<string> { "44100", "48000", "96000" }).Contains(sampleRateLabel))
            {
                Debug.LogError($"Unsupported sample rate: {sampleRateLabel}");
            }
            return sampleRateLabel;
        }
    }

    public static (string name, string path) findCustomReverb()
    {
        string[] paths = System.IO.Directory.GetFiles(reverbModelDirectory);
        var defaultReverbPaths = defaultReverbModelNames.
            Select(modelName => $"{modelName}_{sampleRateLabel}Hz.3dti-brir");
        foreach (string path in paths)
        {
            // Spatializer dumps its default reverb model into the folder so need to check against that.
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


    public static (string name, string filename, string path)[] getDefaultHRTFNamesAndPaths()
    {
        //string hrtfResourceSuffix = hrtfSuffix + ".bytes";
        // LoadAll resource .name doesn't include the extra ".bytes" extension but Spatializer assumes it.
        string resourceDirectory = "Data/HighQuality/HRTF"; // this sits under a "resources" folder in assets
        return Resources.LoadAll<TextAsset>(resourceDirectory)
            .Where(x => x.name.EndsWith(hrtfSuffix))
            .Select(x => (
            Path.GetFileName(x.name).Substring("3DTI_HRTF_".Length, Path.GetFileName(x.name).Length - hrtfSuffix.Length),
            Path.GetFileName(x.name) + ".bytes",
            resourceDirectory + "/" + x.name + ".bytes"
            ))
            .ToArray();
        // e.g. (3DTI_HRTF_IRC1032_128s, 3DTI_HRTF_IRC1032_128s_44100Hz.3dti-hrtf.bytes, Data/HighQuality/HRTF/3DTI_HRTF_IRC1032_128s_44100Hz.3dti-hrtf.bytes)
    }

    public static (string name, string filename, string path)[] getHRTFs()
    {
        string[] paths = System.IO.Directory.GetFiles(hrtfDirectory);
        var defaultHRTFs = getDefaultHRTFNamesAndPaths().ToList();
        var customHRTFs = new List<(string name, string filename, string path)>();
        foreach (string path in paths)
        {
            // Spatializer dumps its default HRTF into the folder so need to check against that.
            // when scanning against default hrtf filenames, we need to remove the extra .bytes extension that binary resources have.
            if (path.EndsWith(hrtfSuffix) && defaultHRTFs.All(x => !path.EndsWith(Path.GetFileNameWithoutExtension(x.filename))))
            {
                string filename = Path.GetFileName(path);
                Debug.Assert(filename.EndsWith(hrtfSuffix));
                string name = filename.Substring(0, filename.Length - hrtfSuffix.Length);
                Debug.Log($"Found custom HRTF model {name} at {path}");
                customHRTFs.Append((name, filename, path));
            }
        }
        return customHRTFs.Concat(defaultHRTFs).ToArray();
    }
}
