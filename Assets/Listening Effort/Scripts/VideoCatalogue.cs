using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

public class VideoCatalogue : MonoBehaviour
{
    public VideoClip[] MaskingVideos;
    public VideoClip[] SpeechVideos;
    public VideoClip[] IdleVideos;

    // If downloaded they are left here.
    // name -> path
    public static Dictionary<string, string> DownloadedMaskingVideos = new Dictionary<string, string>();
    public static Dictionary<string, string> DownloadedSpeechVideos = new Dictionary<string, string>();
    public static Dictionary<string, string> DownloadedIdleVideos = new Dictionary<string, string>();
    
    public static Dictionary<string, string> GetDownloadedVideoDictionary(string type)
    {
        switch (type)
        {
            case "masking":
                return DownloadedMaskingVideos;
            case "speech":
                return DownloadedSpeechVideos;
            case "idle":
                return DownloadedIdleVideos;
            default:
                Debug.LogError($"VideoCatalogue.GetDownloadedVideoDictionary: Unknown type {type}.");
                return null;
        }
    }

    public void Start()
    {
        if (IsUsingDownloadedVideos)
        {
            Debug.Log($"VideoCatalogue started with {DownloadedMaskingVideos.Count} + {DownloadedSpeechVideos.Count} + {DownloadedIdleVideos.Count} downloaded videos.");
        }
    }


    public bool IsUsingDownloadedVideos => DownloadedMaskingVideos.Count > 0 || DownloadedSpeechVideos.Count > 0 || DownloadedIdleVideos.Count > 0;

    public bool Contains(string name)
    {
        return GetClip(name) != null;
    }
    public VideoClip GetClip(string name)
    {
        foreach (VideoClip[] clips in new VideoClip[][] { MaskingVideos, SpeechVideos, IdleVideos })
        {
            VideoClip c = clips.FirstOrDefault(clip => clip.name == name);
            if (c != null)
            {
                return c;
            }
        }
        return null;
    }

    public string GetURL(string name)
    {
        foreach (Dictionary<string, string> dictionary in new Dictionary<string, string>[] { DownloadedMaskingVideos, DownloadedSpeechVideos, DownloadedIdleVideos })
        {
            foreach (KeyValuePair<string,string> entry in dictionary)
            {
                if (entry.Key == name)
                {
                    return entry.Value;
                }
            }
        }
        return null;
    }

}
