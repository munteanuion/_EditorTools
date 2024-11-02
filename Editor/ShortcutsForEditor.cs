using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ShortcutsForEditor : EditorWindow
{
    private static bool isGameWindowMaximized = false;



    // Toggle maximize Game Window
    [MenuItem("Tools/Shortcuts/Toggle Maximize Game Window _&F")] // Shortcut: Shift + Alt + F
    private static void ToggleMaximizeGameWindow()
    {
        isGameWindowMaximized = !isGameWindowMaximized;

        EditorApplication.ExecuteMenuItem("Window/General/Game");

        var gameWindow = EditorWindow.focusedWindow;
        if (gameWindow != null)
        {
            gameWindow.maximized = isGameWindowMaximized;
        }
    }


    // Focus Scene Window when entering Prefab Mode
    [InitializeOnLoadMethod]
    private static void FocusSceneOnPrefabEdit()
    {
        PrefabStage.prefabStageOpened += stage =>
        {
            EditorApplication.ExecuteMenuItem("Window/General/Scene");
        };
    }


    [MenuItem("Tools/Shortcuts/Toggle Lock _&q")] // Alt + Q
    public static void ToggleInspectorLock()
    {
        // Obține Inspectorul activ
        var inspectorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        var inspectorWindow = EditorWindow.GetWindow(inspectorWindowType);

        if (inspectorWindow == null)
        {
            Debug.LogWarning("Nu s-a găsit fereastra Inspector.");
            return;
        }

        // Folosește reflecția pentru a accesa câmpul `isLocked`
        var isLockedProperty = inspectorWindowType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
        if (isLockedProperty != null)
        {
            // Toggle Lock
            bool isLocked = (bool)isLockedProperty.GetValue(inspectorWindow, null);
            isLockedProperty.SetValue(inspectorWindow, !isLocked, null);
            inspectorWindow.Repaint();
        }
        else
        {
            Debug.LogWarning("Nu s-a putut accesa proprietatea isLocked.");
        }
    }





}
