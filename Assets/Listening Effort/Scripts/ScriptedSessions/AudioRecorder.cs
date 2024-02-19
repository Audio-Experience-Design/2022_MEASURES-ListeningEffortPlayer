using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.IO;
using System.Collections;
using System.Net;
using System;

[RequireComponent(typeof(AudioSource))]
public class AudioRecorder : MonoBehaviour
{
    private AudioSource audioSource;
    private int frequency;
    private bool hasMicrophone = false;

    public string saveDirectory = null;
    private double recordingStartTime = -1f;
    public bool isRecording => recordingStartTime > 0.0f;
    private string recordingFilename;
    private Coroutine waitForRecordingCoroutine;
    private double durationToTrimFromBeginning = 0.0;

    public event EventHandler<(string path, AudioClip clip)> recordingFinished;


    public void Start()
    {
        if (saveDirectory == null || saveDirectory == "")
        {
            saveDirectory = Path.Combine(Application.persistentDataPath, "Recordings");
        }
        audioSource = GetComponent<AudioSource>();
        Debug.Log($"Microphone devices: {Microphone.devices.Length}");
        foreach (string device in Microphone.devices)
            Debug.Log($"Device: {device}");
        if (Microphone.devices.Length > 0)
        {
            //microphone = Microphone.devices[0];
            hasMicrophone = true;
            Microphone.GetDeviceCaps(null, out int minFreq, out int maxFreq);
            Debug.Log($"Using microphone: {Microphone.devices[0]}. Min freq {minFreq}, Max freq {maxFreq}");
            frequency = (maxFreq == 0 && minFreq == 0) ? 44100
                : maxFreq >= 48000 ? 48000
                : maxFreq >= 44100 ? 44100
                : maxFreq >= 22050 ? 22050
                : maxFreq >= 16000 ? 16000
                : minFreq;
        }
    }

    public void StartRecording(string filename, int lengthSec)
    {
        Debug.Assert(filename != null && filename != "");
        if (!hasMicrophone)
        {
            Debug.LogError($"Can't Start Recording as no microphone was found.");
        }
        if (!Directory.Exists(saveDirectory))
            Directory.CreateDirectory(saveDirectory);

        StopRecording();
        recordingFilename = filename;
        audioSource.clip = Microphone.Start(null, true, lengthSec, frequency);
        recordingStartTime = AudioSettings.dspTime;
        waitForRecordingCoroutine = StartCoroutine(WaitForRecording(lengthSec));
    }

    // Because the Pico loses the first few seconds of recording, we start early.
    // This lets you mark when the saved part of the recording should begin
    // Can only be called while isRecording is true
    public void MarkRecordingInPoint()
    {
        Debug.Assert(isRecording);
        durationToTrimFromBeginning = AudioSettings.dspTime - recordingStartTime;
    }

    private IEnumerator WaitForRecording(double lengthSec)
    {
        while (isRecording && AudioSettings.dspTime - recordingStartTime < lengthSec)
            yield return null;
        if (isRecording)
        {
            Debug.Log($"Recording ending by timeout after {lengthSec}");
            CloseRecording();
        }
    }

    private void CloseRecording()
    {
        Microphone.End(null);
        // removes extra from end
        double outPoint = Math.Max(AudioSettings.dspTime - recordingStartTime, 0.0);
        int endpointSamples = (int)(outPoint * audioSource.clip.frequency);
        double inPoint = Math.Min(durationToTrimFromBeginning, outPoint);
        int inPointSamples = (int)(inPoint * audioSource.clip.frequency);
        // also removes extra from beginning
        int trimmedLengthSamples = endpointSamples - inPointSamples;
        AudioClip trimmedClip = AudioClip.Create(audioSource.clip.name, trimmedLengthSamples, audioSource.clip.channels, audioSource.clip.frequency, false);
        float[] trimmedSamples = new float[trimmedLengthSamples * audioSource.clip.channels];
        audioSource.clip.GetData(trimmedSamples, inPointSamples);
        trimmedClip.SetData(trimmedSamples, 0);
        string savePath = Path.Combine(saveDirectory, recordingFilename);
        SaveAudio(trimmedClip, savePath);
        Debug.Log($"{trimmedClip.length} seconds recorded and saved to {savePath}. Inpoint: {inPoint}. OutPoint: {outPoint}");
        recordingFinished?.Invoke(this, (savePath, trimmedClip));

        recordingStartTime = -2.0f;
        waitForRecordingCoroutine = null;
        durationToTrimFromBeginning = 0.0;
    }

    /// Warning: The saved file will still be of the requested length, but the remainder will be silence.
    public void StopRecording()
    {
        Debug.Assert(isRecording == (waitForRecordingCoroutine != null));
        if (isRecording)
        {
            Debug.Log($"Recording ending by StopRecording call.");
            StopCoroutine(waitForRecordingCoroutine);
            CloseRecording();
        }
    }

    private void SaveAudio(AudioClip clip, string path)
    {
        FileStream fileStream = File.Create(path);
        byte[] audioBytes = WavUtility.FromAudioClip(clip);
        fileStream.Write(audioBytes, 0, audioBytes.Length);
        fileStream.Close();
    }
}
