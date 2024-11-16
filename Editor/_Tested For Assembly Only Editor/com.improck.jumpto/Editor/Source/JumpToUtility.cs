using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Text;
using System.Collections.Generic;


namespace ImpRock.JumpTo
{
	internal static class JumpToUtility
	{
		public static string GetRootOrderPath(Transform transform, Transform root = null)
		{
			SerializedObject so = null;
			Stack<string> pathStack = new Stack<string>();
			Transform rootParent = root != null ? root.parent : null;
			while (transform != null && transform != rootParent)
			{
				so = new SerializedObject(transform);
				pathStack.Push("/" + so.FindProperty("m_RootOrder").intValue.ToString());

				transform = transform.parent;
			}

			StringBuilder pathBuilder = new StringBuilder();
			while (pathStack.Count > 0)
			{
				pathBuilder.Append(pathStack.Pop());
			}

			return pathBuilder.ToString();
		}

		public static string GetSiblingIndexPath(Transform transform, Transform root = null)
		{
			Stack<string> pathStack = new Stack<string>();
			Transform rootParent = root != null ? root.parent : null;
			while (transform != null && transform != rootParent)
			{
				pathStack.Push("/" + transform.GetSiblingIndex());

				transform = transform.parent;
			}

			StringBuilder pathBuilder = new StringBuilder();
			while (pathStack.Count > 0)
			{
				pathBuilder.Append(pathStack.Pop());
			}

			return pathBuilder.ToString();
		}

		public static string GetTransformPath(Transform transform, Transform root = null)
		{
			Stack<string> pathStack = new Stack<string>();
			Transform rootParent = root != null ? root.parent : null;
			while (transform != null && transform != rootParent)
			{
				pathStack.Push("/" + transform.name);

				transform = transform.parent;
			}

			StringBuilder pathBuilder = new StringBuilder();
			while (pathStack.Count > 0)
			{
				pathBuilder.Append(pathStack.Pop());
			}

			return pathBuilder.ToString();
		}

		public static void GetAllLocalIds(GameObject[] orderedRootObjects,
			Dictionary<int, GameObject> idToGameObjects, Dictionary<int, GameObject> idToPrefabs)
		{
			idToGameObjects.Clear();
			idToPrefabs.Clear();

			if (orderedRootObjects == null || orderedRootObjects.Length == 0)
				return;

			//Ref: https://discussions.unity.com/t/finding-the-root-gameobjects-in-the-scene/7619/5
			HierarchyProperty hierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
			if (!hierarchyProperty.Find(orderedRootObjects[0].GetInstanceID(), null))
				return;
			
			do
			{
				PrefabInstanceStatus prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(hierarchyProperty.pptrValue);

				if (prefabInstanceStatus == PrefabInstanceStatus.NotAPrefab)
				{
					SerializedObject serializedObject = new SerializedObject(hierarchyProperty.pptrValue);
					serializedObject.SetInspectorMode(InspectorMode.Debug);
					int localId = serializedObject.GetLocalIdInFile();

					//disconnected prefabs will have a localId of 0 if they haven't been saved to the scene
					if (localId != 0)
						idToGameObjects.Add(localId, hierarchyProperty.pptrValue as GameObject);
				}
				else if (prefabInstanceStatus == PrefabInstanceStatus.MissingAsset)
				{
					//the localId will be zero, so dig a bit deeper
					SerializedObject serializedObject = new SerializedObject(hierarchyProperty.pptrValue);
					serializedObject.SetInspectorMode(InspectorMode.Debug);

					SerializedProperty prefabInternalProperty = serializedObject.FindProperty("m_PrefabInstance");
					if (prefabInternalProperty.objectReferenceValue != null)
					{
						SerializedObject serializedPrefabObject = new SerializedObject(prefabInternalProperty.objectReferenceValue);
						serializedPrefabObject.SetInspectorMode(InspectorMode.Debug);
						SerializedProperty localIdProperty = serializedPrefabObject.FindProperty("m_LocalIdentfierInFile");
						if (localIdProperty != null)
						{
							int localId = localIdProperty.intValue;
							if (localId != 0)
								idToGameObjects.Add(localId, hierarchyProperty.pptrValue as GameObject);
						}
					}
				}
				else
				{
					Object prefabObject = PrefabUtility.GetPrefabInstanceHandle(hierarchyProperty.pptrValue);
					SerializedObject serializedObject = new SerializedObject(prefabObject);
					serializedObject.SetInspectorMode(InspectorMode.Debug);
					int localId = serializedObject.GetLocalIdInFile();

					if (localId != 0 && !idToPrefabs.ContainsKey(localId))
					{
						SerializedProperty rootGameObject = serializedObject.FindProperty("m_RootGameObject");
						if (rootGameObject.objectReferenceValue != null)
						{
							prefabObject = rootGameObject.objectReferenceValue;
							idToPrefabs.Add(localId, prefabObject as GameObject);
						}
					}
				}
			}
			while (hierarchyProperty.Next(null) && hierarchyProperty.pptrValue != null);
		}

		public static int FindSceneContaining(Object linkReference)
		{
			Transform linkRoot = (linkReference as GameObject).transform.root;

			int sceneCount = SceneManager.sceneCount;
			List<GameObject> rootObjects = new List<GameObject>();
			for (int i = 0; i < sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.isLoaded)
				{
					//NOTE: this clears the list internally
					scene.GetRootGameObjects(rootObjects);

					foreach (GameObject gameObject in rootObjects)
					{
						if (gameObject.transform.root == linkRoot)
							return scene.GetHashCode();
					}
				}
			}

			return 0;
		}
	}


	internal static class SerializedObjectExtension
	{
		private static PropertyInfo m_InspectorModeProperty = null;


		public static InspectorMode GetInspectorMode(this SerializedObject serializedObject)
		{
			if (m_InspectorModeProperty == null)
			{
				m_InspectorModeProperty =
					typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
			}

			return (InspectorMode)m_InspectorModeProperty.GetValue(serializedObject, null);
		}

		public static void SetInspectorMode(this SerializedObject serializedObject, InspectorMode inspectorMode)
		{
			if (m_InspectorModeProperty == null)
			{
				m_InspectorModeProperty =
					typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
			}

			m_InspectorModeProperty.SetValue(serializedObject, inspectorMode, null);
		}

		public static int GetLocalIdInFile(this SerializedObject serializedObject)
		{
			//NOTE: the property name is actually misspelled in the object!
			SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
			if (localIdProp != null)
				return localIdProp.intValue;

			return -1;
		}
	}
}
