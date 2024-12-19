#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialFinder
{
    [MenuItem("Tools/Replace Shader in Materials")]
    public static void ReplaceShaderInMaterials()
    {
        // Shader-ul de căutat
        Shader targetShader = Shader.Find("Universal Render Pipeline/Lit");
        // Shader-ul înlocuitor
        Shader replacementShader = Shader.Find("Universal Render Pipeline/Lit Custom");

        if (targetShader == null || replacementShader == null)
        {
            Debug.LogError("Shader not found! Ensure both target and replacement shaders exist.");
            return;
        }

        // Lista de materiale găsite
        List<Object> materialsUsingShader = new List<Object>();

        string[] guids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader == targetShader)
            {
                Debug.Log($"Material using {targetShader.name}: {path}");

                // Verifică dacă materialul este read-only
                if (AssetDatabase.IsOpenForEdit(mat))
                {
                    // Modifică shader-ul direct
                    mat.shader = replacementShader;
                }
                else
                {
                    // Creează o copie a materialului
                    string newPath = path.Replace(".mat", "_Modified.mat");
                    Material newMaterial = new Material(mat);
                    newMaterial.shader = replacementShader;

                    AssetDatabase.CreateAsset(newMaterial, newPath);
                    Debug.Log($"Created modified material: {newPath}");
                }

                materialsUsingShader.Add(mat);
            }
        }

        // Selectează materialele modificate în Project View
        if (materialsUsingShader.Count > 0)
        {
            Selection.objects = materialsUsingShader.ToArray();
            Debug.Log($"Processed {materialsUsingShader.Count} materials. Check Project View for duplicates.");
        }
        else
        {
            Debug.LogWarning($"No materials found using the shader: {targetShader.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

#endif
