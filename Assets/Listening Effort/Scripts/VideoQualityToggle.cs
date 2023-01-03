using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoQualityToggle : MonoBehaviour
{
    public VideoPlayer videoPlayerHD;
    public VideoPlayer videoPlayer4K;
    public Material videoMaterial;
    public string targetTextureNameInShader;

    // Start is called before the first frame update
    void Start()
    {
        Toggle toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(delegate
        {
            if (toggle.isOn)
            {
                videoPlayerHD.Stop();
                videoPlayer4K.Play();
                videoMaterial.SetTexture(targetTextureNameInShader, videoPlayer4K.targetTexture);
                Debug.Log("Set video player 1 to 4K quality", this);
            }
            else
            {
                videoPlayer4K.Stop();
                videoPlayerHD.Play();
                videoMaterial.SetTexture(targetTextureNameInShader, videoPlayerHD.targetTexture);
                Debug.Log("Set video player 1 to HD quality", this);

            }
        });
        toggle.onValueChanged.Invoke(toggle.isOn);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("Fire1 button");
        }
    }
}
