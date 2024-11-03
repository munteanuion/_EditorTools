using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class SceneMergePostProcessor : AssetPostprocessor
{
	// detected scenes
    private static Dictionary<string, DateTime> m_detectedScenes = new Dictionary<string, DateTime>();
	
	// detected scenes
    private static Dictionary<string, DateTime> m_detectedPrefabs = new Dictionary<string, DateTime>();
	
	// scenes to process
    private static List<string> m_processScenes = new List<string>();
	
	// prefabs to process
    private static List<string> m_processPrefabs = new List<string>();

	// wait for thread to start processing
	private static DateTime m_timerImported = DateTime.MinValue;
	
	// post processor event
	static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
	{
	    if (SceneMergePanel.UsePostProcessor &&
			!Application.isPlaying &&
			!EditorApplication.isCompiling &&
			EditorApplication.timeSinceStartup > 10f)
	    {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

	        foreach (string path in importedAssets)
	        {
                string currentScene = EditorApplication.currentScene;
                if (!string.IsNullOrEmpty(currentScene) &&
                    currentScene.ToUpper().Equals(path.ToUpper()))
                {
                    //Debug.Log(string.Format("{0} Found: {1}", DateTime.Now, scene.path));
                    if (!m_detectedScenes.ContainsKey(currentScene) ||
                        (m_detectedScenes[currentScene] + TimeSpan.FromSeconds(5)) < File.GetLastWriteTime(currentScene) &&
						!m_processScenes.Contains(currentScene))
                    {
                        m_detectedScenes[currentScene] = File.GetLastWriteTime(currentScene);
                        m_processScenes.Add(currentScene);
                    }
                }

                foreach (EditorBuildSettingsScene scene in scenes)
                {
                    if (scene.path.ToUpper().Equals(path.ToUpper()))
                    {
                        //Debug.Log(string.Format("{0} Found: {1}", DateTime.Now, scene.path));
                        if (!m_detectedScenes.ContainsKey(scene.path) ||
                            (m_detectedScenes[scene.path] + TimeSpan.FromSeconds(5)) < File.GetLastWriteTime(scene.path) &&
							!m_processScenes.Contains(scene.path))
                        {
                            m_detectedScenes[scene.path] = File.GetLastWriteTime(scene.path);
                            m_processScenes.Add(scene.path);
                        }
                    }
                }
				
				if (Path.GetExtension(path).ToUpper().Equals(".PREFAB"))
				{
					//Debug.Log(string.Format("Found prefab: {0}", path));
                    if (!m_detectedPrefabs.ContainsKey(path) ||
                        (m_detectedPrefabs[path] + TimeSpan.FromSeconds(5)) < File.GetLastWriteTime(path) &&
						!m_processPrefabs.Contains(path))
                    {
                        m_detectedPrefabs[path] = File.GetLastWriteTime(path);
                        m_processPrefabs.Add(path);
                    }
				}
				
	        }

            if ((m_processScenes.Count > 0 ||
				m_processPrefabs.Count > 0) &&
                m_timerImported < DateTime.Now)
	        {
	            m_timerImported = DateTime.Now + TimeSpan.FromSeconds(3); //don't import again for a while
	            ThreadStart ts = new ThreadStart(Worker);
	            Thread thread = new Thread(ts);
	            thread.Start();
	        }
	    }
	}
	
	static void Worker()
	{
		while (DateTime.Now < m_timerImported)
		{
			Thread.Sleep(0);
		}

        foreach (string scene in m_processScenes)
        {
            Debug.Log(string.Format("{0} Sorting: {1}", DateTime.Now, scene));
            SceneMergePanel.SortScene(scene, false);
            m_detectedScenes[scene] = File.GetLastWriteTime(scene);
        }
		m_processScenes.Clear();
		
        foreach (string prefab in m_processPrefabs)
        {
            Debug.Log(string.Format("{0} Sorting: {1}", DateTime.Now, prefab));
            SceneMergePanel.SortPrefab(prefab, false);
            m_detectedPrefabs[prefab] = File.GetLastWriteTime(prefab);
        }
		m_processPrefabs.Clear();
	}
}