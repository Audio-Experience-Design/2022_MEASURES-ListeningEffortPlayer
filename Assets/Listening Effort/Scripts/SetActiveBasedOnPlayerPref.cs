using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetActiveBasedOnPlayerPref : MonoBehaviour
{
    public string playerPrefKey;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey(playerPrefKey))
        {
            gameObject.SetActive(PlayerPrefs.GetInt(playerPrefKey) != 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
