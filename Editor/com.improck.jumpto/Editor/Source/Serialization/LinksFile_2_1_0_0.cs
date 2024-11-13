using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace ImpRock.JumpTo
{
	//Project Line Format
	//	assetguid[|instanceId]
	//
	//	assetguid = GUID of asset in the DB
	//	instanceId = (optional) instance ID of child within the asset

	//Hierarchy Line Format
	//	prefabtype|localId|siblingindexpath|transformpath
	//
	//	prefabtype = PrefabType int
	//	localId = LocalIdentfierInFile OR prefab object ID
	//	siblingindexpath = child index path to the object from root OR from prefab root
	//	transformpath = name path to the object from root OR from prefab root

	internal static class LinksFile_2_1_0_0
	{
		public static void SerializeProjectLinks(FileFormat fileFormat, StreamWriter streamWriter, IEnumerable<Object> linkReferences)
		{
			int instanceId;
			string line;
			foreach (Object linkReference in linkReferences)
			{
				instanceId = linkReference.GetInstanceID();
				line = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(instanceId));
				if (string.IsNullOrWhiteSpace(line))
					continue;

				if (AssetDatabase.IsSubAsset(instanceId))
					line += "|" + instanceId;

				streamWriter.WriteLine(line);
			}
		}

		public static void DeserializeProjectLinks(FileFormat fileFormat, StreamReader streamReader, JumpLinks jumpLinks)
		{
			int instanceId;
			string line;
			string path;
			while (!streamReader.EndOfStream)
			{
				line = streamReader.ReadLine();
				if (line.Length == 32)
				{
					path = AssetDatabase.GUIDToAssetPath(line);
					if (!string.IsNullOrWhiteSpace(path))
					{
						Object obj = AssetDatabase.LoadMainAssetAtPath(path);
						if (obj != null)
							jumpLinks.CreateOnlyProjectJumpLink(obj);
					}
				}
				else if (line.Length > 33 && line[32] == '|')
				{
					instanceId = int.Parse(line.Substring(33));
					path = AssetDatabase.GUIDToAssetPath(line.Substring(0, 32));
					if (!string.IsNullOrWhiteSpace(path))
					{
						Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
						if (objs != null)
						{
							for (int j = 0; j < objs.Length; j++)
							{
								if (objs[j].GetInstanceID() == instanceId)
									jumpLinks.CreateOnlyProjectJumpLink(objs[j]);
							}
						}
					}
				}
			}
		}

		public static void SerializeHierarchyLinks(FileFormat fileFormat, StreamWriter streamWriter, IEnumerable<Object> linkReferences)
		{
			SerializedObject serializedObject;
			foreach (Object linkReference in linkReferences)
			{
				Object linkReferenceObject = linkReference;
				Transform linkReferenceTransform = (linkReferenceObject as GameObject).transform;

				string paths = string.Empty;

				PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(linkReferenceObject);
				PrefabInstanceStatus prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(linkReferenceObject);
				//if (prefabType == PrefabType.ModelPrefabInstance ||
				//	prefabType == PrefabType.PrefabInstance)
				if (prefabAssetType != PrefabAssetType.NotAPrefab && prefabAssetType != PrefabAssetType.MissingAsset &&
					prefabInstanceStatus == PrefabInstanceStatus.Connected)

				{
					//linkReferenceObject = PrefabUtility.GetPrefabObject(linkReferenceObject);
					linkReferenceObject = PrefabUtility.GetPrefabInstanceHandle(linkReferenceObject);

					//we only want the path up to the prefab instance's root
					GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(linkReferenceTransform.gameObject);
					paths += JumpToUtility.GetSiblingIndexPath(linkReferenceTransform, prefabRoot.transform) + "|" +
						JumpToUtility.GetTransformPath(linkReferenceTransform, prefabRoot.transform);
				}
				else
				{
					paths += JumpToUtility.GetSiblingIndexPath(linkReferenceTransform) + "|" +
						JumpToUtility.GetTransformPath(linkReferenceTransform);
				}

				serializedObject = new SerializedObject(linkReferenceObject);
				serializedObject.SetInspectorMode(InspectorMode.Debug);

				int localId = serializedObject.GetLocalIdInFile();
				if (prefabInstanceStatus == PrefabInstanceStatus.MissingAsset)
				{
					SerializedProperty prefabInternalProperty = serializedObject.FindProperty("m_PrefabInstance");
					if (prefabInternalProperty.objectReferenceValue != null)
					{
						SerializedObject serializedPrefabObject = new SerializedObject(prefabInternalProperty.objectReferenceValue);
						serializedPrefabObject.SetInspectorMode(InspectorMode.Debug);
						SerializedProperty localIdProperty = serializedPrefabObject.FindProperty("m_LocalIdentfierInFile");
						if (localIdProperty != null)
						{
							localId = localIdProperty.intValue;
						}
					}
				}

				streamWriter.WriteLine($"{(int)prefabAssetType}|{(int)prefabInstanceStatus}|{localId}|{paths}");
			}
		}

		public static void DeserializeHierarchyLinks(FileFormat fileFormat, StreamReader streamReader, JumpLinks jumpLinks, Scene scene)
		{
			GameObject[] rootObjects = scene.GetRootGameObjects();
			if (rootObjects.Length > 0)
			{
				Dictionary<int, GameObject> localIdToGameObjects = new Dictionary<int, GameObject>();
				Dictionary<int, GameObject> localIdToPrefabs = new Dictionary<int, GameObject>();
				JumpToUtility.GetAllLocalIds(rootObjects, localIdToGameObjects, localIdToPrefabs);

				string line;
				string transformPath;
				char[] delimiterPipe = new char[] { '|' };
				char[] delimeterForwardSlash = new char[] { '/' };
				string[] lineSegments;
				string[] transformNames;
				while (!streamReader.EndOfStream)
				{
					line = streamReader.ReadLine();
					if (line == null || line.Length == 0)
						continue;

					lineSegments = line.Split(delimiterPipe, System.StringSplitOptions.None);

					if (lineSegments.Length == 0)
						continue;

					if (!int.TryParse(lineSegments[0], out int prefabAssetTypeId))
						continue;
					
					if (!int.TryParse(lineSegments[1], out int prefabInstanceStatusId))
						continue;

					if (!int.TryParse(lineSegments[2], out int localId))
						continue;

					//the localId should NEVER be zero
					if (localId == 0)
						continue;

					PrefabAssetType prefabAssetType = (PrefabAssetType)prefabAssetTypeId;
					PrefabInstanceStatus prefabInstanceStatus = (PrefabInstanceStatus)prefabInstanceStatusId;

					//try to find the object based solely on its localId
					if ((prefabAssetType == PrefabAssetType.NotAPrefab || prefabAssetType == PrefabAssetType.MissingAsset) &&
						(prefabInstanceStatus == PrefabInstanceStatus.NotAPrefab || prefabInstanceStatus == PrefabInstanceStatus.MissingAsset
#if !UNITY_2022_1_OR_NEWER
						|| prefabInstanceStatus == PrefabInstanceStatus.Disconnected
#endif
						))
					{
						if (localIdToGameObjects.TryGetValue(localId, out GameObject gameObject))
							jumpLinks.CreateOnlyHierarchyJumpLink(gameObject);

						//TODO: what if it's not found?
						continue;
					}
					else
					{
						//NOTE: searching for children within prefabs is not reliable because they are not currently 
						//		uniquely addressed within a scene. if a prefab is renamed and moved after its link is
						//		saved, it may not be correctly relinked on load. if a prefab is moved within a scene,
						//		it may not be correctly relinked on load. blame Unity for this.

						//get the root node for the prefab instance
						GameObject gameObject;
						if (localIdToPrefabs.TryGetValue(localId, out gameObject))
						{
							//get names of the path nodes
							transformNames = lineSegments[4].Split(delimeterForwardSlash, System.StringSplitOptions.RemoveEmptyEntries);

							//check for corrupt path
							if (transformNames.Length == 0)
								continue;

							if (transformNames.Length == 1)
							{
								jumpLinks.CreateOnlyHierarchyJumpLink(gameObject);
							}
							else
							{
								transformPath = transformNames[1];
								for (int i = 2; i < transformNames.Length; i++)
								{
									transformPath += "/" + transformNames[i];
								}

								Transform transform = gameObject.transform.Find(transformPath);
								if (transform != null)
								{
									jumpLinks.CreateOnlyHierarchyJumpLink(transform.gameObject);
								}
							}
						}
					}
				}	//while ! end of stream

				localIdToGameObjects.Clear();
				localIdToPrefabs.Clear();
			}	//if root objects exist
		}
	}
}
