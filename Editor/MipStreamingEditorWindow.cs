#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

public class MipStreamingEditorWindow : EditorWindow
{
    private string folderPath = "Assets";
    private bool enableMipStreaming = true;
    private bool enableVirtualTexturing = false;
    private bool omitUITextures = true;
    private int priorityMipStreaming = 0;

    [MenuItem("Tools/Texture/Mip Streaming Settings")]
    public static void ShowWindow()
    {
        GetWindow<MipStreamingEditorWindow>("Mip Streaming Settings");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mip Streaming Configuration", EditorStyles.boldLabel);

        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
        enableMipStreaming = EditorGUILayout.Toggle("Enable Mip Streaming", enableMipStreaming);
        priorityMipStreaming = EditorGUILayout.IntField("Priority Mip Streaming", priorityMipStreaming);
        enableVirtualTexturing = EditorGUILayout.Toggle("Enable Virtual Texturing", enableVirtualTexturing);
        omitUITextures = EditorGUILayout.Toggle("Skip UI Textures", omitUITextures);

        if (GUILayout.Button("Apply Settings"))
        {
            ApplySettings();
        }
    }

    private void ApplySettings()
    {
        string[] textureGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        int totalTextures = textureGUIDs.Length;

        AssetDatabase.StartAssetEditing(); // Start batching changes

        try
        {
            for (int i = 0; i < totalTextures; i++)
            {
                string textureGUID = textureGUIDs[i];
                string texturePath = AssetDatabase.GUIDToAssetPath(textureGUID);
                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;

                if (importer != null && importer.mipmapEnabled)
                {
                    if (omitUITextures && importer.textureType == TextureImporterType.GUI)
                        continue;

                    importer.streamingMipmaps = enableMipStreaming;
                    importer.vtOnly = enableVirtualTexturing;
                    importer.streamingMipmapsPriority = priorityMipStreaming;

                    EditorUtility.SetDirty(importer); // Mark the importer as dirty for saving changes
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
                }

                // Update the progress bar
                EditorUtility.DisplayProgressBar("Applying Mip Settings", $"Processing texture {i + 1}/{totalTextures}", (float)i / totalTextures);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing(); // Stop batching changes
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar(); // Clear the progress bar
            Debug.Log("Mip Streaming settings applied.");
        }
    }
}

#endif
