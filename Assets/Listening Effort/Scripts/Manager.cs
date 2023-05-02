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
        MainMenu, // initial state
        TestingVideos, // set by event from VideoChecker
        WaitingForOSCConnection, // set here on user request
        RunningOSC, // set here based on OscController state
        RunningAutomatedSession, // set here on user request
        Error, // set here if something goes wrong
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

    private VideoChecker videoChecker;

    void Start()
    {
        // should only be one Manager
        Debug.Assert(FindObjectOfType<Manager>() == this);

        videoChecker = FindObjectOfType<VideoChecker>();
        Debug.Assert(videoChecker != null);

        videoChecker.isCheckingVideosChanged += (sender, isCheckingVideos) =>
        {
            if (isCheckingVideos)
            {
                Debug.Assert(state == State.MainMenu);
                state = State.TestingVideos;
            }
            else
            {
                Debug.Assert(state == State.TestingVideos);
                state = State.MainMenu;
            }
        };

        oscController.onClientConnected += (sender, client) =>
        {
            if (state == State.WaitingForOSCConnection)
            {
                state = State.RunningOSC;
            }
        };
    }

    public void startAutomaticSession(string yamlFileNoExtension)
    {
        Debug.Assert(state == State.MainMenu);

        state = State.RunningAutomatedSession;
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

    public void startOSCSession()
    {
        Debug.Assert(state == State.MainMenu);

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

}
