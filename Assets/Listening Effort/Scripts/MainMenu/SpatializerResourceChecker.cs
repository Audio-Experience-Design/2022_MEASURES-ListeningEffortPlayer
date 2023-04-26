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
    public static string[] hrtfSuffixes => new string[]
    {
        $"_{sampleRateLabel}Hz.3dti-hrtf",
        $"_{sampleRateLabel}Hz.sofa",
        };


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
        if (!Directory.Exists(reverbModelDirectory))
        {
            // create it - catch and print any errors
            try
            {
                Directory.CreateDirectory(reverbModelDirectory);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create reverb model directory {reverbModelDirectory}: {e.Message}");
                return ("", "");
            }
        }

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
        return ("", "");
    }


    public static (string name, string filename, string path)[] getDefaultHRTFNamesAndPaths()
    {
        //string hrtfResourceSuffix = hrtfSuffix + ".bytes";
        // LoadAll resource .name doesn't include the extra ".bytes" extension but Spatializer assumes it.
        string resourceDirectory = "Data/HighQuality/HRTF"; // this sits under a "resources" folder in assets
        var textAssets = Resources.LoadAll<TextAsset>(resourceDirectory)
            .Where(x => hrtfSuffixes.Any(suffix => x.name.EndsWith(suffix)))
            .ToArray();

        Debug.Log($"Collating HRTF names and paths from {textAssets.Length} text assets");

        var result = new List<(string name, string filename, string path)>();
        foreach (TextAsset asset in textAssets)
        {
            foreach (string suffix in hrtfSuffixes)
            {
                if (asset.name.EndsWith(suffix))
                {
                    string filename = asset.name + ".bytes";
                    string path = $"{resourceDirectory}/{asset.name}.bytes";
                    string name = asset.name.Substring("3DTI_HRTF_".Length, asset.name.Length - suffix.Length);
                    result.Add((name, filename, path));
                }
            }
        }
        return result.ToArray();
        // e.g. (3DTI_HRTF_IRC1032_128s, 3DTI_HRTF_IRC1032_128s_44100Hz.3dti-hrtf.bytes, Data/HighQuality/HRTF/3DTI_HRTF_IRC1032_128s_44100Hz.3dti-hrtf.bytes)
    }

    public static (string name, string filename, string path)[] getHRTFs()
    {
        Debug.Log($"Searching for built-in HRTFs");
        var defaultHRTFs = getDefaultHRTFNamesAndPaths().ToList();
        Debug.Log($"Found {defaultHRTFs.Count} default HRTF models");

        // create hrtfDirectory if it doesn't exist. on failure catch and print and errors
        if (!Directory.Exists(hrtfDirectory))
        {
            try
            {
                Directory.CreateDirectory(hrtfDirectory);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create HRTF directory {hrtfDirectory}: {e.Message}");
                return defaultHRTFs.ToArray();
            }
        }


        var customHRTFs = new List<(string name, string filename, string path)>();
        string[] paths = System.IO.Directory.GetFiles(hrtfDirectory);
        foreach (string path in paths)
        {
            Debug.Log($"Searching for HRTFS in {path}");
            // Spatializer dumps its default HRTF into the folder so need to check against that.
            // when scanning against default hrtf filenames, we need to remove the extra .bytes extension that binary resources have.
            foreach (string suffix in hrtfSuffixes)
            {
                if (path.EndsWith(suffix) && defaultHRTFs.All(x => !path.EndsWith(Path.GetFileNameWithoutExtension(x.filename))))
                {
                    string filename = Path.GetFileName(path);
                    Debug.Assert(filename.EndsWith(suffix));
                    string name = filename.Substring(0, filename.Length - suffix.Length);
                    Debug.Log($"Found custom HRTF model {name} at {path}");
                    customHRTFs.Add((name, filename, path));
                }
            }
        }
        Debug.Log($"Found {customHRTFs.Count} custom HRTF models");
        return customHRTFs.Concat(defaultHRTFs).ToArray();
    }
}
