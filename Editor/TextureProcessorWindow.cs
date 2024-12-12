#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class TextureProcessorWindow : EditorWindow
{
    private Vector2 scrollPosition = Vector2.zero;
    private string folderPath = "Assets/Textures";
    private List<string> nameFilters = new List<string>(); // Listă pentru filtrarea după nume

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
                propertiesToApply = new Dictionary<string, bool>
                {
                    { "Size", false },
                    { "Compression", false },
                    { "Readable", false },
                    { "sRGB", false },
                    { "Alpha Source", false },
                    { "Alpha Is Transparency", false }
                },
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

        // Secțiune pentru filtrul de nume
        GUILayout.Label("Name Filters (macar una daca contine va procesa textura) ", EditorStyles.boldLabel);
        if (GUILayout.Button("Add Filter"))
        {
            nameFilters.Add(""); // Adaugă un element gol în listă
        }
        for (int i = 0; i < nameFilters.Count; i++)
        {
            GUILayout.BeginHorizontal();
            nameFilters[i] = EditorGUILayout.TextField($"Filter {i + 1}:", nameFilters[i]);
            if (GUILayout.Button("Remove"))
            {
                nameFilters.RemoveAt(i);
                i--; // Ajustează indexul pentru a preveni salturi
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        foreach (TextureType type in new List<TextureType>(textureSettings.Keys))
        {
            GUILayout.Label($"Settings for {type}:", EditorStyles.boldLabel);

            textureSettings[type].skipTheseTextures = EditorGUILayout.Toggle("Skip These:", textureSettings[type].skipTheseTextures);

            GUILayout.Label("Select Properties to Apply:", EditorStyles.boldLabel);
            foreach (var key in new List<string>(textureSettings[type].propertiesToApply.Keys))
            {
                textureSettings[type].propertiesToApply[key] = EditorGUILayout.Toggle(key, textureSettings[type].propertiesToApply[key]);
            }

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(5);

            if (textureSettings[type].propertiesToApply["Size"])
                textureSettings[type].size = (TextureSize)EditorGUILayout.EnumPopup("Size:", textureSettings[type].size);
            if (textureSettings[type].propertiesToApply["Compression"])
                textureSettings[type].compression = (TextureCompression)EditorGUILayout.EnumPopup("Compression:", textureSettings[type].compression);
            if (textureSettings[type].propertiesToApply["Readable"])
                textureSettings[type].textureIsRead = (TextureIsRead)EditorGUILayout.EnumPopup("Is Readable:", textureSettings[type].textureIsRead);
            if (textureSettings[type].propertiesToApply["sRGB"])
                textureSettings[type].sRGB = EditorGUILayout.Toggle("sRGB:", textureSettings[type].sRGB);
            if (textureSettings[type].propertiesToApply["Alpha Source"])
                textureSettings[type].alphaSource = (TextureImporterAlphaSource)EditorGUILayout.EnumPopup("Alpha Source:", textureSettings[type].alphaSource);
            if (textureSettings[type].propertiesToApply["Alpha Is Transparency"])
                textureSettings[type].alphaIsTransparency = EditorGUILayout.Toggle("Alpha Is Transparency:", textureSettings[type].alphaIsTransparency);

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
        if (string.IsNullOrEmpty(folderPath) || !System.IO.Directory.Exists(folderPath))
        {
            Debug.LogError("Invalid folder path. Please provide a valid path.");
            return;
        }

        string[] textureGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        Debug.Log($"Found {textureGUIDs.Length} textures in folder: {folderPath}");

        if (textureGUIDs.Length == 0)
        {
            Debug.LogWarning("No textures found in the specified folder.");
            return;
        }

        AssetDatabase.StartAssetEditing();

        try
        {
            for (int i = 0; i < textureGUIDs.Length; i++)
            {
                string textureGUID = textureGUIDs[i];
                string texturePath = AssetDatabase.GUIDToAssetPath(textureGUID);
                TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;

                if (textureImporter != null)
                {
                    string textureName = System.IO.Path.GetFileNameWithoutExtension(texturePath);


                    if (nameFilters.Count > 0)
                    {
                        // Verifică dacă numele texturii conține vreun cuvânt din filtru
                        bool matchesFilter = nameFilters.Exists(filter => textureName.Contains(filter));

                        if (!matchesFilter) continue; // Dacă nu trece filtrul, sari peste textură
                    }

                    TextureType textureType = DetermineTextureType(textureImporter);
                    TextureSettings settings = textureSettings[textureType];

                    if (settings.skipTheseTextures) continue;

                    if (settings.propertiesToApply["Size"])
                        textureImporter.maxTextureSize = (int)settings.size;
                    if (settings.propertiesToApply["Readable"])
                        textureImporter.isReadable = settings.textureIsRead == TextureIsRead.Enabled;
                    if (settings.propertiesToApply["sRGB"])
                        textureImporter.sRGBTexture = settings.sRGB;
                    if (settings.propertiesToApply["Alpha Source"])
                        textureImporter.alphaSource = settings.alphaSource;
                    if (settings.propertiesToApply["Alpha Is Transparency"])
                        textureImporter.alphaIsTransparency = settings.alphaIsTransparency;

                    if (settings.propertiesToApply["Compression"])
                    {
                        if (settings.compression == TextureCompression.Compressed)
                        {
                            textureImporter.textureCompression = TextureImporterCompression.Compressed;
                            textureImporter.crunchedCompression = (settings.compressionType == TextureCompressionType.Crunch);
                        }
                        else
                        {
                            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                        }
                    }

                    EditorUtility.SetDirty(textureImporter);
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
                }

                EditorUtility.DisplayProgressBar("Processing Textures", $"Processing texture {i + 1}/{textureGUIDs.Length}", (float)i / textureGUIDs.Length);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            Debug.Log("Texture processing complete for folder: " + folderPath);
        }
    }

    private TextureType DetermineTextureType(TextureImporter importer)
    {
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
        public Dictionary<string, bool> propertiesToApply;
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
