using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionController : MonoBehaviour
{
    Session session;

    private void Start()
    {
        session = Session.LoadFromYaml(Resources.Load<TextAsset>("SampleAutomatedSession").text);
        Debug.Log($"Loaded SampleAutomatedSession.yaml");
    }


}
