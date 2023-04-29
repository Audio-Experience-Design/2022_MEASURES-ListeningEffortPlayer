using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SessionController : MonoBehaviour
{
    Session session;

    public VideoCatalogue videoCatalogue;

    public VideoPlayer skyboxVideoPlayer;
    public VideoManager[] videoManagers;
    public GameObject[] babblePrefabs;

    public Text statusText;
    public Button startButton;
    public Button recordResponseButton;
    public Button continueButton;


    private void Start()
    {
        string yamlText = Resources.Load<TextAsset>("SampleAutomatedSession").text;
        session = Session.LoadFromYaml(yamlText, videoCatalogue);
        Debug.Log($"Loaded SampleAutomatedSession.yaml");
    }

    public IEnumerator StartSession()
    {
        Debug.Log($"Starting automated trial session");
        
        videoManagers.ToList().ForEach(vm => vm.audioSource.volume = session.SpeakerAmplitude);

        videoCatalogue.SetPlayerSource(skyboxVideoPlayer, session.MaskingVideo);
        skyboxVideoPlayer.Play();
        
        if (session.Maskers.Count() > babblePrefabs.Count())
        {
            throw new System.Exception($"There are {session.Maskers.Count()} maskers defined in YAML but only {babblePrefabs.Count()} babble sources available.");
        }
        
        for (int i=0; i<session.Maskers.Count(); i++)
        {
            babblePrefabs[i].GetComponent<AudioSource>().volume = session.Maskers[i].Amplitude;
            babblePrefabs[i].transform.localRotation = Quaternion.Euler(0, session.Maskers[i].Rotation, 0);
        }

        //for (int i=0; i<)
        // TODO: Finish
        yield break;
    }
}
