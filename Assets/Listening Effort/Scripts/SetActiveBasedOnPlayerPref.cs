using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetActiveBasedOnPlayerPref : MonoBehaviour
{
    public string playerPrefKey;
    public bool setActiveIfKeyTrue = true;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey(playerPrefKey))
        {
            bool keyValue = PlayerPrefs.GetInt(playerPrefKey) != 0;
            gameObject.SetActive(setActiveIfKeyTrue? keyValue : !keyValue);
        }
        else
        {
            Debug.LogWarning("PlayerPrefKey " + playerPrefKey + " not found", this);
        }
    }

}
