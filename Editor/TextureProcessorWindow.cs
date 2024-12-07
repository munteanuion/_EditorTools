#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class TextureProcessorWindow : EditorWindow
{
    private Vector2 scrollPosition = Vector2.zero;
    private string folderPath = "Assets/Textures"; // Calea implicită

    private Dictionary<TextureType, TextureSettings> textureSettings;

    [MenuItem("Tools/Texture/Texture Processor")]
    private static void ShowWindow()
    {
        TextureProcessorWindow window = GetWindow<TextureProcessorWindow>();
        window.titleContent = new GUIContent("Texture Processor");
        window.Show();
    }

    private void OnEnable()
    {
        textureSettings = new Dictionary<TextureType, TextureSettings>();
        foreach (TextureType type in System.Enum.GetValues(typeof(TextureType)))
        {
            textureSettings[type] = new TextureSettings
            {
                skipTheseTextures = true,
                size = TextureSize._1024x1024,
                compression = TextureCompression.Compressed,
                compressionType = TextureCompressionType.Normal,
                textureIsRead = TextureIsRead.Disabled,
                sRGB = true,
                alphaSource = TextureImporterAlphaSource.FromInput,
                alphaIsTransparency = false
            };
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Texture Processor", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Descriere:", EditorStyles.boldLabel);
        GUILayout.Label("Acest tool permite procesarea și ajustarea texturilor în proiectul tău Unity.");

        GUILayout.Label("Folder Path", EditorStyles.boldLabel);
        folderPath = EditorGUILayout.TextField("Path:", folderPath);

        GUILayout.Space(10);

        foreach (TextureType type in System.Enum.GetValues(typeof(TextureType)))
        {
            GUILayout.Label($"Settings for {type}:", EditorStyles.boldLabel);

            textureSettings[type].skipTheseTextures = EditorGUILayout.Toggle("Skip These:", textureSettings[type].skipTheseTextures);
            textureSettings[type].size = (TextureSize)EditorGUILayout.EnumPopup("Size:", textureSettings[type].size);
            textureSettings[type].compression = (TextureCompression)EditorGUILayout.EnumPopup("Compression:", textureSettings[type].compression);
            textureSettings[type].textureIsRead = (TextureIsRead)EditorGUILayout.EnumPopup("Is Readable:", textureSettings[type].textureIsRead);
            textureSettings[type].sRGB = EditorGUILayout.Toggle("sRGB:", textureSettings[type].sRGB);
            textureSettings[type].alphaSource = (TextureImporterAlphaSource)EditorGUILayout.EnumPopup("Alpha Source:", textureSettings[type].alphaSource);
            textureSettings[type].alphaIsTransparency = EditorGUILayout.Toggle("Alpha Is Transparency:", textureSettings[type].alphaIsTransparency);

            if (textureSettings[type].compression == TextureCompression.Compressed)
            {
                textureSettings[type].compressionType = (TextureCompressionType)EditorGUILayout.EnumPopup("Compression Type:", textureSettings[type].compressionType);
            }

            GUILayout.Space(10);
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        if (GUILayout.Button("Process Textures"))
        {
            ProcessTextures(folderPath);
        }
    }

    private void ProcessTextures(string folderPath)
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
                TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;

                if (textureImporter != null)
                {
                    TextureType textureType = DetermineTextureType(textureImporter);

                    TextureSettings settings = textureSettings[textureType];

                    if (settings.skipTheseTextures) continue;

                    textureImporter.isReadable = settings.textureIsRead == TextureIsRead.Enabled;
                    textureImporter.maxTextureSize = (int)settings.size;
                    textureImporter.sRGBTexture = settings.sRGB;
                    textureImporter.alphaSource = settings.alphaSource;
                    textureImporter.alphaIsTransparency = settings.alphaIsTransparency;

                    if (settings.compression == TextureCompression.Compressed)
                    {
                        textureImporter.textureCompression = TextureImporterCompression.Compressed;
                        textureImporter.compressionQuality = 50;
                        textureImporter.crunchedCompression = (settings.compressionType == TextureCompressionType.Crunch);
                    }
                    else
                    {
                        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    }

                    // Mark the importer as dirty and reimport the texture to apply changes
                    EditorUtility.SetDirty(textureImporter);
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
                }

                // Update the progress bar
                EditorUtility.DisplayProgressBar("Processing Textures", $"Processing texture {i + 1}/{totalTextures}", (float)i / totalTextures);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing(); // Stop batching changes
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar(); // Clear the progress bar
            Debug.Log("Texture processing complete for folder: " + folderPath);
        }
    }

    private TextureType DetermineTextureType(TextureImporter importer)
    {
        // Logic for determining texture type based on the importer
        if (importer.textureType == TextureImporterType.Sprite)
            return TextureType.UI;
        if (importer.textureType == TextureImporterType.NormalMap)
            return TextureType.NormalMap;

        return TextureType.Default;
    }

    private enum TextureSize
    {
        _32x32 = 32,
        _64x64 = 64,
        _128x128 = 128,
        _256x256 = 256,
        _512x512 = 512,
        _1024x1024 = 1024,
        _2048x2048 = 2048,
        _4096x4096 = 4096,
        _8192x8192 = 8192
    }

    private enum TextureCompression
    {
        Compressed,
        Uncompressed
    }

    private enum TextureCompressionType
    {
        Crunch,
        Normal
    }

    private enum TextureIsRead
    {
        Enabled,
        Disabled
    }

    private enum TextureType
    {
        Default,
        UI,
        NormalMap
    }

    private class TextureSettings
    {
        public bool skipTheseTextures = true;
        public TextureSize size;
        public TextureCompression compression;
        public TextureCompressionType compressionType;
        public TextureIsRead textureIsRead;
        public bool sRGB;
        public TextureImporterAlphaSource alphaSource;
        public bool alphaIsTransparency;
    }
}
#endif
