using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

public class VideoCatalogue : MonoBehaviour
{
    public VideoClip[] MaskingVideos;
    public VideoClip[] SentenceVideos;
    public VideoClip[] IdleVideos;

    public bool Contains(string name)
    {
        return GetClip(name) != null;
    }
    public VideoClip GetClip(string name)
    {
        foreach (VideoClip[] clips in new VideoClip[][] { MaskingVideos, SentenceVideos, IdleVideos })
        {
            VideoClip c = clips.FirstOrDefault(clip => clip.name == name);
            if (c != null)
            {
                return c;
            }
        }
        return null;
    }

}
