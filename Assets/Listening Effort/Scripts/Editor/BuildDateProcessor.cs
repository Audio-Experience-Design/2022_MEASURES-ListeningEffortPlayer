using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Text;

public class BuildDateProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        ////Give the user a file folder pop-up asking for the location you wish to save the file to
        //string path = EditorUtility.SaveFolderPanel("Save location", "", "");
        ////Alternative you can also just hardcode the path..
        ////string path = "C:/Dev/Unity/MyProject/MyBuilds/

        ////Get the current datetime and convert it to a string with some explanatory text
        //string date = string.Format("Build date: {0}", DateTime.Now.ToString());

        //TextAsset textAsset = new TextAsset(date);
        //AssetDatabase.CreateAsset(textAsset, "Assets/BuildDate.txt");

        StringBuilder sb = new StringBuilder();
        sb.Append("public static class BuildInfo");
        sb.Append("{");
        sb.Append("public static string BUILD_TIME = \"");
        sb.Append(DateTime.UtcNow.ToString());
        sb.Append("\";");
        sb.Append("}");
        using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"Assets/BuildInfo.cs"))
        {
            file.WriteLine(sb.ToString());
        }
    }
}