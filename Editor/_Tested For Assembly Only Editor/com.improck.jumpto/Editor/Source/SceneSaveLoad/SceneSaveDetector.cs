using UnityEditor;
using UnityEngine;
using System.IO;


namespace ImpRock.JumpTo
{
	/// <summary>
	/// Attempts to detect when scenes are saved as assets in the project.
	/// </summary>
	internal sealed class SceneSaveDetector : UnityEditor.AssetModificationProcessor
	{
		public static event System.Action<string> OnSceneWillSave;
		public static event System.Action<string> OnSceneSaved;
		public static event System.Action<string> OnSceneWillDelete;
		public static event System.Action<string> OnSceneDeleted;


		public static string[] OnWillSaveAssets(string[] assetPaths)
		{
			//linear search for scenes asset within the paths
			for (int i = 0; i < assetPaths.Length; i++)
			{
				if (assetPaths[i].EndsWith(".unity"))
				{
					//signal that a scene is about to be saved
					SceneWillSave(assetPaths[i]);
				}
			}

			//return the asset paths without any modifications
			return assetPaths;
		}

		public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
		{
			if (assetPath.EndsWith(".unity"))
			{
				SceneWillDelete(assetPath);
			}

			//this means "i did not delete the file manually," in
			//	which case Unity will continue to delete the file
			//	as normal
			return AssetDeleteResult.DidNotDelete;
		}

		private static void SceneWillSave(string sceneAssetPath)
		{
			OnSceneWillSave?.Invoke(sceneAssetPath);

			if (OnSceneSaved != null)
			{
				EditorApplication.delayCall +=
					delegate ()
					{
						OnSceneSaved(sceneAssetPath);
					};
			}
		}

		private static void SceneWillDelete(string sceneAssetPath)
		{
			OnSceneWillDelete?.Invoke(sceneAssetPath);

			if (OnSceneDeleted != null)
			{
				EditorApplication.delayCall +=
					delegate ()
					{
						OnSceneDeleted(sceneAssetPath);
					};
			}
			//only do this if JumpTo isn't open because it handles it internally
			else if (!JumpToEditorWindow.IsOpen())
			{
				EditorApplication.delayCall +=
					delegate()
					{
						//TODO: make this prettier and more reliable
						string jumpToPath = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "JumpTo" + Path.DirectorySeparatorChar;
						string hierarchyLinksPath = jumpToPath + "HierarchyLinks" + Path.DirectorySeparatorChar;
						string guid = AssetDatabase.AssetPathToGUID(sceneAssetPath);
						string saveFilePath = hierarchyLinksPath + guid + ".jumpto";

						DeleteFile(saveFilePath);
					};
			}
		}

		private static bool DeleteFile(string filePath)
		{
			bool deleted = true;

			if (File.Exists(filePath))
			{
				try
				{
					File.Delete(filePath);
				}
				catch (System.Exception)
				{
					//TODO: localize this?
					Debug.LogError("JumpTo: Caught an exception while trying to delete a file\n" + filePath);
				}
			}

			return deleted;
		}
	}
}
