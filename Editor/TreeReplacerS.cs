using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

// Replaces Unity terrain trees with prefab GameObject.
// http://answers.unity3d.com/questions/723266/converting-all-terrain-trees-to-gameobjects.html
[ExecuteInEditMode]
public class TreeReplacerS : EditorWindow
{

    [Header("Settings")]

    [Header("References")]
    public Terrain _terrain;

    string savedTreesName = "SAVED_TREES_TERRAIN_";
    string convertedParentName = "CONVERTED_TREES_TERRAIN_";


    [MenuItem("Tools/Terrain/TreeReplacer")]
    static void Init()
    {
        TreeReplacerS window = (TreeReplacerS)GetWindow(typeof(TreeReplacerS));
    }

    void OnGUI()
    {
        // _terrain = (Terrain)EditorGUILayout.ObjectField(_terrain, typeof(Terrain), true);

        GUILayout.Label("Create");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Convert (Clear Previous)", GUILayout.Height(40f)))
        {
            ClearAll();
            ConvertAll();
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear AllTerrainTreeInstances", GUILayout.Height(40f)))
        {
            ClearAllTerrainTreeInstances();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.green;

        if (GUILayout.Button("Save (Current Trees)", GUILayout.Height(40f)))
        {
            Savetrees();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.red;

        if (GUILayout.Button("Delete (Converted Trees)", GUILayout.Height(40f)))
        {
            DeleteConvertedTrees();
        }

        if (GUILayout.Button("Delete (Saved Trees)", GUILayout.Height(40f)))
        {
            DeleteSavedTrees();
        }
        GUILayout.EndHorizontal();

    }

    public void Convert(Terrain terrain, string parentName)
    {
        TerrainData data = terrain.terrainData;
        float width = data.size.x;
        float height = data.size.z;
        float y = data.size.y;

        // Create parent
        GameObject parent = GameObject.Find(parentName);

        if (parent == null)
        {
            parent = new GameObject(parentName);
        }

        parent.transform.parent = terrain.transform;
        parent.transform.localPosition = Vector3.zero;

        // Create trees
        for (int i = 0; i < data.treeInstances.Length; i++)
        {
            TreeInstance tree = data.treeInstances[i];
            GameObject _tree = data.treePrototypes[tree.prototypeIndex].prefab;

            //Vector3 position = new Vector3(tree.position.x * width, tree.position.y * y, tree.position.z * height);
            Vector3 position = Vector3.Scale(tree.position, data.size) + terrain.transform.position;

            // Instantiate as Prefab if is one, if not, instantiate as normal
            GameObject go = PrefabUtility.InstantiatePrefab(_tree) as GameObject;

            if (go != null)
            {
                go.name += " (" + i.ToString() + ")";
                go.transform.position = position;
                go.transform.parent = parent.transform;
            }
            else
            {
                Instantiate(_tree, position, Quaternion.identity, parent.transform);
            }

            Transform treeTransform = go.transform;
            // set correct Scale of tree instance
            treeTransform.localScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
            // set random rotation
            RotateTrees(treeTransform.transform);
            // set correct slope of tree
            RotateToMatchTerrainSlope(treeTransform, terrain);

        }

    }
    public void ConvertAll()
    {
        Terrain[] terrains = GetAllTerrains();

        for (int i = 0; i < terrains.Length; i++)
        {
            Convert(terrains[i], (convertedParentName + i));
        }
    }

    Terrain[] GetAllTerrains()
    {
        return GameObject.FindObjectsOfType<Terrain>();
    }

    public void ClearAll()
    {
        List<GameObject> allGeneratedTrees = new List<GameObject>();

        for (int i = GetAllTerrains().Length - 1; i >= 0; i--)
        {
            allGeneratedTrees.Add(GameObject.Find("TREES_GENERATED" + i));
        }

        for (int i = allGeneratedTrees.Count - 1; i >= 0; i--)
        {
            GameObject parentObj = allGeneratedTrees[i];
            allGeneratedTrees.Remove(parentObj);
            DestroyImmediate(parentObj);

        }
    }

    public void ClearAllTerrainTreeInstances()
    {
        Terrain[] terrains = GetAllTerrains();

        for (int i = 0; i < terrains.Length; i++)
        {
            terrains[i].terrainData.treeInstances = new List<TreeInstance>().ToArray();
        }
    }
    public void Savetrees()
    {
        Terrain[] terrains = GetAllTerrains();


        for (int i = 0; i < terrains.Length; i++)
        {
            GameObject parent = GameObject.Find(convertedParentName + i);

            if (parent != null)
                parent.name = savedTreesName + i;
            else
            {
                Debug.Log("Cant find Converted Trees");
            }
        }
    }

    public void DeleteConvertedTrees()
    {
        Terrain[] terrains = GetAllTerrains();

        for (int i = 0; i < terrains.Length; i++)
        {
            GameObject parent = GameObject.Find(convertedParentName + i);

            DestroyImmediate(parent);
        }
    }
    public void DeleteSavedTrees()
    {
        Terrain[] terrains = GetAllTerrains();


        for (int i = 0; i < terrains.Length; i++)
        {
            GameObject parent = GameObject.Find(savedTreesName + i);

            DestroyImmediate(parent);
        }
    }
    void RotateTrees(Transform target)
    {
        float randomRotation = Random.Range(0, 360);

        target.transform.RotateAround(target.transform.position, target.transform.up, randomRotation);
    }
    void RotateToMatchTerrainSlope(Transform target, Terrain terrain)
    {
        RaycastHit hit;
        if (Physics.Raycast(target.position, Vector3.down, out hit))
        {
            target.rotation = Quaternion.FromToRotation(terrain.transform.up, hit.normal) * target.rotation;
        }
    }
}