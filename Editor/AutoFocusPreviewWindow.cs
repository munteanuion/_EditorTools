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
            // Focalizează pe Preview Window
            FocusOnPreviewWindow();
        }
    }

    private static bool IsSelectionInHierarchy(Object selectedObject)
    {
        // Obiectele din Hierarchy sunt de obicei de tip GameObject
        return selectedObject is GameObject;
    }

    private static bool IsSelectionInProjectWindow(Object selectedObject)
    {
        // Verificăm dacă selecția vine din Project Browser
        return AssetDatabase.Contains(selectedObject);
    }

    private static void FocusOnSceneView()
    {
        // Utilizează comanda din meniu pentru a focaliza pe Scene View
        EditorApplication.ExecuteMenuItem("Window/General/Scene");
        Debug.Log("Focalizat pe Scene View.");
    }

    private static void FocusOnPreviewWindow()
    {
        var previewWindow = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.PreviewWindow"));
        if (previewWindow != null)
        {
            previewWindow.Focus();
            Debug.Log("Focalizat pe Preview Window.");
        }
    }
}

#endif
