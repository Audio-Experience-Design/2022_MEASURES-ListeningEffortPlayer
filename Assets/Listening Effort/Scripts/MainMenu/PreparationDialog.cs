using API_3DTI;
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

    public Toggle reverbSmallToggle, reverbMediumToggle, reverbLargeToggle, reverbCustomToggle;

    public GameObject developerConsole;

    private List<(Toggle toggle, string name)> reverbModels => new List<(Toggle toggle, string name)>
    {
        (reverbLargeToggle, "3DTI_BRIR_large"),
        (reverbMediumToggle, "3DTI_BRIR_medium"),
        (reverbSmallToggle, "3DTI_BRIR_small"),
        (reverbCustomToggle, SpatializerResourceChecker.customReverbModelName)
    };

    public Toggle[] hrtfIRCToggles;
    public Toggle[] hrtfLengthToggles;
    public Toggle[] hrtfCustomToggles;


    public void Awake()
    {

        loadingText.text = loadingText.text
            .Replace("{VIDEO_DIRECTORIES}", videoChecker.videoDirectories.Aggregate((a, b) => $"{a}\n{b}"))
            .Replace("{REVERB_DIRECTORY}", SpatializerResourceChecker.reverbModelDirectory)
            .Replace("{CUSTOM_REVERB_SUFFIX}", SpatializerResourceChecker.customReverbSuffix)
            ;

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


        developerConsoleToggle.isOn = developerConsole.activeSelf;
        developerConsoleToggle.onValueChanged.AddListener(toggle =>
        {
            developerConsole.SetActive(toggle);
        });


        // reverb toggles
        {
            // confirm names match with those in CustomSpatializerBinaryChecker
            Debug.Assert(reverbModels.Take(3).Select(model => model.name).All(name => SpatializerResourceChecker.defaultReverbModelNames.Contains(name)));

            // set up callbacks
            string reverbModel = PlayerPrefs.GetString("reverbModel");
            if (!reverbModels.Any(model => model.name == reverbModel))
            {
                Debug.LogWarning($"Previous reverb model '{PlayerPrefs.GetString("")}' not found.");
                PlayerPrefs.SetString("reverbModel", reverbModels[0].name);
            }
            foreach ((Toggle toggle, string name) in reverbModels)
            {
                if (PlayerPrefs.GetString("reverbModel") == name)
                {
                    toggle.SetIsOnWithoutNotify(true);
                }
                toggle.GetComponentInChildren<Text>().text = name;
                toggle.onValueChanged.AddListener(isOn => PlayerPrefs.SetString("reverbModel", name));
            }

            // Find custom reverb
            (string customReverbName, string customReverbPath) = SpatializerResourceChecker.findCustomReverb();
            Debug.Assert(customReverbName != null && customReverbPath != null);
            Debug.Assert((customReverbName == "") == (customReverbPath == ""));
            if (customReverbName == "")
            {
                if (reverbCustomToggle.isOn)
                {
                    reverbModels[0].toggle.isOn = true;
                }
                reverbCustomToggle.enabled = false;
                reverbCustomToggle.GetComponentInChildren<Text>().text = "None found";
            }
            else
            {
                reverbCustomToggle.GetComponentInChildren<Text>().text = customReverbName;
                reverbCustomToggle.enabled = true;
            }

            PlayerPrefs.Save();

        }


        // HRTF toggles
        {

        }

    }

    private void updateButtons()
    {
        startButton.interactable = videoChecker.videosAreOK;
        reloadVideosButton.interactable = !videoChecker.isCheckingVideos;
    }

 

}
