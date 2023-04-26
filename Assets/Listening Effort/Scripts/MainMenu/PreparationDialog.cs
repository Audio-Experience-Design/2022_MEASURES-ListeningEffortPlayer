using API_3DTI;
using System;
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
    public Text buildDateText;

    private List<(Toggle toggle, string name)> reverbModels => new List<(Toggle toggle, string name)>
    {
        (reverbLargeToggle, "3DTI_BRIR_large"),
        (reverbMediumToggle, "3DTI_BRIR_medium"),
        (reverbSmallToggle, "3DTI_BRIR_small"),
        (reverbCustomToggle, SpatializerResourceChecker.customReverbModelName)
    };

    public Toggle[] hrtfToggles;


    public void Start()
    {

        loadingText.text = loadingText.text
            .Replace("{VIDEO_DIRECTORIES}", videoChecker.videoDirectories.Aggregate((a, b) => $"{a}\n{b}"))
            .Replace("{REVERB_DIRECTORY}", SpatializerResourceChecker.reverbModelDirectory)
            .Replace("{CUSTOM_REVERB_SUFFIX}", SpatializerResourceChecker.customReverbSuffix)
            .Replace("{HRTF_DIRECTORY}", SpatializerResourceChecker.hrtfDirectory)
            .Replace("{HRTF_SUFFIX}", $"({string.Join(" | ", SpatializerResourceChecker.hrtfSuffixes)})")
            ;
        buildDateText.text = buildDateText.text.Replace("{BUILD_DATE}", BuildInfo.BUILD_TIME);

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
        if (!PlayerPrefs.HasKey("developerConsole"))
        {
            PlayerPrefs.SetInt("developerConsole", developerConsole.activeSelf? 1 : 0);
        }
        developerConsoleToggle.onValueChanged.AddListener(toggle =>
        {
            developerConsole.SetActive(toggle);
            PlayerPrefs.SetInt("developerConsole", toggle ? 1 : 0);
            PlayerPrefs.Save();
        });


        try
        // reverb toggles
        {
            Debug.Log("Checking Reverb BRIRs");
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
            Debug.Log("Searching for custom reverb file");
            (string customReverbName, string customReverbPath) = SpatializerResourceChecker.findCustomReverb();
            Debug.Assert(customReverbName != null && customReverbPath != null);
            Debug.Assert((customReverbName == "") == (customReverbPath == ""));
            if (customReverbName == "")
            {
                if (reverbCustomToggle.isOn)
                {
                    reverbModels[0].toggle.isOn = true;
                }
                reverbCustomToggle.gameObject.SetActive(false);
                reverbCustomToggle.GetComponentInChildren<Text>().text = "None found";
            }
            else
            {
                Debug.Log($"Found custom reverb file '{customReverbName}' at '{customReverbPath}'");
                reverbCustomToggle.GetComponentInChildren<Text>().text = customReverbName;
                reverbCustomToggle.enabled = true;
            }

            PlayerPrefs.Save();

        }
        catch (Exception e)
        {
            Debug.LogError($"Exception while setting up reverb toggles");
            Debug.LogException(e);
        }



        // HRTF toggles
        try
        {
            Debug.Log("Checking inbuilt and custom HRTFs");
            (string name, string filename, string path)[] hrtfs = SpatializerResourceChecker.getHRTFs();
            Debug.Log($"Found {hrtfs.Length} HRTFs.");
            if (hrtfs.Length > hrtfToggles.Length)
            {
                Debug.LogWarning($"More HRTFs have been found than there is space to display them.");
            }
            int N = Mathf.Min(hrtfs.Length, hrtfToggles.Length);
            for (int i = 0; i < N; i++)
            {
                hrtfToggles[i].enabled = true;
                hrtfToggles[i].GetComponentInChildren<Text>().text = hrtfs[i].name;
                string path = hrtfs[i].path;
                hrtfToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        PlayerPrefs.SetString("hrtf", path);
                    }
                });
                if (PlayerPrefs.GetString("hrtf", "") == path)
                {
                    hrtfToggles[i].isOn = true;
                }
            }
            for (int i = N; i < hrtfToggles.Length; i++)
            {
                hrtfToggles[i].gameObject.SetActive(false);
            }
            if (PlayerPrefs.GetString("hrtf", "") == "")
            {
                hrtfToggles[0].isOn = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception while setting up HRTF toggles");
            Debug.LogException(e);
        }

    }

    private void updateButtons()
    {
        startButton.interactable = videoChecker.videosAreOK;
        reloadVideosButton.interactable = !videoChecker.isCheckingVideos;
    }



}
