using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public OSCController oscController;
    public AutomaticSessionController automaticSessionController;
    public enum State
    {
        MainMenu,
        WaitingForOSCConnection,
        RunningOSC,
        RunningAutomatedSession,
        Error,
    }

    private State state_ = State.MainMenu;
    public State state
    {
        get => state_;
        private set
        {
            state_ = value;
            onStateChanged?.Invoke(this, state_);
        }
    }
    public event EventHandler<State> onStateChanged;

    public string errorMessage { get; private set; } = "";

    // Start is called before the first frame update
    void Start()
    {
        // should only be one Manager
        Debug.Assert(FindObjectOfType<Manager>() == this);

        // Later down the line the LoadVideo scene should be merged into this scene, in which case it will be shown with MainMenu.
        // For now, we're using PlayerPrefs to pass through the settings
        bool isAutomaticMode = PlayerPrefs.GetInt("automaticMode", 0) != 0;
        if (isAutomaticMode)
        {
            state = State.RunningAutomatedSession;
            string yamlFileNoExtension = PlayerPrefs.GetString("automaticModeSessionYAMLWithoutExtension", "");
            if (yamlFileNoExtension == "")
            {
                errorMessage = "Internal error reading session YAML filename";
                state = State.Error;
            }
            try
            {
                automaticSessionController.StartSession(yamlFileNoExtension);
            }
            catch (Exception e)
            {
                errorMessage = $"Error loading session YAML file\n{e}";
                state = State.Error;
            }
        }
        else
        {
            oscController.inOSCSessionMode = true;
            if (oscController.isClientConnected)
            {
                state = State.RunningOSC;
            }
            else
            {
                state = State.WaitingForOSCConnection;
            }
        }

        oscController.onClientConnected += (sender, client) =>
        {
            if (state == State.WaitingForOSCConnection)
            {
                state = State.RunningOSC;
            }
        };
    }

    // Update is called once per frame
    void Update()
    {

    }
}
