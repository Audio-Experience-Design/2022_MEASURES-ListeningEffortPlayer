using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OSCUIController : MonoBehaviour
{
    public GameObject oscUIPanel;
    public Text oscUIText;
    public OSCController oscController;

    // Start is called before the first frame update
    void Start()
    {
        bool isAutomaticMode = PlayerPrefs.GetInt("automaticMode", 0) != 0;
        oscUIPanel.SetActive(!isAutomaticMode && !oscController.isClientConnected);

        oscController.onClientConnected += (sender, client) =>
        {
            oscUIText.text = $"Connection received from {client.ip}:{client.port}.";
            StartCoroutine(hideUIAfterDelay());
        };
    }

    private IEnumerator hideUIAfterDelay()  
    {
        yield return new WaitForSeconds(1);
        oscUIPanel.SetActive(false);
    }
}
