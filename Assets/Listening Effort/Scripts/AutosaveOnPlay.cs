#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutosaveOnRun : ScriptableObject
{
	static AutosaveOnRun()
	{
		EditorApplication.playModeStateChanged += (s) =>
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
			{
//#pragma warning disable CS0618 // Type or member is obsolete
				Debug.Log("Auto-Saving scene before entering Play mode: " + EditorSceneManager.GetActiveScene().name);
//#pragma warning restore CS0618 // Type or member is obsolete

				EditorSceneManager.SaveOpenScenes();
				//EditorApplication.SaveScene();
			}
		};
	}
}
#endif