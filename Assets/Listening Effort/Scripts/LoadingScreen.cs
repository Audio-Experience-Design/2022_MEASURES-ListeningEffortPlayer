using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public Text loadingText;
    public VideoChecker videoChecker;
    public Button startButton;
    public Button demoButton;
    public Button reloadVideosButton;

    public void Awake()
    {
        
        loadingText.text = loadingText.text
            .Replace("{VIDEO_DIRECTORIES}", videoChecker.videoDirectories.Aggregate((a,b) => $"{a}\n{b}"));

        videoChecker.videosAreOKChanged += (sender, videosAreOK) => updateButtons();
        videoChecker.isCheckingVideosChanged += (sender, isChecking) => updateButtons();

        startButton.onClick.AddListener(() =>
        {
            VideoCatalogue.UseDemoVideos = false;
            SceneManager.LoadSceneAsync("MainScene");
        });

        demoButton.onClick.AddListener(() =>
        {
            VideoCatalogue.UseDemoVideos = true;
            SceneManager.LoadSceneAsync("MainScene");
        });

        reloadVideosButton.onClick.AddListener(() =>
        {
            StartCoroutine(videoChecker.CheckVideos());
        });
        updateButtons();

    }

    private void updateButtons()
    {
        startButton.interactable = videoChecker.videosAreOK;
        reloadVideosButton.interactable = !videoChecker.isCheckingVideos;
    }

}
