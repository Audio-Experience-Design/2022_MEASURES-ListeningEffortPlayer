using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
	public string VideoPath;
	public string IdleVideoPath;
	public Material TargetMaterial;
	public bool IsIdleVideoPlaying
	{
		get { return GetComponent<VideoPlayer>()?.url == IdleVideoPath; }
	}

	private RenderTexture renderTexture;

	private MeshRenderer meshRenderer;

	void Awake()
	{
		meshRenderer = GetComponentInChildren<MeshRenderer>();

		VideoPlayer player = GetComponent<VideoPlayer>();
		if (VideoPath != "")
		{
			player.url = VideoPath;
		}

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
		if (string.IsNullOrEmpty(IdleVideoPath))
		{
			Debug.Log("Cannot start idle video as non has been set.", this);
			return false;
		}
		else
		{
			VideoPlayer player = GetComponent<VideoPlayer>();
			player.url = IdleVideoPath;
			player.isLooping = true;
			player.Prepare();
			return true;
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

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}
}
