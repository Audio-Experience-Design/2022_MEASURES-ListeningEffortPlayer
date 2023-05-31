using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoChecker : MonoBehaviour
{
    public VideoCatalogue videoCatalogue;
    public Text statusText;
    public string rootVideoDirectory => Application.persistentDataPath + "/Videos";
    public string[] videoTypes => new string[] { "masking", "speech", "idle" };
    public string[] videoDirectories => videoTypes.Select(type => $"{rootVideoDirectory}/{type}").ToArray();
    private bool _videosAreOK = false;
    public bool videosAreOK
    {
        get => _videosAreOK;
        private set
        {
            _videosAreOK = value;
            videosAreOKChanged?.Invoke(this, value);
        }
    }
    public event System.EventHandler<bool> videosAreOKChanged;

    [NonSerialized]
    public bool _isCheckingVideos = false;
    public bool isCheckingVideos
    {
        get => _isCheckingVideos;
        private set
        {
            _isCheckingVideos = value;
            isCheckingVideosChanged?.Invoke(this, value);
        }
    }
    public event System.EventHandler<bool> isCheckingVideosChanged;

    // Start is called before the first frame update
    void Start()
    {
        // check video directories exist and make them if they don't
        System.Array.ForEach(videoDirectories, dir =>
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        );

    }

    private void setStatus(string text)
    {
        Debug.Log(text, this);
        statusText.text = text;
    }

    public void StartCheckingVideos()
    {
        StartCoroutine(CheckVideos());
    }

    public (string type, string[] names)[] GetVideoPaths()
    {
        Debug.Assert(videoDirectories.Length == videoTypes.Length);
        var paths = new (string type, string[] names)[videoTypes.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            string videoDirectory = videoDirectories[i];
            paths[i].type = videoTypes[i];
            paths[i].names = System.IO.Directory.GetFiles(videoDirectory);
        }
        return paths;
    }

    private IEnumerator CheckVideos()
    {
        if (isCheckingVideos)
        {
            Debug.LogWarning("Cannot call CheckVideos as it's already running.", this);
            yield break;
        }
        isCheckingVideos = true;
        setStatus("Checking videos");
        yield return null;
        // create an array to hold the number of videos of each type, initialize it with zeros
        int[] videoCounts = new int[3] { 0, 0, 0 };
        (string type, string[] names)[] foundVideoPaths = GetVideoPaths();
        List<string> failedVideoPaths = new List<string>();
        // iterate through the videoDirectories, loading each video one at a time to check its resolution is non zero
        for (int i = 0; i < videoDirectories.Length; i++)
        {
            string videoDirectory = videoDirectories[i];
            string videoType = videoTypes[i];
            setStatus($"Checking {videoType} videos");
            string[] videoPaths = foundVideoPaths[i].names;
            videoCounts[i] = videoPaths.Length;
            yield return null;
            foreach (string videoPath in videoPaths)
            {
                setStatus($"Checking {videoType} video {videoPath}");
                yield return null;
                VideoPlayer player = gameObject.AddComponent<VideoPlayer>();
                player.url = videoPath;
                player.source = VideoSource.Url;
                player.SetDirectAudioVolume(0, 0);
                player.Prepare();
                int timeLeft = 10;
                while (!player.isPrepared && timeLeft > 0)
                {
                    yield return new WaitForSeconds(1);
                    timeLeft--;
                }
                if (timeLeft == 0 || player.width == 0 || player.height == 0)
                {
                    setStatus($"Video {videoPath} failed to load");
                    failedVideoPaths.Add(videoPath);
                    yield return null;
                }
                else
                {
                    setStatus($"Video {videoPath} loaded successfully");
                    videoCounts[i]++;
                    // add the video to VideoCatalogue
                    videoCatalogue.GetDownloadedVideoDictionary(videoType).Add(Path.GetFileName(videoPath), videoPath);
                    Debug.Assert(videoCatalogue.GetDownloadedVideoDictionary(videoType).ContainsKey(Path.GetFileName(videoPath)));
                    yield return null;
                }
                Destroy(player);
            }
        }
        videosAreOK = failedVideoPaths.Count == 0 && videoCounts.All(i => i > 0);
        setStatus($"{(videosAreOK ? "Videos loaded OK" : "There's a problem with the video files")}.\n{videoCounts.Sum()} videos checked OK. {failedVideoPaths.Count} videos failed to load.\n" +
            videoTypes.Select((type, i) => $"{type}: {videoCounts[i]} videos loaded.").Aggregate((a, b) => a + " " + b)
            );
        videoCatalogue.LogVideoNames();
        isCheckingVideos = false;
    }
}
