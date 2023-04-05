using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{
    public VideoDownloader videoDownloader;

    // Start is called before the first frame update
    void Start()
    {
        videoDownloader.IsReadyChanged += (sender, isReady) =>
        {
            GetComponent<Button>().interactable = isReady;
        };
        GetComponent<Button>().onClick.AddListener(() =>
        {
            SceneManager.LoadSceneAsync("MainScene");
        });
    }
}
