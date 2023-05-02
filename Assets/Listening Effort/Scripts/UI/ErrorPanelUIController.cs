using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ErrorPanelUIController : MonoBehaviour
{
    private Manager manager;
    public GameObject errorPanel;
    public Text errorText;

    private void Awake()
    {
        manager = FindObjectOfType<Manager>();
        manager.onStateChanged += (sender, state) =>
        {
            if (state == Manager.State.Error)
            {
                errorText.text = manager.errorMessage;
            }
            errorPanel.SetActive(state == Manager.State.Error);
        };
    }
}
