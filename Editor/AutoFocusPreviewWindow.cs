#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoFocusPreviewWindow
{
    static AutoFocusPreviewWindow()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    private static void OnSelectionChanged()
    {
        // Obține obiectul selectat
        Object selectedObject = Selection.activeObject;

        if (selectedObject == null) return;

        // Verifică dacă suntem în modul Play
        if (!EditorApplication.isPlaying)
        {
            // Dacă obiectul provine din Hierarchy, focalizează pe Scene View
            if (IsSelectionInHierarchy(selectedObject))
            {
                FocusOnSceneView();
                return;
            }
        }

        // Verifică dacă selecția este din Project Browser
        if (IsSelectionInProjectWindow(selectedObject))
        {
            // Focalizează pe Preview Window doar dacă nu este un folder
            if (!IsFolder(selectedObject))
            {
                FocusOnPreviewWindow();
            }
        }
    }

    private static bool IsSelectionInHierarchy(Object selectedObject)
    {
        // Obiectele din Hierarchy sunt de obicei de tip GameObject
        return selectedObject is GameObject && !AssetDatabaseContains(selectedObject);
    }
    
    private static bool AssetDatabaseContains(Object selectedObject)
    {
        // Obține calea asset-ului
        string assetPath = AssetDatabase.GetAssetPath(selectedObject);
    
        // Verifică dacă calea este validă și nu este goală
        return !string.IsNullOrEmpty(assetPath) && !AssetDatabase.IsValidFolder(assetPath);
    }


    private static bool IsSelectionInProjectWindow(Object selectedObject)
    {
        // Verificăm dacă selecția vine din Project Browser
        return AssetDatabaseContains(selectedObject);
    }

    private static bool IsFolder(Object selectedObject)
    {
        // Verifică dacă obiectul selectat este un folder
        string path = AssetDatabase.GetAssetPath(selectedObject);
        return AssetDatabase.IsValidFolder(path);
    }

    private static void FocusOnSceneView()
    {
        // Utilizează comanda din meniu pentru a focaliza pe Scene View
        EditorApplication.ExecuteMenuItem("Window/General/Scene");
    }

    private static void FocusOnPreviewWindow()
    {
        var previewWindow = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.PreviewWindow"));
        if (previewWindow != null)
        {
            previewWindow.Focus();
        }
    }
}

#endif
