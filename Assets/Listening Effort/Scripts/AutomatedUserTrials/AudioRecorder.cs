using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.IO;
using System.Collections;
using System.Net;
using System;

public class AudioRecorder : MonoBehaviour
{
    private AudioClip recordedClip;
    public string subfolder = "Recordings";
    private string microphone;
    private int frequency;

    public event EventHandler<string> onRecordingFinished;


    public void Start()
    {
        Debug.Log($"Microphone devices: {Microphone.devices.Length}");
        foreach (string device in Microphone.devices)
            Debug.Log($"Device: {device}");
        if (Microphone.devices.Length > 0)
        {
            microphone = Microphone.devices[0];
            Microphone.GetDeviceCaps(microphone, out int minFreq, out int maxFreq);
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
        if (microphone == null)
        {
            Debug.LogError($"Can't Start Recording as no microphone was found.");
        }

        StopRecording();

        string path = Path.Combine(Application.persistentDataPath, subfolder);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        recordedClip = Microphone.Start(microphone, true, lengthSec, frequency);
        if (recordedClip == null)
        {
            Debug.LogError($"Failed to open default microphone for recording");
        }
        else
        {
            StartCoroutine(WaitForRecording(filename, path));
            Debug.Log($"Started recording {lengthSec} seconds of audio...");
        }
    }

    private IEnumerator WaitForRecording(string filename, string path)
    {
        while (Microphone.IsRecording(microphone))
            yield return null;

        int length = Microphone.GetPosition(microphone);
        Microphone.End(microphone);
        if (length == 0)
        {
            Debug.LogWarning($"No audio was able to be recorded");
        }
        else
        {
            AudioClip trimmedClip = AudioClip.Create(recordedClip.name, length, recordedClip.channels, recordedClip.frequency, false);
            float[] samples = new float[length * recordedClip.channels];
            recordedClip.GetData(samples, 0);
            trimmedClip.SetData(samples, 0);
            SaveAudio(trimmedClip, filename, path);
            onRecordingFinished?.Invoke(this, Path.Combine(path, filename));
        }
    }

    public void StopRecording()
    {
        if (Microphone.IsRecording(microphone))
            Microphone.End(microphone);
    }

    private void SaveAudio(AudioClip clip, string filename, string path)
    {
        FileStream fileStream = File.Create(Path.Combine(path, filename));
        byte[] audioBytes = WavUtility.FromAudioClip(clip);
        fileStream.Write(audioBytes, 0, audioBytes.Length);
        fileStream.Close();
        Debug.Log($"Audio saved to {Path.Combine(path, filename)}");
    }
}
