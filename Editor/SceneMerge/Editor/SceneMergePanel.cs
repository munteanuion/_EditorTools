using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SceneMergePanel : EditorWindow
{
    public const string VERSION = "1.7";

    private const string KEY_POST_PROCESSOR = "SCENEMERGE_POST_PROCESSOR";
    private const string KEY_GIT_CLIENT = "SCENEMERGE_GIT_CLIENT";
    private const string KEY_SVN_CLIENT = "SCENEMERGE_SVN_CLIENT";

    #region Settings

    /// <summary>
    /// Path to the GIT client
    /// </summary>
    private static string PATH_GIT_CLIENT = @"C:\Program Files (x86)\Git\bin\git.exe";

    /// <summary>
    /// Path to the SVN client
    /// </summary>
    private static string PATH_SVN_CLIENT = @"C:\Program Files\TortoiseSVN\bin\svn.exe";

    /// <summary>
    /// Post processor toggle
    /// </summary>
    public static bool UsePostProcessor = false;

    #endregion

    private int m_processId = 0;

    private string m_machineName = string.Empty;

    private bool m_showConfiguration = false;

    void OnEnable()
    {
        if (EditorPrefs.HasKey(KEY_POST_PROCESSOR))
        {
            UsePostProcessor = EditorPrefs.GetBool(KEY_POST_PROCESSOR);
        }

        if (EditorPrefs.HasKey(KEY_GIT_CLIENT))
        {
            PATH_GIT_CLIENT = EditorPrefs.GetString(KEY_GIT_CLIENT);
        }

        if (EditorPrefs.HasKey(KEY_SVN_CLIENT))
        {
            PATH_SVN_CLIENT = EditorPrefs.GetString(KEY_SVN_CLIENT);
        }

        try
        {
            Process process = Process.GetCurrentProcess();
            if (null != process)
            {
                m_processId = process.Id;
            }
            else
            {
                Debug.LogError("OnEnable: Failed to get process id");
                m_processId = -1;
            }
        }
        catch
        {
            Debug.LogError("OnEnable: Failed to get process id");
            m_processId = -2;
        }

        try
        {
            string machineName = System.Environment.MachineName;
            if (!string.IsNullOrEmpty(machineName))
            {
                m_machineName = machineName;
            }
            else
            {
                m_machineName = "Unknown";
            }
        }
        catch (System.Exception)
        {
            Debug.LogError("GetMachineName: Failed to get machine name");
            m_machineName = "Error";
        }
    }

    private const string FILE_PREFAB_BACKUP = "prefab.backup";
    private const string FILE_PREFAB_SNAPSHOT = "prefab.snapshot";
    private const string FILE_PREFAB_SORT = "prefab.sort";

    private const string FILE_SCENE_BACKUP = "scene.backup";
    private const string FILE_SCENE_SNAPSHOT = "scene.snapshot";
    private const string FILE_SCENE_SORT = "scene.sort";

    private struct CompareFiles
    {
        public string Path1;
        public string Path2;
    }

    [MenuItem("Window/Open Scene Merge")]
    private static void MenuOpen()
    {
        GetWindow<SceneMergePanel>("Scene Merge");
    }

    private void Update()
    {
        Repaint();
    }

    private Vector2 m_scroll = Vector2.zero;

    private void OnGUI()
    {
        m_scroll = GUILayout.BeginScrollView(m_scroll);

        GUILayout.BeginHorizontal(GUILayout.Width(position.width));
        GUILayout.Label(string.Format("Unity PID={0}\tMachine Name={1} ver={2}", GetProcessID(), GetMachineName(), VERSION));
        GUILayout.EndHorizontal();

        m_showConfiguration = GUILayout.Toggle(m_showConfiguration, "Configuration:");

        if (m_showConfiguration)
        {
            bool usePostProcessor = GUILayout.Toggle(UsePostProcessor, "Toggle post processor (Automatic sorting)");
            if (usePostProcessor != UsePostProcessor)
            {
                UsePostProcessor = usePostProcessor;
                EditorPrefs.SetBool(KEY_POST_PROCESSOR, usePostProcessor);
            }

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                EditorGUILayout.Separator();

                GUILayout.BeginHorizontal(GUILayout.Width(position.width));
                GUILayout.Label("Path to GIT Client:");
                if (GUILayout.Button("Browse"))
                {
                    string pathGitClient = EditorUtility.OpenFilePanel("GIT Client (git.exe)", Path.GetDirectoryName(PATH_GIT_CLIENT), "exe");
                    if (File.Exists(pathGitClient))
                    {
                        PATH_GIT_CLIENT = pathGitClient;
                        EditorPrefs.SetString(KEY_GIT_CLIENT, PATH_GIT_CLIENT);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(PATH_GIT_CLIENT);

                EditorGUILayout.Separator();

                GUILayout.BeginHorizontal(GUILayout.Width(position.width));
                GUILayout.Label("Path to SVN Client:");
                if (GUILayout.Button("Browse"))
                {
                    string pathSvnClient = EditorUtility.OpenFilePanel("SVN Client (svn.exe)", Path.GetDirectoryName(PATH_SVN_CLIENT), "exe");
                    if (File.Exists(pathSvnClient))
                    {
                        PATH_SVN_CLIENT = pathSvnClient;
                        EditorPrefs.SetString(KEY_SVN_CLIENT, PATH_SVN_CLIENT);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(PATH_SVN_CLIENT);
            }
        }
        EditorGUILayout.Separator();

        GUILayout.Label("Actions:");

        #region Compare files


        int selectedCount = 0;
        if (null != Selection.objects)
        {
            selectedCount = Selection.objects.Length;
        }
        GUI.enabled = selectedCount == 2;
        if (GUILayout.Button(string.Format("Compare Selected Files [{0}] of 2", selectedCount)))
        {
            MenuCompareObjects();
        }
        GUI.enabled = true;

        #endregion

        EditorGUILayout.Separator();

        #region Scene Manipulation

        string currentScene = EditorApplication.currentScene;

        if (string.IsNullOrEmpty(currentScene))
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Scene Take Snapshot"))
        {
            MenuSceneSnapshot();
        }

        if (GUILayout.Button("Scene Compare with Snapshot"))
        {
            MenuCompareScene();
        }

        if (GUILayout.Button("Scene Compare with Previous Version (GIT)"))
        {
            MenuCompareSceneGIT();
        }

        if (GUILayout.Button("Scene Compare with Previous Version (SVN)"))
        {
            MenuCompareSceneSVN();
        }

        if (GUILayout.Button("Sort and Save Scene"))
        {
            MenuSortScene();
        }

        if (string.IsNullOrEmpty(currentScene))
        {
            GUI.enabled = true;
        }

        #endregion

        EditorGUILayout.Separator();

        #region Prefabs

        bool hasPrefab = false;
        if (Selection.activeObject)
        {
            UnityEngine.Object obj = PrefabUtility.GetPrefabObject(Selection.activeObject);
            if (obj)
            {
                hasPrefab = true;
            }
        }

        if (!hasPrefab)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Prefab Take Snapshot"))
        {
            MenuPrefabSnapshot();
        }

        if (GUILayout.Button("Prefab Compare with Snapshot"))
        {
            MenuComparePrefab();
        }

        if (GUILayout.Button("Prefab Compare with Previous Version (GIT)"))
        {
            MenuComparePrefabGIT();
        }

        if (GUILayout.Button("Prefab Compare with Previous Version (SVN)"))
        {
            MenuComparePrefabSVN();
        }

        if (GUILayout.Button("Sort and Save Prefab"))
        {
            MenuSortPrefab();
        }

        if (!hasPrefab)
        {
            GUI.enabled = true;
        }

        #endregion

        GUILayout.EndScrollView();
    }

    #region Scene Merge

    public class YamlBlock : IComparable<YamlBlock>
    {
        public List<string> contents = new List<string>();

        public string GetFirst()
        {
            if (contents.Count > 0)
            {
                return contents[0];
            }
            else
            {
                return string.Empty;
            }
        }

        public int CompareTo(YamlBlock rhs)
        {
            return String.Compare(GetFirst(), rhs.GetFirst());
        }
    }

    static bool SortYaml(string filePath, out YamlBlock firstBlock, out List<YamlBlock> blocks)
    {
        firstBlock = new YamlBlock();
        blocks = new List<YamlBlock>();
        
        if (!File.Exists(filePath))
        {
            return false;
        }

        YamlBlock block = firstBlock;

        using (
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read,
                                      FileShare.ReadWrite))
        {
            using (StreamReader sr = new StreamReader(fs))
            {
                string line = string.Empty;
                do
                {
                    line = sr.ReadLine();
                    if (null == line)
                    {
                        break;
                    }
                    if (line.Trim().StartsWith("---")) // new section
                    {
                        block = new YamlBlock();
                        block.contents.Add(line);
                        blocks.Add(block);
                    }
                    else
                    {
                        block.contents.Add(line);
                    }
                    //Debug.Log(line);
                } while (true);
            }
            blocks.Sort();
        }

        Debug.Log(string.Format("Detected {0} Yaml Blocks", blocks.Count));

        // sort fileID
        string tokenFileID = @"- {fileID: ";
        foreach (YamlBlock item in blocks)
        {
            List<string> newContent = new List<string>();
            List<string> files = new List<string>();
            bool skipFileSort = true;
            foreach (String s in item.contents)
            {
                if (s.Trim().ToLower().Equals("m_Children:"))
                {
                    skipFileSort = false; //temporarily enable sorting
                }
                else if (s.Trim().StartsWith(tokenFileID))
                {
                    if (!skipFileSort)
                    {
                        files.Add(s);
                        continue;
                    }
                }
                else
                {
                    skipFileSort = true; //back to skipping file sorting

                    if (files.Count > 0)
                    {
                        files.Sort();
                        foreach (string file in files)
                        {
                            newContent.Add(file);
                        }
                        files.Clear();
                    }
                }

                newContent.Add(s);
            }
            if (files.Count > 0)
            {
                files.Sort();
                foreach (string file in files)
                {
                    newContent.Add(file);
                }
                files.Clear();
            }
            item.contents = newContent;
        }

        return true;
    }

    static void WriteYaml(string path, YamlBlock firstBlock, List<YamlBlock> blocks)
    {
        using (
            FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write,
                                      FileShare.ReadWrite))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (String s in firstBlock.contents)
                {
                    sw.WriteLine(s);
                }
                foreach (YamlBlock item in blocks)
                {
                    foreach (String s in item.contents)
                    {
                        sw.WriteLine(s);
                    }
                }
            }
        }

        Debug.Log(string.Format("{0} Created: {1}", DateTime.Now, path));
    }

    static bool SortAndWriteYawml(string filePath)
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;
        if (!SortYaml(filePath, out firstBlock, out blocks))
        {
            return false;
        }

        WriteYaml(filePath, firstBlock, blocks);
        return true;
    }

    [MenuItem("File/Compare Objects", validate=true)]
    public static bool MenuCompareObjectsValidate(UnityEngine.Object[] objects)
    {
        if (null != objects &&
            objects.Length == 2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    [MenuItem("File/Compare Objects")]
    public static void MenuCompareObjects()
    {
        UnityEngine.Object[] objects = Selection.objects;
        if (null == objects ||
            objects.Length != 2)
        {
            return;
        }

        CompareFiles param;
        param.Path1 = AssetDatabase.GetAssetPath(objects[0].GetInstanceID());
        param.Path2 = AssetDatabase.GetAssetPath(objects[1].GetInstanceID());

        ParameterizedThreadStart ts = new ParameterizedThreadStart(RunObjectsDiff);
        Thread thread = new Thread(ts);
        thread.Start(param);
    }

    [MenuItem("File/Scene Take Snapshot")]
    public static void MenuSceneSnapshot()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;
        string currentScene = EditorApplication.currentScene;
        if (string.IsNullOrEmpty(currentScene))
        {
            return;
        }
        Debug.Log(string.Format("Current Scene: {0}", currentScene));
        if (!SortYaml(currentScene, out firstBlock, out blocks))
        {
            return;
        }

        WriteYaml(FILE_SCENE_SNAPSHOT, firstBlock, blocks);
    }

    [MenuItem("File/Scene Compare with Snapshot")]
    public static void MenuCompareScene()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;
        string currentScene = EditorApplication.currentScene;
        if (string.IsNullOrEmpty(currentScene))
        {
            return;
        }
        Debug.Log(string.Format("Current Scene: {0}", currentScene));
        if (!SortYaml(currentScene, out firstBlock, out blocks))
        {
            return;
        }

        WriteYaml(FILE_SCENE_SORT, firstBlock, blocks);

        Debug.Log(string.Format("{0} Sort scene complete", DateTime.Now));

        AssetDatabase.Refresh();

        ThreadStart ts = new ThreadStart(RunSceneDiff);
        Thread thread = new Thread(ts);
        thread.Start();
    }

    [MenuItem("File/Scene Compare with Previous Version (SVN)")]
    static public void MenuCompareSceneSVN()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;
        string currentScene = EditorApplication.currentScene;
        if (string.IsNullOrEmpty(currentScene))
        {
            return;
        }
        Debug.Log(string.Format("Current Scene: {0}", currentScene));
        if (!SortYaml(currentScene, out firstBlock, out blocks))
        {
            return;
        }

        WriteYaml(FILE_SCENE_SORT, firstBlock, blocks);

        Debug.Log(string.Format("{0} Sort scene complete", DateTime.Now));

        AssetDatabase.Refresh();

        ParameterizedThreadStart ts = new ParameterizedThreadStart(RunSceneSVN);
        Thread thread = new Thread(ts);
        thread.Start(currentScene);
    }

    [MenuItem("File/Scene Compare with Previous Version (GIT)")]
    static public void MenuCompareSceneGIT()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;
        string currentScene = EditorApplication.currentScene;
        if (string.IsNullOrEmpty(currentScene))
        {
            return;
        }
        Debug.Log(string.Format("Current Scene: {0}", currentScene));
        if (!SortYaml(currentScene, out firstBlock, out blocks))
        {
            return;
        }

        WriteYaml(FILE_SCENE_SORT, firstBlock, blocks);

        Debug.Log(string.Format("{0} Sort scene complete", DateTime.Now));

        AssetDatabase.Refresh();

        ParameterizedThreadStart ts = new ParameterizedThreadStart(RunSceneGIT);
        Thread thread = new Thread(ts);
        thread.Start(currentScene);
    }

    [MenuItem("File/Sort and Save Scene")]
    public static void MenuSortScene()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;
        string currentScene = EditorApplication.currentScene;
        if (string.IsNullOrEmpty(currentScene))
        {
            return;
        }
        Debug.Log(string.Format("Current Scene: {0}", currentScene));
        if (!SortYaml(currentScene, out firstBlock, out blocks))
        {
            return;
        }

        WriteYaml(string.Format("{0}", currentScene), firstBlock, blocks);

        Debug.Log(string.Format("{0} Sort scene complete", DateTime.Now));

        AssetDatabase.Refresh();
    }
	
    public static void SortScene(string currentScene, bool refresh)
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;
        if (string.IsNullOrEmpty(currentScene))
        {
            return;
        }
        Debug.Log(string.Format("Current Scene: {0}", currentScene));
        if (!SortYaml(currentScene, out firstBlock, out blocks))
        {
            return;
        }

        WriteYaml(string.Format("{0}", currentScene), firstBlock, blocks);

        Debug.Log(string.Format("{0} Sort scene complete", DateTime.Now));

        if (refresh)
        {
            AssetDatabase.Refresh();
        }
    }

    private static bool HasSelectedPrefab()
    {
        if (Selection.activeObject)
        {
            UnityEngine.Object obj = PrefabUtility.GetPrefabObject(Selection.activeObject);
            if (obj)
            {
                return true;
            }
        }

        return false;
    }

    [MenuItem("File/Prefab Take Snapshot", validate = true)]
    public static bool MenuPrefabSnapshotValidate()
    {
        return HasSelectedPrefab();
    }

    [MenuItem("File/Prefab Take Snapshot")]
    public static void MenuPrefabSnapshot()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;

        if (Selection.activeObject)
        {
            UnityEngine.Object obj = PrefabUtility.GetPrefabObject(Selection.activeObject);
            if (obj)
            {
                string prefabPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    Debug.Log(string.Format("Current Prefab: {0}", prefabPath));
                    if (!SortYaml(prefabPath, out firstBlock, out blocks))
                    {
                        return;
                    }

                    WriteYaml(FILE_PREFAB_SNAPSHOT, firstBlock, blocks);
                }
            }
        }
    }

    [MenuItem("File/Prefab Compare with Snapshot", validate = true)]
    public static bool MenuComparePrefabValidate()
    {
        return HasSelectedPrefab();
    }

    [MenuItem("File/Prefab Compare with Snapshot")]
    static public void MenuComparePrefab()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;

        if (Selection.activeObject)
        {
            UnityEngine.Object obj = PrefabUtility.GetPrefabObject(Selection.activeObject);
            if (obj)
            {
                string prefabPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    Debug.Log(string.Format("Current Prefab: {0}", prefabPath));
                    if (!SortYaml(prefabPath, out firstBlock, out blocks))
                    {
                        return;
                    }

                    WriteYaml(string.Format(FILE_PREFAB_SORT), firstBlock, blocks);

                    Debug.Log(string.Format("{0} Sort prefab complete", DateTime.Now));

                    AssetDatabase.Refresh();

                    ThreadStart ts = new ThreadStart(RunPrefabDiff);
                    Thread thread = new Thread(ts);
                    thread.Start();
                }
            }
        }
    }

    [MenuItem("File/Prefab Compare with previous version (SVN)", validate = true)]
    public static bool MenuCompareMenuComparePrefabSVN()
    {
        return HasSelectedPrefab();
    }

    [MenuItem("File/Prefab Compare with previous version (SVN)")]
    static public void MenuComparePrefabSVN()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;

        if (Selection.activeObject)
        {
            UnityEngine.Object obj = PrefabUtility.GetPrefabObject(Selection.activeObject);
            if (obj)
            {
                string prefabPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    Debug.Log(string.Format("Current Prefab: {0}", prefabPath));
                    if (!SortYaml(prefabPath, out firstBlock, out blocks))
                    {
                        return;
                    }

                    WriteYaml(string.Format(FILE_PREFAB_SORT), firstBlock, blocks);

                    Debug.Log(string.Format("{0} Sort prefab complete", DateTime.Now));

                    AssetDatabase.Refresh();

                    ParameterizedThreadStart ts = new ParameterizedThreadStart(RunPrefabSVN);
                    Thread thread = new Thread(ts);
                    thread.Start(prefabPath);
                }
            }
        }
    }

    [MenuItem("File/Prefab Compare with previous version (GIT)", validate = true)]
    public static bool MenuCompareMenuComparePrefabGIT()
    {
        return HasSelectedPrefab();
    }

    [MenuItem("File/Prefab Compare with previous version (GIT)")]
    static public void MenuComparePrefabGIT()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;

        if (Selection.activeObject)
        {
            UnityEngine.Object obj = PrefabUtility.GetPrefabObject(Selection.activeObject);
            if (obj)
            {
                string prefabPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    Debug.Log(string.Format("Current Prefab: {0}", prefabPath));
                    if (!SortYaml(prefabPath, out firstBlock, out blocks))
                    {
                        return;
                    }

                    WriteYaml(string.Format(FILE_PREFAB_SORT), firstBlock, blocks);

                    Debug.Log(string.Format("{0} Sort prefab complete", DateTime.Now));

                    AssetDatabase.Refresh();

                    ParameterizedThreadStart ts = new ParameterizedThreadStart(RunPrefabGIT);
                    Thread thread = new Thread(ts);
                    thread.Start(prefabPath);
                }
            }
        }
    }

    [MenuItem("File/Sort and Save Prefab", validate = true)]
    public static bool MenuSortPrefabValidate()
    {
        return HasSelectedPrefab();
    }

    [MenuItem("File/Sort and Save Prefab")]
    public static void MenuSortPrefab()
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;

        if (Selection.activeObject)
        {
            UnityEngine.Object obj = PrefabUtility.GetPrefabObject(Selection.activeObject);
            if (obj)
            {
                string prefabPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    Debug.Log(string.Format("Current Prefab: {0}", prefabPath));
                    if (!SortYaml(prefabPath, out firstBlock, out blocks))
                    {
                        return;
                    }

                    WriteYaml(string.Format("{0}", prefabPath), firstBlock, blocks);

                    Debug.Log(string.Format("{0} Sort prefab complete", DateTime.Now));

                    AssetDatabase.Refresh();
                }
            }
        }
    }
	
    public static void SortPrefab(string prefabPath, bool refresh)
    {
        YamlBlock firstBlock;
        List<YamlBlock> blocks;

	    if (!string.IsNullOrEmpty(prefabPath))
	    {
	        Debug.Log(string.Format("Current Prefab: {0}", prefabPath));
	        if (!SortYaml(prefabPath, out firstBlock, out blocks))
	        {
	            return;
	        }
	
	        WriteYaml(string.Format("{0}", prefabPath), firstBlock, blocks);
	
	        Debug.Log(string.Format("{0} Sort prefab complete", DateTime.Now));
	
			if (refresh)
			{
		        AssetDatabase.Refresh();
			}
		}
    }
	
    #endregion

    #region Compare processes

    public static void RunObjectsDiff(object data)
    {
        if (null == data ||
            !(data is CompareFiles))
        {
            return;
        }

        CompareFiles param = (CompareFiles)data;

        string path1 = param.Path1;
        string path2 = param.Path2;

        if (!File.Exists(path1))
        {
            Debug.LogError(string.Format("Can't find object 1={0}", path1));
            return;
        }

        if (!File.Exists(path2))
        {
            Debug.LogError(string.Format("Can't find object 2={0}", path2));
            return;
        }

        string diffProgram = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                diffProgram = @"/Applications/Xcode.app/Contents/Applications/FileMerge.app/Contents/MacOS/FileMerge";
                break;
            case RuntimePlatform.WindowsEditor:
                diffProgram = @"C:\Program Files (x86)\WinMerge\WinMergeU.exe";
                break;
        }

        if (!File.Exists(diffProgram))
        {
            Debug.LogError(string.Format("Can't find diff program={0}", diffProgram));
            return;
        }

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                RunProcess(diffProgram, string.Format("-left \"{0}\" -right \"{1}\"",
                    path1,
                    path2));
                break;
            case RuntimePlatform.WindowsEditor:
                RunProcess(diffProgram, string.Format("\"{0}\" \"{1}\"",
                    path1,
                    path2));
                break;
        }
    }

    public static void RunSceneDiff()
    {
        if (!File.Exists(FILE_SCENE_SNAPSHOT))
        {
            Debug.LogError(string.Format("Can't find snapshot={0}", FILE_SCENE_SNAPSHOT));
            return;
        }

        if (!File.Exists(FILE_SCENE_SORT))
        {
            Debug.LogError(string.Format("Can't find sorted scene={0}", FILE_SCENE_SORT));
            return;
        }

        string diffProgram = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                diffProgram = @"/Applications/Xcode.app/Contents/Applications/FileMerge.app/Contents/MacOS/FileMerge";
                break;
            case RuntimePlatform.WindowsEditor:
                diffProgram = @"C:\Program Files (x86)\WinMerge\WinMergeU.exe";
                break;
        }

        if (!File.Exists(diffProgram))
        {
            Debug.LogError(string.Format("Can't find diff program={0}", diffProgram));
            return;
        }

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                RunProcess(diffProgram, string.Format("-left \"{0}\" -right \"{1}\"",
                    FILE_SCENE_SNAPSHOT,
                    FILE_SCENE_SORT));
                break;
            case RuntimePlatform.WindowsEditor:
                RunProcess(diffProgram, string.Format("\"{0}\" \"{1}\"",
                    FILE_SCENE_SNAPSHOT,
                    FILE_SCENE_SORT));
                break;
        }
    }

    public static void RunSceneSVN(object obj)
    {
        string original = obj as string;

        string command = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                command = @"/usr/bin/svn";
                break;
            case RuntimePlatform.WindowsEditor:
                command = PATH_SVN_CLIENT;
                break;
        }

        #region Make copy of reverted original, make snapshot, and restore backup

        //backup original
        File.Copy(original, FILE_SCENE_BACKUP, true);
        Debug.Log(string.Format("Backup scene={0}", original));

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                RunProcess(command, string.Format("revert \"{0}\"",
                    original));
                break;
            case RuntimePlatform.WindowsEditor:
                RunProcess(command, string.Format("revert \"{0}\"",
                    original));
                break;
        }

        //copy reverted to snapshot
        File.Copy(original, FILE_SCENE_SNAPSHOT, true);

        //check if copy failed
        if (!File.Exists(FILE_SCENE_SNAPSHOT))
        {
            Debug.LogError(string.Format("Can't find scene snapshot={0}", FILE_SCENE_SNAPSHOT));
        }
        else
        {
            SortAndWriteYawml(FILE_SCENE_SNAPSHOT);
        }

        //restore backup
        File.Copy(FILE_SCENE_BACKUP, original, true);
        Debug.Log(string.Format("Restore original scene={0}", original));

        #endregion

        RunSceneDiff();
    }

    public static void RunSceneGIT(object obj)
    {
        string original = obj as string;

        string command = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                command = @"/usr/bin/git";
                break;
            case RuntimePlatform.WindowsEditor:
                command = PATH_GIT_CLIENT;
                break;
        }

        #region Make copy of reverted original, make snapshot, and restore backup

        //backup original
        File.Copy(original, FILE_SCENE_BACKUP, true);
        Debug.Log(string.Format("Backup scene={0}", original));

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                RunProcess(command, string.Format("checkout -- \"{0}\"",
                    original));
                break;
            case RuntimePlatform.WindowsEditor:
                RunProcess(command, string.Format("checkout -- \"{0}\"",
                    original));
                break;
        }

        //copy reverted to snapshot
        File.Copy(original, FILE_SCENE_SNAPSHOT, true);

        //check if copy failed
        if (!File.Exists(FILE_SCENE_SNAPSHOT))
        {
            Debug.LogError(string.Format("Can't find scene snapshot={0}", FILE_SCENE_SNAPSHOT));
        }
        else
        {
            SortAndWriteYawml(FILE_SCENE_SNAPSHOT);
        }

        //restore backup
        File.Copy(FILE_SCENE_BACKUP, original, true);
        Debug.Log(string.Format("Restore original scene={0}", original));

        #endregion

        RunSceneDiff();
    }

    public static void RunPrefabDiff()
    {
        if (!File.Exists(FILE_PREFAB_SNAPSHOT))
        {
            Debug.LogError(string.Format("Can't find {0}", FILE_PREFAB_SNAPSHOT));
            return;
        }

        if (!File.Exists(FILE_PREFAB_SORT))
        {
            Debug.LogError(string.Format("Can't find {0}", FILE_PREFAB_SORT));
            return;
        }

        string diffProgram = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                diffProgram = @"/Applications/Xcode.app/Contents/Applications/FileMerge.app/Contents/MacOS/FileMerge";
                break;
            case RuntimePlatform.WindowsEditor:
                diffProgram = @"C:\Program Files (x86)\WinMerge\WinMergeU.exe";
                break;
        }

        if (!File.Exists(diffProgram))
        {
            Debug.LogError(string.Format("Can't find diff program={0}", diffProgram));
            return;
        }

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                RunProcess(diffProgram, string.Format("-left \"{0}\" -right \"{1}\"",
                    FILE_PREFAB_SNAPSHOT,
                    FILE_PREFAB_SORT));
                break;
            case RuntimePlatform.WindowsEditor:
                RunProcess(diffProgram, string.Format("\"{0}\" \"{1}\"",
                    FILE_PREFAB_SNAPSHOT,
                    FILE_PREFAB_SORT));
                break;
        }
    }

    public static void RunPrefabSVN(object obj)
    {
        string original = obj as string;

        string command = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                command = @"/usr/bin/svn";
                break;
            case RuntimePlatform.WindowsEditor:
                command = PATH_SVN_CLIENT;
                break;
        }

        #region Make copy of reverted original, make snapshot, and restore backup

        //backup original
        File.Copy(original, FILE_PREFAB_BACKUP, true);
        Debug.Log(string.Format("Backup prefab={0}", original));

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                RunProcess(command, string.Format("revert \"{0}\"",
                    original));
                break;
            case RuntimePlatform.WindowsEditor:
                RunProcess(command, string.Format("revert \"{0}\"",
                    original));
                break;
        }

        //copy reverted to snapshot
        File.Copy(original, FILE_PREFAB_SNAPSHOT, true);

        //check if copy failed
        if (!File.Exists(FILE_PREFAB_SNAPSHOT))
        {
            Debug.LogError(string.Format("Can't find prefab snapshot={0}", FILE_PREFAB_SNAPSHOT));
        }
        else
        {
            SortAndWriteYawml(FILE_PREFAB_SNAPSHOT);
        }

        //restore backup
        File.Copy(FILE_PREFAB_BACKUP, original, true);
        Debug.Log(string.Format("Restore original prefab={0}", original));

        #endregion

        RunPrefabDiff();
    }

    public static void RunPrefabGIT(object obj)
    {
        string original = obj as string;

        string command = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                command = @"/usr/bin/git";
                break;
            case RuntimePlatform.WindowsEditor:
                command = PATH_GIT_CLIENT;
                break;
        }

        #region Make copy of reverted original, make snapshot, and restore backup

        //backup original
        File.Copy(original, FILE_PREFAB_BACKUP, true);
        Debug.Log(string.Format("Backup prefab={0}", original));

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                RunProcess(command, string.Format("checkout -- \"{0}\"",
                    original));
                break;
            case RuntimePlatform.WindowsEditor:
                RunProcess(command, string.Format("checkout -- \"{0}\"",
                    original));
                break;
        }

        //copy reverted to snapshot
        File.Copy(original, FILE_PREFAB_SNAPSHOT, true);

        //check if copy failed
        if (!File.Exists(FILE_PREFAB_SNAPSHOT))
        {
            Debug.LogError(string.Format("Can't find prefab snapshot={0}", FILE_PREFAB_SNAPSHOT));
        }
        else
        {
            SortAndWriteYawml(FILE_PREFAB_SNAPSHOT);
        }

        //restore backup
        File.Copy(FILE_PREFAB_BACKUP, original, true);
        Debug.Log(string.Format("Restore original prefab={0}", original));

        #endregion

        RunPrefabDiff();
    }

    #endregion

    #region RUN PROCESS
    public static void RunProcess(string path, string arguments)
    {
        string error = string.Empty;
        string output = string.Empty;
        RunProcess(path, string.Empty, arguments, ref output, ref error);
    }

    public static void RunProcess(string path, string WorkingDirectory, string arguments)
    {
        string error = string.Empty;
        string output = string.Empty;
        RunProcess(path, WorkingDirectory, arguments, ref output, ref error);
    }

    public static void RunProcess(string path, string WorkingDirectory, string arguments, ref string output, ref string error)
    {
        try
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = WorkingDirectory;
            process.StartInfo.FileName = path;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.ErrorDialog = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            DateTime startTime = DateTime.Now;
            Debug.Log(string.Format("[Running Process] filename={0} arguments={1}", process.StartInfo.FileName,
                                    process.StartInfo.Arguments));

            process.Start();

            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();

            float elapsed = (float)(DateTime.Now - startTime).TotalSeconds;
            Debug.Log(string.Format("[Results] elapsedTime: {3} errors: {2}\noutput: {1}", process.StartInfo.FileName,
                                    output, error, elapsed));

            //if (output.Length > 0 ) Debug.Log("Output: " + output);
            //if (error.Length > 0 ) Debug.Log("Error: " + error); 
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning(string.Format("Unable to run process: path={0} arguments={1} exception={2}", path, arguments, ex));
        }
    }

    /// <summary>
    /// Get the IDE process IDE
    /// </summary>
    /// <returns></returns>
    public int GetProcessID()
    {
        return m_processId;
    }

    /// <summary>
    /// Get the machine name
    /// </summary>
    /// <returns></returns>
    public string GetMachineName()
    {
        return m_machineName;
    }
    #endregion
}