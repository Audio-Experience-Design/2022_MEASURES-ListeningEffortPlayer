using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator

public class VideoFPSCounter : MonoBehaviour
{
    VideoPlayer videoPlayer;
    private float timeOfLastFrame;
    public float CurrentFPS;

    // Start is called before the first frame update
    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.sendFrameReadyEvents = true;

        videoPlayer.frameReady += (VideoPlayer source, long frameIdx) =>
        {
            float dt = Time.time - timeOfLastFrame;
            timeOfLastFrame = Time.time;
            if (dt==0.0)
            {
                CurrentFPS = -1;
            }
            else
            {
                CurrentFPS = 1.0f / dt;
            }
        };

        videoPlayer.started += (VideoPlayer) =>
        {
            timeOfLastFrame = Time.time;
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - timeOfLastFrame > 3)
        {
            CurrentFPS = 0;
        }
    }
}
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
