using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreparationDialog : MonoBehaviour
{
    public Text loadingText;
    public VideoChecker videoChecker;
    public Button startButton;
    public Button demoButton;
    public Button reloadVideosButton;
    public Toggle developerConsoleToggle;

    public Toggle reverbSmallToggle, reverbMediumToggle, reverbLargeToggle;

    public GameObject developerConsole;

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



        // reverb toggles
        {
            List<(Toggle toggle, string name)> reverbOptions = new List<(Toggle toggle, string name)>
                {
                    (reverbLargeToggle, "3DTI_BRIR_large"),
                    (reverbMediumToggle, "3DTI_BRIR_medium"),
                    (reverbSmallToggle, "3DTI_BRIR_small"),
                };
            string reverbModel = PlayerPrefs.GetString("reverbModel");
            if (!reverbOptions.Any(model => model.name == reverbModel))
            {
                Debug.LogWarning($"Previous reverb model '{PlayerPrefs.GetString("")}' not found.");
                PlayerPrefs.SetString("reverbModel", reverbOptions[0].name);
            }
            foreach ((Toggle toggle, string name) in reverbOptions)
            {
                if (PlayerPrefs.GetString("reverbModel") == name)
                {
                    toggle.SetIsOnWithoutNotify(true);
                }
                toggle.GetComponentInChildren<Text>().text = name;
                toggle.onValueChanged.AddListener(isOn => PlayerPrefs.SetString("reverbModel", name));
            }
        }


        developerConsoleToggle.isOn = developerConsole.activeSelf;
        developerConsoleToggle.onValueChanged.AddListener(toggle =>
        {
            developerConsole.SetActive(toggle);
        });
    }

    private void updateButtons()
    {
        startButton.interactable = videoChecker.videosAreOK;
        reloadVideosButton.interactable = !videoChecker.isCheckingVideos;
    }

}
