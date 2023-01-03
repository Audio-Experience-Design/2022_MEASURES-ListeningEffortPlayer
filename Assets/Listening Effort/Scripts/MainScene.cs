using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class MainScene : MonoBehaviour
{
    void Awake()
    {
        // Limit camera movement to just rotation rather than position (because we're using a mono 360 video)
        UnityEngine.XR.InputTracking.disablePositionalTracking = true;
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
