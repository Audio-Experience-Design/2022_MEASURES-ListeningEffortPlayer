using API_3DTI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreparationDialog : MonoBehaviour
{
    Spatializer spatializer;
    ScriptFileManager scriptedSessionFileManager;
    Manager manager;

    public Text[] textsForSubstitution;
    public VideoChecker videoChecker;
    public Button startOSCButton;
    public Button reloadVideosButton;
    public Button startScriptedSessionButton;
    public Toggle developerConsoleToggle;

    public GameObject developerConsole;

    public Dropdown hrtfDropdown;
    public Dropdown reverbDropdown;
    public Dropdown scriptedSessionDropdown;

    public Text loadScriptStatus;

    private bool selectedScriptTestedOK = false;
    private string GetOSCAddresses()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Where(ip => !(ip.ToString() == "127.0.0.1"))
            .Select(ip => ip.ToString())
            .Select(ipAddresses => $"{ipAddresses}:{OSCController.listenPort}")
            .Aggregate((head, tail) => $"{head}, {tail}");
        
    }

    public void Start()
    {
        spatializer = FindObjectOfType<Spatializer>();
        Debug.Assert(spatializer != null);
        scriptedSessionFileManager = FindObjectOfType<ScriptFileManager>();
        Debug.Assert(scriptedSessionFileManager != null);
        manager = FindObjectOfType<Manager>();
        Debug.Assert(manager != null);

        manager.onStateChanged += (sender, state) => gameObject.SetActive(state == Manager.State.MainMenu || state == Manager.State.TestingVideos);

        Array.ForEach(textsForSubstitution, text => text.text = text.text
            .Replace("{VIDEO_DIRECTORIES}", videoChecker.videoDirectories.Aggregate((a, b) => $"{a}\n{b}"))
            .Replace("{REVERB_DIRECTORY}", SpatializerResourceChecker.reverbModelDirectory)
            .Replace("{REVERB_SUFFIXES}", string.Join(" | ", SpatializerResourceChecker.reverbSuffixes))
            .Replace("{HRTF_DIRECTORY}", SpatializerResourceChecker.hrtfDirectory)
            .Replace("{HRTF_SUFFIXES}", string.Join(" | ", SpatializerResourceChecker.hrtfSuffixes))
            .Replace("{SCRIPT_DIRECTORY}", scriptedSessionFileManager.scriptsDirectory)
            .Replace("{BUILD_DATE}", BuildInfo.BUILD_TIME)
            .Replace("{IP_ADDRESS}", GetOSCAddresses())
            );

        videoChecker.videosAreOKChanged += (sender, videosAreOK) => updateButtons();
        videoChecker.isCheckingVideosChanged += (sender, isChecking) => updateButtons();

        startOSCButton.onClick.AddListener(() =>
        {
            manager.startOSCSession();
        });


        reloadVideosButton.onClick.AddListener(() =>
        {
            videoChecker.StartCheckingVideos();
        });


        developerConsole.SetActive(PlayerPrefs.GetInt("developerConsole", 0) != 0);
        developerConsoleToggle.isOn = developerConsole.activeSelf;
        if (!PlayerPrefs.HasKey("developerConsole"))
        {
            PlayerPrefs.SetInt("developerConsole", developerConsole.activeSelf ? 1 : 0);
        }
        developerConsoleToggle.onValueChanged.AddListener(toggle =>
        {
            developerConsole.SetActive(toggle);
            PlayerPrefs.SetInt("developerConsole", toggle ? 1 : 0);
            PlayerPrefs.Save();
        });


        // REVERB
        try
        {
            Debug.Log("Checking Reverb BRIRs");

            reverbDropdown.ClearOptions();
            var reverbs = SpatializerResourceChecker.getReverbs();
            Debug.Assert(reverbs.Length > 0);
            reverbDropdown.AddOptions(reverbs.Select((reverb, i) => reverb.name).ToList());

            string savedReverbModelPath = PlayerPrefs.GetString("reverbModel", "");

            if (!reverbs.Any(reverb => reverb.path == savedReverbModelPath))
            {
                Debug.LogWarning($"Previous reverb model not found: '{savedReverbModelPath}'.");
                PlayerPrefs.SetString("reverbModel", reverbs[0].path);
            }

            reverbDropdown.SetValueWithoutNotify(Array.FindIndex(reverbs, reverb => reverb.path == PlayerPrefs.GetString("reverbModel")));
            
            reverbDropdown.onValueChanged.AddListener(index =>
            {
                spatializer.GetSampleRate(out TSampleRateEnum sampleRate);
                Debug.Log($"Setting Reverb BRIR to {reverbs[index].filename}. (Current sample rate: {sampleRate})");
                spatializer.SetBinaryResourcePath(BinaryResourceRole.ReverbBRIR, sampleRate, reverbs[index].path);

                PlayerPrefs.SetString("reverbModel", reverbs[index].path);
                PlayerPrefs.Save();
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception while setting up reverb UI");
            Debug.LogException(e);
        }



        // HRTF
        try
        {
            Debug.Log("Checking inbuilt and custom HRTFs");
            var hrtfs = SpatializerResourceChecker.getHRTFs();
            Debug.Log($"Found {hrtfs.Length} HRTFs.");
            hrtfDropdown.ClearOptions();
            hrtfDropdown.AddOptions(hrtfs.Select(hrtf => hrtf.name).ToList());
            
            string savedHRTF = PlayerPrefs.GetString("hrtf", "");
            
            if (!hrtfs.Any(hrtf => hrtf.path == savedHRTF))
            {
                Debug.LogWarning($"Previous HRTF model '{savedHRTF}' not found.");
                PlayerPrefs.SetString("hrtf", hrtfs[0].path);
            }

            hrtfDropdown.SetValueWithoutNotify(Array.FindIndex(hrtfs, hrtf => hrtf.path == savedHRTF));

            hrtfDropdown.onValueChanged.AddListener(index =>
            {
                spatializer.GetSampleRate(out TSampleRateEnum sampleRate);
                Debug.Log($"Setting HRTF to {hrtfs[index].filename}. (Current sample rate: {sampleRate})");
                spatializer.SetBinaryResourcePath(BinaryResourceRole.HighQualityHRTF, sampleRate, hrtfs[index].path);

                PlayerPrefs.SetString("hrtf", hrtfs[index].path);
                PlayerPrefs.Save();
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception while setting up HRTF UI");
            Debug.LogException(e);
        }

        // SCRIPTS
        try
        {
            string[] scripts = scriptedSessionFileManager.scripts;
            scriptedSessionDropdown.ClearOptions();
            scriptedSessionDropdown.AddOptions(new List<string> { "Select a script" });
            scriptedSessionDropdown.AddOptions(scripts.Select(s => Path.GetFileName(s)).ToList());
            scriptedSessionDropdown.SetValueWithoutNotify(0);

            

            scriptedSessionDropdown.onValueChanged.AddListener(listboxIndex =>
            {
                selectedScriptTestedOK = false;
                updateButtons();
                if (listboxIndex == 0)
                {
                    loadScriptStatus.text = $"No script selected";
                }
                else
                {
                    int scriptIndex = listboxIndex - 1;
                    try
                    {
                        Session.LoadFromYamlPath(scripts[scriptIndex], videoChecker.videoCatalogue);
                        loadScriptStatus.text = $"{Path.GetFileName(scripts[scriptIndex])} loaded successfully";
                        selectedScriptTestedOK = true;
                        updateButtons();
                    }
                    catch (Exception e)
                    {
                        loadScriptStatus.text = $"Error loading {scripts[scriptIndex]}: {e.Message}";
                    }

                }
            });

            startScriptedSessionButton.onClick.AddListener(() =>
            {
                int scriptIndex = scriptedSessionDropdown.value - 1;
                Debug.Assert(0 <= scriptIndex && scriptIndex < scripts.Length);
                manager.startAutomaticSession(scripts[scriptIndex]); ;
            });

        }
        catch (Exception e)
        {
            Debug.LogError($"Exception while setting up Script selection UI: {e}");
        }

        updateButtons();

    }

    private void updateButtons()
    {
        startOSCButton.interactable = videoChecker.videosAreOK;
        reloadVideosButton.interactable = !videoChecker.isCheckingVideos;
        // Selectable[] selectablesIfVideosOK = new Selectable[] {
        //     startScriptedSessionButton,
        // };
        // foreach (var selectable in selectablesIfVideosOK)
        // {
        //     selectable.interactable = videoChecker.videosAreOK && !videoChecker.isCheckingVideos;
        // }
        scriptedSessionDropdown.interactable = !videoChecker.isCheckingVideos;
        startScriptedSessionButton.interactable = selectedScriptTestedOK && !videoChecker.isCheckingVideos;

    }



}
