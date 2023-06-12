using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ScriptFileManager : MonoBehaviour
{
    public string scriptsDirectory => $"{Application.persistentDataPath}/Scripts";
    public TextAsset demoScript;

    private string[] _scripts = null;
    public string[] scripts
    {
        get
        {
            if (_scripts == null)
            {
                VideoCatalogue videoCatalogue = FindObjectOfType<VideoCatalogue>();
                Debug.Assert(videoCatalogue != null);

                //if (!Directory.Exists(scriptsDirectory))
                //{
                    //Debug.Log("Scripts directory does not exist. Will create and copy in the demo script.");
                    Directory.CreateDirectory(scriptsDirectory);
                    File.WriteAllText($"{scriptsDirectory}/demo.yaml", demoScript.text);
                //}

                List<string> scriptPaths = new List<string>();
                foreach (string path in Directory.GetFiles(scriptsDirectory))
                {
                    if (path.EndsWith(".yaml") || path.EndsWith(".yml"))
                    {
                        //try
                        //{
                        //    Session session = Session.LoadFromYamlPath(path, videoCatalogue);
                        //}
                        //catch (Exception e)
                        //{
                        //    Debug.LogError($"Error reading session at {path}\n{e}", this);
                        //}
                        scriptPaths.Add(path);
                    }
                }
                _scripts = scriptPaths.ToArray();
            }
            return _scripts;
        }
    }
}
