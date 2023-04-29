using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Video;
using UnityOSC;

public class VideoManager : MonoBehaviour
{
	//public string VideoPath;
	[HideInInspector]
	public string IdleVideoURL;
	[HideInInspector]
    public VideoClip IdleVideoClip;
	public Material TargetMaterial;
	public bool IsIdleVideoPlaying
	{
		get {
			VideoPlayer player = GetComponent<VideoPlayer>();
			if (player.source == VideoSource.Url)
			{
				return player.url == IdleVideoURL;
			}
			else
			{
				return player.clip == IdleVideoClip;
			}
		}
	}

	private RenderTexture renderTexture;

	private MeshRenderer meshRenderer;

	private VideoCatalogue videoCatalogue;
	private VideoPlayer player;

	void Awake()
	{
		meshRenderer = GetComponentInChildren<MeshRenderer>();
		videoCatalogue = FindAnyObjectByType<VideoCatalogue>();
		if (videoCatalogue == null)
		{
			throw new System.Exception("Failed to find VideoCatalogue instances");
		}

		player = GetComponent<VideoPlayer>();
		//if (VideoPath != "")
		//{
		//	player.url = VideoPath;
		//}

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
				//bool isIdleVideoPlaying = StartIdleVideo();
				//if (!isIdleVideoPlaying && meshRenderer != null)
				//{
				//	meshRenderer.enabled = false;
				//}
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
        if (IdleVideoClip != null)
		{
            player.clip = IdleVideoClip;
            player.source = VideoSource.VideoClip;
            player.isLooping = true;
            player.Prepare();
            return true;
        }
		else if (!string.IsNullOrEmpty(IdleVideoURL))
		{
            player.url = IdleVideoURL;
            player.source = VideoSource.Url;
            player.isLooping = true;
            player.Prepare();
            return true;
        }
		else
		{
			Debug.Log("Cannot start idle video as non has been set.", this);
			return false;
		}
	}

	/// <summary>
	///  
	/// </summary>
	/// <param name="name">Refers to a name in the VideoCatalogue</param>
	public void PlayVideo(string videoName)
	{
        player.Stop();
		videoCatalogue.SetPlayerSource(player, videoName);
        player.SetTargetAudioSource(0, player.GetComponentInChildren<AudioSource>());
        player.isLooping = false;
        player.Prepare();
        // player will play automatically due to onPrepared callback
        Debug.Assert(player.GetComponentInChildren<AudioSource>() == player.GetTargetAudioSource(0));
        Debug.Assert(player.GetComponentInChildren<AudioSource>().spatialize == true);
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
