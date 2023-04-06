using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

public class VideoDownloader : MonoBehaviour
{
    public Text StatusText;
    public InputField inputField;
    //public string VideoName;
    // for testing video
    //private VideoPlayer _player;
    private UnityWebRequest _mostRecentRequest;
    private Coroutine _currentDownloadCoroutine;
    //private bool _isDownloading;
    private bool _isAborting;
    private VideoPlayer _player;
    private string _originalStatusText;

    public event EventHandler<bool> IsReadyChanged;
    /// <summary>
    /// This indicates this video is downloaded and ready to go.
    /// </summary>
    private bool _isReady = true;

    public bool IsReady
    {
        get => _isReady;
        private set
        {
            _isReady = value;
            IsReadyChanged?.Invoke(this, _isReady);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        // player for testing videos
        _player = gameObject.AddComponent<VideoPlayer>();
        _originalStatusText = StatusText.text;

        string PrefsKey = $"lastContentURL";
        //InputField inputField = GetComponentInChildren<InputField>();


        inputField.onEndEdit.AddListener((string url) =>
        {
            PlayerPrefs.SetString(PrefsKey, url);
            Debug.Log("Starting coroutine downloadVideo");
            if (_currentDownloadCoroutine != null && _mostRecentRequest != null)
            {
                Debug.Log("Stopping previous downloadVideo coroutine");
                StopCoroutine(_currentDownloadCoroutine);
            }
            _currentDownloadCoroutine = null;
            _mostRecentRequest = null;

            _currentDownloadCoroutine = StartCoroutine(downloadVideo(url));

        });

        if (PlayerPrefs.HasKey(PrefsKey))
        {
            inputField.text = PlayerPrefs.GetString(PrefsKey, "");
            inputField.onEndEdit.Invoke(inputField.text);
        }

    }

    IEnumerator downloadVideo(string url)
    {
        IsReady = false;

        // To prevent overwriting of values, if there is already a request
        // in process we need to stop it first
        int secondsPassed = 0;
        while (_mostRecentRequest != null && secondsPassed < 30)
        {
            StatusText.text = $"Cancelling previous download... (timeout in {30 - secondsPassed} seconds)";
            if (!_isAborting)
            {
                _mostRecentRequest.Abort();
                _isAborting = true;
            }
            secondsPassed++;
            yield return new WaitForSecondsRealtime(1);
        }
        if (_mostRecentRequest != null)
        {
            Debug.LogWarning($"Timed out waiting for previous request to cancel.");
            _mostRecentRequest = null;
        }
        _isAborting = false;
        IsReady = false;

        VideoCatalogue.DownloadedMaskingVideos.Clear();
        VideoCatalogue.DownloadedSpeechVideos.Clear();
        VideoCatalogue.DownloadedIdleVideos.Clear();

        if (String.IsNullOrWhiteSpace(url))
        {
            StatusText.text = _originalStatusText;
            IsReady = true;
            yield break;
        }

        StatusText.text = "Connecting...";
        //var thisRequest = new UnityWebRequest(url);
        //_mostRecentRequest = thisRequest;
        _mostRecentRequest = new UnityWebRequest(url);
        string savePath = Path.Combine(Application.persistentDataPath, $"content.zip");
        _mostRecentRequest.downloadHandler = new DownloadHandlerFile(savePath)
        {
            removeFileOnAbort = true
        };
        _mostRecentRequest.timeout = 30;
        //_isDownloading = true;

        yield return _mostRecentRequest.SendWebRequest();

        //_isDownloading = false;

        if (_mostRecentRequest.isNetworkError || _mostRecentRequest.isHttpError)
        {
            // END OF THIS REQUEST
            StatusText.text = "Download error: " + _mostRecentRequest.error;
            _mostRecentRequest = null;
            yield break;
        }
        StatusText.text = "Unzipping file...";

        // unzip content.zip
        // create "content" directory
        string contentPath = Path.Combine(Application.persistentDataPath, "content");
        try
        {
            if (Directory.Exists(contentPath))
            {
                Directory.Delete(contentPath, true);
            }
        }
        catch (Exception e)
        {
            StatusText.text = $"ERROR: Failed to delete previously downloaded content: {e.ToString()}";
            // END OF THIS REQUEST
            _mostRecentRequest = null;
            yield break;
        }
        try
        {
            Directory.CreateDirectory(contentPath);
        }
        catch (Exception e)
        {
            StatusText.text = $"ERROR: Failed to create content directory: {e.ToString()}";
            // END OF THIS REQUEST
            _mostRecentRequest = null;
            yield break;
        }
        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(savePath, contentPath);
        }
        catch (Exception e)
        {
            StatusText.text = $"ERROR: Failed to extract downloaded zip file: {e.ToString()}";
            // END OF THIS REQUEST
            _mostRecentRequest = null;
            yield break;
        }
        string[] videoTypes =
        {
            "idle",
            "masking",
            "speech",
        };
        int numVideos = 0;
        foreach (string videoType in videoTypes)
        {
            // check directory exists in contentPath
            if (!Directory.Exists(Path.Combine(contentPath, videoType)))
            {
                StatusText.text = $"ERROR: Failed to find expected folder {videoType} in downloaded content. Note: folder names should be lowercase.";
                // END OF THIS REQUEST
                _mostRecentRequest = null;
                yield break;
            }
            // iterate through the files in this folder
            string[] videoPaths = Directory.GetFiles(Path.Combine(contentPath, videoType), "*.mp4");
            if (videoPaths.Length == 0)
            {
                StatusText.text = $"ERROR: No videos found in folder {videoType}.";
                // END OF THIS REQUEST
                _mostRecentRequest = null;
                yield break;
            }
            foreach (string video in videoPaths)
            {
                StatusText.text = $"Checking video: {video}...";
                yield return null;
                bool isLoadedOK = false;
                bool isError = false;
                _player.url = video;
                _player.source = VideoSource.Url;
                _player.prepareCompleted += (source) =>
                {
                    int width = _player.texture.width;
                    int height = _player.texture.height;
                    Debug.Log($"{Path.GetFileName(video)} checked and of size {width}x{height}.");
                    StatusText.text = $"Video downloaded successfully. Size: {width}x{height}.";
                    isLoadedOK = true;
                };

                _player.errorReceived += (source, message) =>
                {
                    Debug.Log(message);
                    StatusText.text = "Video error: " + message;
                    isError = true;
                };

                while (!isLoadedOK && !isError)
                {
                    yield return null;
                }

                if (isError)
                {
                    // END OF THIS REQUEST
                    _mostRecentRequest = null;
                    yield break;
                }

                Debug.Assert(isLoadedOK);
                VideoCatalogue.GetDownloadedVideoDictionary(videoType).Add(Path.GetFileName(video), video);

                numVideos++;
            }
        }
        StatusText.text = $"Download successful. {numVideos} videos loaded.";
        Debug.Log($"{numVideos} downloaded and checked OK. Ready to proceed.");
        IsReady = true;
        _mostRecentRequest = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (_mostRecentRequest != null)
        {
            if (_mostRecentRequest.downloadProgress > 0.0 && _mostRecentRequest.downloadProgress < 1.0)
            {
                StatusText.text = $"Downloading... {(int)(100 * _mostRecentRequest.downloadProgress)}% complete";
            }
        }
    }
}