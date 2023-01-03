using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{
    private VideoDownloader[] uiElementsToCheck;

    // Start is called before the first frame update
    void Start()
    {
        uiElementsToCheck = FindObjectsOfType<VideoDownloader>();
        Debug.Log($"{uiElementsToCheck.Length} ui elements to check", this);
        foreach (var elem in uiElementsToCheck)
        {
            elem.IsReadyChanged += (isReady, sender) =>
            {
                updateEnabled();
            };
        }
        GetComponent<Button>().onClick.AddListener(() =>
        {
            SceneManager.LoadSceneAsync("MainScene");
        });
    }

    private void updateEnabled()
    {
        GetComponent<Button>().interactable = Array.TrueForAll(uiElementsToCheck, elem => elem.IsReady);
    }
}
