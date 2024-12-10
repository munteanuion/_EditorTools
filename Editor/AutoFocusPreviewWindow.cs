#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoFocusPreviewWindow
{
    private const string HIERARCHY = "Hierarchy";
    private const string SCENE = "Scene";
    private const string PREVIEW = "Preview";
    private const string PROJECT = "Project";
    
    private static string lastOpenedWindow = "";
    
    
    
    static AutoFocusPreviewWindow()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    private static void OnSelectionChanged()
    {
        Object selectedObject = Selection.activeObject;

        if (selectedObject == null) return;

        if (!EditorApplication.isPlaying)
        {
            if (IsSelectionInHierarchy(selectedObject))
            {
                FocusOnSceneView();
                return;
            }
        }

        if (IsSelectionInProjectWindow(selectedObject))
        {
            if (!IsObjectForPreview(selectedObject))
            {
                FocusOnPreviewWindow();
            }
        }
    }

    private static bool IsSelectionInHierarchy(Object selectedObject)
    {
        return selectedObject is GameObject && !AssetDatabaseContains(selectedObject);
    }
    
    private static bool AssetDatabaseContains(Object selectedObject)
    {
        string assetPath = AssetDatabase.GetAssetPath(selectedObject);
    
        return !string.IsNullOrEmpty(assetPath) && !AssetDatabase.IsValidFolder(assetPath);
    }


    private static bool IsSelectionInProjectWindow(Object selectedObject)
    {
        EditorWindow focusedWindow = EditorWindow.focusedWindow;

        return focusedWindow != null 
               && focusedWindow.title == PROJECT 
               && AssetDatabaseContains(selectedObject);
    }

    private static bool IsObjectForPreview(Object selectedObject)
    {
        string path = AssetDatabase.GetAssetPath(selectedObject);

        bool previewFound = 
            !AssetDatabase.IsValidFolder(path) // Nu este un folder
            && AssetDatabaseContains(selectedObject) // Este un asset valid
            && !(
                selectedObject is GameObject
                || selectedObject is AnimationClip
                || selectedObject is Mesh
                || selectedObject is AudioClip
                || selectedObject is Texture
                || selectedObject is Sprite
                || selectedObject is Material
                || path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) // Este un fișier FBX
            );

        return previewFound;
    }

    private static void FocusOnSceneView()
    {
        if (IsYetOpenedWindow(SCENE))
            return;
        
        EditorApplication.ExecuteMenuItem("Window/General/Scene");
    }

    private static void FocusOnPreviewWindow()
    {
        if (IsYetOpenedWindow(PREVIEW))
            return;
        
        var previewWindow = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.PreviewWindow"));
        if (previewWindow != null)
        {
            previewWindow.Focus();
        }
    }

    private static bool IsYetOpenedWindow(string windowTitle)
    {
        EditorWindow focusedWindow = EditorWindow.focusedWindow;
        
        bool focusedWindowPresent = focusedWindow != null 
                                    && (!lastOpenedWindow.Equals(windowTitle) || lastOpenedWindow.Equals(""));
        
        if (windowTitle.Equals(PREVIEW))
        {
            lastOpenedWindow = SCENE;
        }
        else if (windowTitle.Equals(SCENE))
        {
            lastOpenedWindow = PREVIEW;
        }
        
        return focusedWindowPresent;
    }
}

#endif
