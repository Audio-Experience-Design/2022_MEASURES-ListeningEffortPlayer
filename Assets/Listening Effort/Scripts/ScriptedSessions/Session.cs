using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using UnityEditor;
//using UnityEngine.Windows;

public class Session
{
    public string Name { get; private set; }
    public float BrightnessCalibrationDurationFromBlackToWhite { get; private set; }
    public float BrightnessCalibrationDurationToHoldOnWhite { get; private set; }
    public float SpeakerAmplitude { get; private set; }
    public float DelayBeforePlayingVideos { get; private set; }
    public float DelayAfterPlayingVideos { get; private set; }
    public float RecordingDuration { get; private set; }
    public string MaskingVideo { get; private set; }
    public bool PlayMaskersContinuously { get; private set; }
    public class Masker
    {
        public float Rotation;
        public float Amplitude;
    }
    public List<Masker> Maskers { get; private set; }
    public class VideoScreen
    {
        public float Inclination;
        public float Azimuth;
        public float Twist;
        public float RotationOnXAxis;
        public float RotationOnYAxis;
        public float ScaleWidth;
        public float ScaleHeight;
        public string IdleVideo;
    }
    public List<VideoScreen> VideoScreens { get; private set; }
    public List<List<string>> Challenges { get; private set; }
    public Dictionary<string, string> UserInterfaceTexts { get; private set; }
    public string yaml { get; private set; }

    public static Session LoadFromYamlPath(string yamlPath, VideoCatalogue videoCatalogue)
    {
        string yamlText = File.ReadAllText(yamlPath);
        Session session = LoadFromYamlString(yamlText, videoCatalogue);
        if (session.Name == null)
        {
            session.Name = Path.GetFileNameWithoutExtension(yamlPath);
        }
        return session;
    }

    private static Session LoadFromYamlString(string yamlText, VideoCatalogue videoCatalogue)
    {
        try
        {
            var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            Session session = deserializer.Deserialize<Session>(yamlText); /* compile error */

            foreach (Masker masker in session.Maskers)
            {
                if (masker.Amplitude < 0.0f || 1.0f < masker.Amplitude)
                {
                    throw new Exception("Masker amplitude must be between 0.0 and 1.0.");
                }
            }
            if (session.Maskers.Count != 4)
            {
                throw new Exception("The 'maskers' array must have exactly 4 elements.");
            }
            if (session.VideoScreens.Count != 3)
            {
                throw new Exception("The 'idle videos' array must have exactly 3 elements.");
            }

            foreach (List<string> challenge in session.Challenges)
            {
                if (challenge.Count != 3)
                {
                    throw new Exception("The nested members of 'challenges' must have arrays with exactly 3 elements.");
                }
            }
            if (session.Challenges.Count == 0)
            {
                throw new Exception("There must be at least one challenge.");
            }

            var videosMissingFromCatalogue = session.Challenges
                .SelectMany(subList => subList)
                .Concat(session.VideoScreens.Select(screen => screen.IdleVideo))
                .Append(session.MaskingVideo)
                .Where(video => !videoCatalogue.Contains(video));
            if (videosMissingFromCatalogue.Count() > 0)
            {
                videoCatalogue.LogVideoNames();
                throw new Exception($"The following videos are missing from the catalogue: {string.Join(", ", videosMissingFromCatalogue)}");
            }

            Debug.Assert(session.Invariant());

            return session;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception while loading YAML. Will rethrow. Message: {ex.Message}");
            throw ex;
        }
    }

    public bool Invariant()
    {
        if (SpeakerAmplitude <= 0 || string.IsNullOrEmpty(MaskingVideo) || Maskers == null || VideoScreens == null || Challenges == null)
        {
            return false;
        }

        if (Maskers.Count != 4)
        {
            return false;
        }

        if (VideoScreens.Count != 3)
        {
            return false;
        }

        if (Challenges.Count == 0)
        {
            return false;
        }
        foreach (var challenge in Challenges)
        {
            if (challenge.Count != 3)
            {
                return false;
            }

            foreach (var video in challenge)
            {
                if (string.IsNullOrEmpty(video))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

