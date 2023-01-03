using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoDownloader : MonoBehaviour
{
    public Text StatusText;
    public string VideoName;
    // for testing video
    //private VideoPlayer _player;
    private UnityWebRequest _mostRecentRequest;
    //private bool _isDownloading;
    private bool _isAborting;
    private VideoPlayer _player;
    private string _originalStatusText;

    public event EventHandler<bool> IsReadyChanged;
    /// <summary>
    /// This indicates this video is downloaded and ready to go.
    /// </summary>
    private bool isReady;

    public bool IsReady {
        get => isReady;
        private set {
            isReady = value;
            IsReadyChanged?.Invoke(this, isReady);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        // player for testing videos
        _player = gameObject.AddComponent<VideoPlayer>();
        _originalStatusText = StatusText.text;

        string PrefsKey = $"{VideoName}_url)";
        InputField inputField = GetComponentInChildren<InputField>();


        inputField.onEndEdit.AddListener((string url) =>
        {
            PlayerPrefs.SetString(PrefsKey, url);
                StartCoroutine(downloadVideo(url));

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
        if (_mostRecentRequest != null)
        {
            if (!_isAborting)
            {
                _mostRecentRequest.Abort();
                StatusText.text = "Cancelling previous download...";
                _isAborting = true;
            }
            yield return 0;
        }
        _isAborting = false;
        IsReady = false;

        if (String.IsNullOrWhiteSpace(url))
        {
            StatusText.text = _originalStatusText;
            yield break;
        }


        StatusText.text = "Connecting...";
        //var thisRequest = new UnityWebRequest(url);
        //_mostRecentRequest = thisRequest;
        _mostRecentRequest = new UnityWebRequest(url);
        string savePath = Path.Combine(Application.persistentDataPath, $"{VideoName}.mp4");
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
        }
        else
        {
            Debug.Assert(_mostRecentRequest.downloadHandler.isDone);
            StatusText.text = "Checking video";

            _player.url = savePath;
            _player.source = VideoSource.Url;

            _player.prepareCompleted += (source) =>
            {
                //// test that we haven't had a new request come in
                //if (thisRequest == _mostRecentRequest)
                //{
                int width = _player.texture.width;
                int height = _player.texture.height;
                Debug.Log($"{VideoName} downloaded and of size {width}x{height}.");
                StatusText.text = $"Video downloaded successfully. Size: {width}x{height}.";
                IsReady = true;
                // END OF THIS REQUEST
                _mostRecentRequest = null;
            };

            _player.errorReceived += (source, message) =>
            {
                StatusText.text = "Video error: " + message.Replace(savePath, url);
                // END OF THIS REQUEST
                _mostRecentRequest = null;
            };

            _player.Prepare();
            // Request is not finished - will be completed by one of the above
            // callbacks
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_mostRecentRequest != null)
        {
            if (_mostRecentRequest.downloadProgress > 0)
            {
                StatusText.text = $"Downloading... {(int)(100 * _mostRecentRequest.downloadProgress)}% complete";
            }
        }
    }
}