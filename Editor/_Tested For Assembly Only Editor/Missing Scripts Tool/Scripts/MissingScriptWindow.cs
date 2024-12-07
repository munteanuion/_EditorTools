using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace PCLauncher
{
    public class MissingScriptWindow : EditorWindow
    {
        #region Variables

        private SceneAsset selectedScene;
        private List<GameObject> objectsWithMissingScripts = new List<GameObject>();
        private bool showWarnings = true;
        private string actionLog = "";

        #endregion

        #region Editor Window Setup
        [MenuItem("Tools/Missing Script Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<MissingScriptWindow>("Missing Script Tool");
            window.maxSize = new Vector2(720, 400);
            window.minSize = new Vector2(720, 400);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.cyan }
            };
            GUILayout.Label("Missing Script Tool", labelStyle);

            GUILayout.Space(10);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = Texture2D.whiteTexture, textColor = Color.white },
                hover = { background = Texture2D.whiteTexture, textColor = Color.cyan },
                padding = new RectOffset(10, 10, 5, 5)
            };

            if (GUILayout.Button("Find Missing Scripts in Entire Project", buttonStyle))
            {
                objectsWithMissingScripts.Clear();
                FindMissingScriptsInProject();
            }

            GUILayout.Space(10);

            selectedScene = (SceneAsset)EditorGUILayout.ObjectField("Scene", selectedScene, typeof(SceneAsset), false, GUILayout.Height(30));

            if (GUILayout.Button("Open Current Scene", buttonStyle))
            {
                OpenCurrentScene();
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Find Missing Scripts in Scene", buttonStyle) || (Event.current.shift && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F))
            {
                objectsWithMissingScripts.Clear();
                if (selectedScene != null)
                {
                    string scenePath = AssetDatabase.GetAssetPath(selectedScene);
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    FindMissingScriptsInScene(scene);
                    LogAction($"Found {objectsWithMissingScripts.Count} objects with missing scripts.");
                }
                else
                {
                    LogAction("Please select a scene.");
                }
            }

            if (GUILayout.Button("Save Scene", buttonStyle))
            {
                SaveScene();
            }

            EditorGUILayout.EndHorizontal();

            if (objectsWithMissingScripts.Count > 0)
            {
                if (GUILayout.Button("Remove Missing Components", buttonStyle) || (Event.current.control && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R))
                {
                    ShowConfirmationDialog("Are you sure you want to remove missing components?", RemoveMissingComponents);
                }
            }

            GUILayout.Space(10);

            GUIStyle textAreaStyle = new GUIStyle(GUI.skin.textArea)
            {
                fontSize = 12,
                wordWrap = true,
                stretchHeight = true,
                normal = { textColor = Color.white, background = Texture2D.whiteTexture }
            };
            GUILayout.Label("Action Log", labelStyle);
            actionLog = EditorGUILayout.TextArea(actionLog, textAreaStyle, GUILayout.Height(150));
        }

        #endregion

        #region Operations
        private void OpenCurrentScene()
        {
            EditorSceneManager.OpenScene(SceneManager.GetActiveScene().path, OpenSceneMode.Single);
            LogAction("Opened current scene: " + SceneManager.GetActiveScene().name);
        }

        private void FindMissingScriptsInScene(Scene scene)
        {
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                FindMissingScripts(obj);
            }
        }

        private void FindMissingScripts(GameObject obj)
        {
            Component[] components = obj.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    if (!objectsWithMissingScripts.Contains(obj))
                    {
                        objectsWithMissingScripts.Add(obj);
                        if (showWarnings)
                        {
                            Debug.LogWarning("Missing script on object: " + obj.name);
                        }
                        LogAction("Missing script on object: " + obj.name);
                    }
                    break;
                }
            }

            foreach (Transform child in obj.transform)
            {
                FindMissingScripts(child.gameObject);
            }
        }

        private void FindMissingScriptsInProject()
        {
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
            foreach (string prefabGUID in allPrefabs)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab != null)
                {
                    try
                    {
                        FindMissingScripts(prefab);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Error processing prefab {prefab.name}: {e.Message}");
                        LogAction($"Error processing prefab {prefab.name}: {e.Message}");
                    }
                }
            }

            string[] allScenes = AssetDatabase.FindAssets("t:Scene");
            foreach (string sceneGUID in allScenes)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
                try
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    FindMissingScriptsInScene(scene);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error processing scene {scenePath}: {e.Message}");
                    LogAction($"Error processing scene {scenePath}: {e.Message}");
                }
            }

            if (objectsWithMissingScripts.Count == 0)
            {
                LogAction("No missing scripts found in project.");
            }
        }

        private void RemoveMissingComponents()
        {
            foreach (GameObject obj in objectsWithMissingScripts)
            {
                if (obj != null)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                    LogAction("Removed missing components from: " + obj.name);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            objectsWithMissingScripts.Clear();
            LogAction("Missing components removed from scene.");
        }

        private void SaveScene()
        {
            if (selectedScene != null)
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                LogAction("Scene saved.");
            }
        }

        private void SaveSceneAs()
        {
            string path = EditorUtility.SaveFilePanel("Save Scene As", "Assets", selectedScene.name, "unity");
            if (!string.IsNullOrEmpty(path))
            {
                Scene scene = SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(scene, path);
                LogAction("Scene saved as: " + path);
            }
        }

        private void ShowConfirmationDialog(string message, System.Action onConfirmed)
        {
            if (EditorUtility.DisplayDialog("Confirmation", message, "Yes", "No"))
            {
                onConfirmed.Invoke();
            }
        }

        private void LogAction(string message)
        {
            actionLog += $"{System.DateTime.Now}: {message}\n";

            Debug.Log(message);
        }

        #endregion
    }
}
#endif