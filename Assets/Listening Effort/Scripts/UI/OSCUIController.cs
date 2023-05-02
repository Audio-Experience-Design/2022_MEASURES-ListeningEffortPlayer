using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OSCUIController : MonoBehaviour
{
    public GameObject oscUIPanel;

    // Start is called before the first frame update
    void Awake()
    {
        Manager manager = FindObjectOfType<Manager>();
        manager.onStateChanged += (sender, state) =>
        {
            oscUIPanel.SetActive(state == Manager.State.WaitingForOSCConnection);
        };
    }
}
