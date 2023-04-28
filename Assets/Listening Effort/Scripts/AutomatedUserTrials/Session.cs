using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UIElements;
using YamlDotNet.RepresentationModel;

public class Session
{
    public float SpeakerAmplitude { get; private set; }
    public string MaskingVideo { get; private set; }
    public List<(float Position, float Amplitude)> Maskers { get; private set; }
    public List<string> IdleVideos { get; private set; }
    public List<List<string>> Challenges { get; private set; }

    public static Session LoadFromYaml(string yamlText)
    {
        try
        {
            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(yamlText));

            YamlMappingNode rootNode = (YamlMappingNode)yamlStream.Documents[0].RootNode;
            YamlMappingNode sessionNode = (YamlMappingNode)rootNode.Children[new YamlScalarNode("session")];

            Session session = new Session
            {
                SpeakerAmplitude = Convert.ToSingle(sessionNode.Children[new YamlScalarNode("speakerAmplitude")].ToString()),
                MaskingVideo = sessionNode.Children[new YamlScalarNode("masking_video")].ToString(),
                Maskers = new List<(float Position, float Amplitude)>(),
                IdleVideos = new List<string>(),
                Challenges = new List<List<string>>()
            };

            var maskersNode = (YamlSequenceNode)sessionNode.Children[new YamlScalarNode("maskers")];
            foreach (YamlMappingNode maskerNode in maskersNode)
            {
                (float Position, float Amplitude) masker = (
                    Convert.ToSingle(maskerNode.Children[new YamlScalarNode("position")].ToString()),
                    Convert.ToSingle(maskerNode.Children[new YamlScalarNode("amplitude")].ToString())
                );
                if (masker.Amplitude < 0.0f || 1.0f < masker.Amplitude)
                {
                    throw new Exception("Masker amplitude must be between 0.0 and 1.0.");
                }

                session.Maskers.Add(masker);
            }

            if (session.Maskers.Count != 4)
            {
                throw new Exception("The 'maskers' array must have exactly 4 elements.");
            }

            var idleVideosNode = (YamlSequenceNode)sessionNode.Children[new YamlScalarNode("idle_videos")];
            foreach (YamlScalarNode idleVideoNode in idleVideosNode)
            {
                session.IdleVideos.Add(idleVideoNode.ToString());
            }
            if (session.IdleVideos.Count != 3)
            {
                throw new Exception("The 'idle_videos' array must have exactly 3 elements.");
            }

            var challengesNode = (YamlSequenceNode)sessionNode.Children[new YamlScalarNode("challenges")];
            foreach (YamlSequenceNode challengeNode in challengesNode)
            {
                List<string> challenge = new List<string>();
                foreach (YamlScalarNode videoNode in challengeNode)
                {
                    challenge.Add(videoNode.ToString());
                }
                session.Challenges.Add(challenge);

                if (challenge.Count != 3)
                {
                    throw new Exception("The nested members of 'challenges' must have arrays with exactly 3 elements.");
                }
            }
            if (session.Challenges.Count == 0)
            {
                throw new Exception("There must be at least one challenge.");
            }

            return session;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading YAML: {ex.Message}");
        }
    }

    public bool IsValid()
    {
        if (SpeakerAmplitude <= 0 || string.IsNullOrEmpty(MaskingVideo) || Maskers == null || IdleVideos == null || Challenges == null)
        {
            return false;
        }

        if (Maskers.Count != 4)
        {
            return false;
        }

        if (IdleVideos.Count != 3)
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

