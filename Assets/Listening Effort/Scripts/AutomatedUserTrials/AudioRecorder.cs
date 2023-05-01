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
    //private AudioClip recordedClip;
    private AudioSource audioSource;
    public string subfolder = "Recordings";
    //private string microphone;
    private int frequency;
    private bool hasMicrophone = false;

    private string saveDirectory => Path.Combine(Application.persistentDataPath, subfolder);
    private double recordingStartTime = -1f;
    public bool isRecording => recordingStartTime > 0.0f;
    private string recordingFilename;
    private Coroutine waitForRecordingCoroutine;

    public event EventHandler<string> recordingFinished;


    public void Start()
    {
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
        Debug.Log($"Started recording {lengthSec} seconds of audio...");
        recordingStartTime = AudioSettings.dspTime;
        waitForRecordingCoroutine = StartCoroutine(WaitForRecording(lengthSec));
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
        double length = Math.Max(AudioSettings.dspTime - recordingStartTime, (double) audioSource.clip.length);
        Debug.Log($"{length} seconds recorded");
        {
            int lengthSamples = (int)(length * audioSource.clip.frequency);
            AudioClip trimmedClip = AudioClip.Create(audioSource.clip.name, lengthSamples, audioSource.clip.channels, audioSource.clip.frequency, false);
            float[] samples = new float[lengthSamples * audioSource.clip.channels];
            audioSource.clip.GetData(samples, 0);
            trimmedClip.SetData(samples, 0);
            string savePath = Path.Combine(saveDirectory, recordingFilename);
            SaveAudio(trimmedClip, savePath);
            recordingFinished?.Invoke(this, savePath);
        }
        waitForRecordingCoroutine = null;
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
        Debug.Log($"Audio saved to {path}");
    }
}
