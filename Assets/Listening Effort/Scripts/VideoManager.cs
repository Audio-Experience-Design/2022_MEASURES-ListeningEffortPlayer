using System;
using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    //public string VideoPath;
    public string idleVideoName;
    public Material TargetMaterial;
    public bool IsIdleVideoPlaying
    {
        get
        {
            VideoPlayer player = GetComponent<VideoPlayer>();
            if (player.source == VideoSource.Url)
            {
                return player.url == idleVideoName;
            }
            else
            {
                return player.clip.name == idleVideoName || player.clip.name == idleVideoName + ".mp4";
            }
        }
    }

    private RenderTexture renderTexture;

    private MeshRenderer meshRenderer;

    private VideoCatalogue videoCatalogue;
    public VideoPlayer player;
    public AudioSource audioSource => player.GetComponentInChildren<AudioSource>();

    public event EventHandler playbackFinished;

    void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        videoCatalogue = FindAnyObjectByType<VideoCatalogue>();
        if (videoCatalogue == null)
        {
            throw new System.Exception("Failed to find VideoCatalogue instances");
        }

        player = GetComponent<VideoPlayer>();
        Debug.Assert(player != null);

        player.prepareCompleted += (source) =>
        {
            if (player.targetTexture != null && (player.targetTexture.width != player.width || player.targetTexture.height != player.height))
            {
                Debug.Log("Destroying render texture");
                Debug.Assert(player.targetTexture == renderTexture);
                renderTexture.Release();
                player.targetTexture = null;
            }
            if (player.targetTexture == null)
            {
                Debug.Log("Creating render texture");
                renderTexture = new RenderTexture((int)player.width, (int)player.height, 0);
                player.targetTexture = renderTexture;

                TargetMaterial.mainTexture = renderTexture;
                if (meshRenderer != null)
                {
                    GetComponentInChildren<MeshRenderer>().material = TargetMaterial;
                }
            }
            player.Play();
        };

        player.started += (source) =>
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = true;
            }
        };

        player.loopPointReached += (source) =>
        {
            // if non-looping video then return to idle video if there is one.
            // otherwise hide the mesh.
            if (!player.isLooping)
            {
                player.Stop();
                playbackFinished?.Invoke(this, new EventArgs());
                bool isIdleVideoPlaying = StartIdleVideo();
                if (!isIdleVideoPlaying && meshRenderer != null)
                {
                    meshRenderer.enabled = false;
                }
            }
            
        };

        if (player.url != "")
        {
            Debug.Log("Preparing player");
            player.Prepare();
        }

    }

    public bool StartIdleVideo()
    {
        if (!videoCatalogue.Contains(idleVideoName))
        {
            Debug.Log("Cannot start idle video as non has been set.", this);
            return false;
        }
        videoCatalogue.SetPlayerSource(player, idleVideoName);
        player.isLooping = true;
        player.Prepare();
        return true;
    }

    /// <summary>
    ///  
    /// </summary>
    /// <param name="name">Refers to a name in the VideoCatalogue</param>
    public void PlayVideo(string videoName)
    {
        player.Stop();
        videoCatalogue.SetPlayerSource(player, videoName);
        player.SetTargetAudioSource(0, audioSource);
        player.isLooping = false;
        player.Prepare();
        // player will play automatically due to onPrepared callback
        Debug.Assert(audioSource == player.GetTargetAudioSource(0));
        Debug.Assert(audioSource.spatialize == true);
    }

    public void StopVideo()
    {
        player.Stop();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            Debug.Log("Destroying render texture");
            renderTexture.Release();
        }
    }


}
