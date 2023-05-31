using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

//public enum VideoType
//{
//    Masking,
//    Speech,
//    Idle,
//    Invalid,
//}

public class VideoCatalogue : MonoBehaviour
{
    public VideoClip[] DemoMaskingVideos;
    public VideoClip[] DemoSpeechVideos;
    public VideoClip[] DemoIdleVideos;

    // If downloaded they are left here.
    // name -> path
    public static Dictionary<string, string> UserMaskingVideos = new Dictionary<string, string>();
    public static Dictionary<string, string> UserSpeechVideos = new Dictionary<string, string>();
    public static Dictionary<string, string> UserIdleVideos = new Dictionary<string, string>();
    //public static bool UseDemoVideos = true;
    public static bool HasUserVideos = UserMaskingVideos.Count + UserSpeechVideos.Count > 0;

    public static Dictionary<string, string> GetDownloadedVideoDictionary(string type)
    {
        switch (type)
        {
            case "masking":
                return UserMaskingVideos;
            case "speech":
                return UserSpeechVideos;
            case "idle":
                return UserIdleVideos;
            default:
                Debug.LogError($"VideoCatalogue.GetDownloadedVideoDictionary: Unknown type {type}.");
                return null;
        }
    }

    public (string type, IEnumerable<string> names)[] GetVideoNames()
    {
        if (HasUserVideos)
        {
            return new (string type, IEnumerable<string> names)[]
            {
                ("masking", UserMaskingVideos.Keys),
                ("speech", UserSpeechVideos.Keys),
                ("idle", UserIdleVideos.Keys),
            };
        }
        else
        {
            return new (string type, IEnumerable<string> names)[]
            {
            ("masking", DemoMaskingVideos.Select(clip => clip.name)),
            ("speech", DemoSpeechVideos.Select(clip => clip.name)),
            ("idle", DemoIdleVideos.Select(clip => clip.name)),
            };
        }
    }

    public void Start()
    {
        Debug.Log($"VideoCatalogue started with {UserMaskingVideos.Count} + {UserSpeechVideos.Count} + {UserIdleVideos.Count} user videos and {DemoMaskingVideos.Length} + {DemoSpeechVideos.Length} + {DemoIdleVideos.Length} in-built demo videos.");
    }


    //public bool IsUsingUserVideos => !UseDemoVideos;

    private bool Invariant()
    {
        return UserMaskingVideos.Keys.All(name => GetURL(name) != null)
            && UserSpeechVideos.Keys.All(name => GetURL(name) != null)
            && UserIdleVideos.Keys.All(name => GetURL(name) != null)
            && DemoMaskingVideos.All(clip => GetClip(clip.name) != null)
            && DemoSpeechVideos.All(clip => GetClip(clip.name) != null)
            && DemoIdleVideos.All(clip => GetClip(clip.name) != null)
            // and each demo type must have at least one video
            && DemoMaskingVideos.Length > 0
            && DemoSpeechVideos.Length > 0
            && DemoIdleVideos.Length > 0;
    }

    public bool Contains(string name)
    {
        return GetURL(name) != null || GetClip(name) != null;
    }

    public VideoClip GetClip(string name)
    {
        foreach (VideoClip[] clips in new VideoClip[][] { DemoMaskingVideos, DemoSpeechVideos, DemoIdleVideos })
        {
            VideoClip c = clips.FirstOrDefault(clip => clip.name == name || $"{clip.name}.mp4" == name);
            if (c != null)
            {
                return c;
            }
        }
        return null;
    }

    public string GetURL(string name)
    {
        foreach (Dictionary<string, string> dictionary in new Dictionary<string, string>[] { UserMaskingVideos, UserSpeechVideos, UserIdleVideos })
        {
            foreach (KeyValuePair<string, string> entry in dictionary)
            {
                if (entry.Key == name)
                {
                    return entry.Value;
                }
            }
        }
        return null;
    }

    public void SetPlayerSource(VideoPlayer player, string name)
    {
        string url = GetURL(name);
        if (url != null)
        {
            player.source = VideoSource.Url;
            player.url = url;
        }
        else
        {
            VideoClip clip = GetClip(name);
            if (clip != null)
            {
                player.source = VideoSource.VideoClip;
                player.clip = clip;
            }
            else
            {
                throw new Exception($"VideoCatalogue.LoadVideoIntoPlayer: No video with name {name}.");
            }
        }
    }

}
