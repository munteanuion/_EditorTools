using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace ImpRock.JumpTo
{
	internal delegate void SerializeProjectLinksDelegate(FileFormat fileFormat, StreamWriter streamWriter, IEnumerable<Object> linkReferences);
	internal delegate void DeserializeProjectLinksDelegate(FileFormat fileFormat, StreamReader streamReader, JumpLinks jumpLinks);
	internal delegate void SerializeHierarchyLinksDelegate(FileFormat fileFormat, StreamWriter streamWriter, IEnumerable<Object> linkReferences);
	internal delegate void DeserializeHierarchyLinksDelegate(FileFormat fileFormat, StreamReader streamReader, JumpLinks jumpLinks, Scene scene);


	//TODO: all auto-saving of hierarchy links is disabled. not enough info from unity editor to make it work.
	internal sealed class SerializationControl
	{
		private JumpToEditorWindow m_Window = null;
		private FileFormat m_CurrentFileFormat;


		public FileFormat CurrentFileFormat => m_CurrentFileFormat;


		public void Initialize(JumpToEditorWindow window)
		{
			m_CurrentFileFormat.FileVersion = new System.Version(2, 1, 0, 0);
			m_CurrentFileFormat.SavePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "JumpTo"));
			m_CurrentFileFormat.HierarchySavePath = Path.Combine(m_CurrentFileFormat.SavePath, "HierarchyLinks");
			m_CurrentFileFormat.ProjectLinksFileName = "projectlinks";
			m_CurrentFileFormat.FileExtension = "jumpto";

			m_Window = window;

			SceneSaveDetector.OnSceneWillSave += OnSceneWillSave;
			SceneSaveDetector.OnSceneDeleted += OnSceneDeleted;
			SceneStateMonitor.OnSceneOpened += OnSceneOpened;

			foreach (SceneState sceneState in SceneStateMonitor.Instance.GetSceneStates())
			{
				sceneState.OnIsLoadedChange += OnSceneLoadedChange;
				sceneState.OnClose += OnSceneClosed;
			}
		}

		public void Uninitialize()
		{
			SceneSaveDetector.OnSceneWillSave -= OnSceneWillSave;
			SceneSaveDetector.OnSceneDeleted -= OnSceneDeleted;
			SceneStateMonitor.OnSceneOpened -= OnSceneOpened;

			foreach (SceneState sceneState in SceneStateMonitor.Instance.GetSceneStates())
			{
				sceneState.OnIsLoadedChange -= OnSceneLoadedChange;
				sceneState.OnClose -= OnSceneClosed;
			}
		}

		/// <summary>
		/// Called from SceneSaveDetector
		/// </summary>
		/// <param name="sceneAssetPath"></param>
		private void OnSceneWillSave(string sceneAssetPath)
		{
			SaveProjectLinks();

			//NOTE: can't save hierarchy links
			//objects that are new in the scene have not been
			//	assigned a localidinfile at this point, so they
			//	will not save.
		}

		/// <summary>
		/// Called from SceneSaveDetector
		/// </summary>
		/// <param name="sceneAssetPath"></param>
		private void OnSceneDeleted(string sceneAssetPath)
		{
			string sceneGuid = AssetDatabase.AssetPathToGUID(sceneAssetPath);
			string filePath = m_CurrentFileFormat.GetHierarchyLinkFilePath(sceneGuid);
			DeleteSaveFile(filePath);
		}

		private void OnSceneOpened(SceneState sceneState)
		{
			sceneState.OnIsLoadedChange += OnSceneLoadedChange;
			sceneState.OnClose += OnSceneClosed;

			if (sceneState.IsLoaded)
			{
				LoadHierarchyLinks(sceneState.SceneId, sceneState.Scene);
			}
		}
		
		private void OnSceneLoadedChange(SceneState sceneState, bool oldIsLoaded)
		{
			if (sceneState.IsLoaded)
			{
				LoadHierarchyLinks(sceneState.SceneId, sceneState.Scene);
			}
		}

		private void OnSceneClosed(SceneState sceneState)
		{
			sceneState.OnClose -= OnSceneClosed;
			sceneState.OnIsLoadedChange -= OnSceneLoadedChange;
		}

		public void OnWindowEnable()
		{
			if (m_Window.JumpLinksInstance.ProjectLinks.Links.Count == 0)
			{
				LoadProjectLinks();
			}

			foreach (SceneState sceneState in SceneStateMonitor.Instance.GetSceneStates())
			{
				HierarchyJumpLinkContainer links =
					m_Window.JumpLinksInstance.GetHierarchyJumpLinkContainer(sceneState.SceneId);

				if ((links == null || links.Links.Count == 0) && sceneState.IsLoaded)
					LoadHierarchyLinks(sceneState.SceneId, sceneState.Scene);
			}
		}

		public void OnWindowDisable()
		{
			SaveProjectLinks();

			//TODO: why did i comment this out?
			//if (EditorApplication.currentScene != string.Empty)
			//{
			//	GetHierarchyLinkPaths();
			//	SaveHierarchyLinks(EditorApplication.currentScene);
			//}
		}

		public void OnWindowClose()
		{
			SaveProjectLinks();

			//TODO: why did i comment this out?
			//if (EditorApplication.currentScene != string.Empty)
			//{
			//	GetHierarchyLinkPaths();
			//	SaveHierarchyLinks(EditorApplication.currentScene);
			//}
		}

		public bool SaveProjectLinks()
		{
			if (!m_CurrentFileFormat.CreateSaveDirectories())
				return false;

			bool success = true;

			if (m_Window.JumpLinksInstance.ProjectLinks.Links.Count > 0)
			{
				using (StreamWriter streamWriter = new StreamWriter(m_CurrentFileFormat.GetProjectLinksFilePath()))
				{
					try
					{
						streamWriter.WriteLine(m_CurrentFileFormat.FileVersion.ToString(4));

						SerializeProjectLinksDelegate serializer = FindProjectLinksSerializer(m_CurrentFileFormat.FileVersion);
						if (serializer != null)
							serializer?.Invoke(m_CurrentFileFormat, streamWriter, m_Window.JumpLinksInstance.ProjectLinks.AllLinkReferences);
						else
							Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[0]));
					}
					catch (System.Exception ex)
					{
						success = false;
						Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[0]) + "\n" + ex.Message);
					}
					finally
					{
						streamWriter.Close();
					}
				}
			}
			else
			{
				DeleteSaveFile(m_CurrentFileFormat.GetProjectLinksFilePath());
			}

			return success;
		}

		public bool SaveHierarchyLinks(int sceneId)
		{
			if (!m_CurrentFileFormat.CreateSaveDirectories())
				return false;

			bool success = true;

			string sceneAssetPath = SceneStateMonitor.Instance.GetSceneState(sceneId).Path;
			string sceneGuid = AssetDatabase.AssetPathToGUID(sceneAssetPath);
			if (m_Window.JumpLinksInstance.HierarchyLinks[sceneId].Links.Count > 0)
			{
				using (StreamWriter streamWriter = new StreamWriter(m_CurrentFileFormat.GetHierarchyLinkFilePath(sceneGuid)))
				{
					try
					{
						streamWriter.WriteLine(m_CurrentFileFormat.FileVersion.ToString(4));

						SerializeHierarchyLinksDelegate serializer = FindHierarchyLinksSerializer(m_CurrentFileFormat.FileVersion);
						if (serializer != null)
							serializer?.Invoke(m_CurrentFileFormat, streamWriter, m_Window.JumpLinksInstance.HierarchyLinks[sceneId].AllLinkReferences);
						else
							Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[1]));
					}
					catch (System.Exception ex)
					{
						success = false;
						Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[1]) + "\n" + ex.Message);
					}
					finally
					{
						streamWriter.Close();
					}
				}
			}
			else
			{
				DeleteSaveFile(m_CurrentFileFormat.GetHierarchyLinkFilePath(sceneGuid));
			}

			return success;
		}
		
		private void LoadProjectLinks()
		{
			string filePath = m_CurrentFileFormat.GetProjectLinksFilePath();
			if (!File.Exists(filePath))
				return;

			ProjectJumpLinkContainer links = m_Window.JumpLinksInstance.ProjectLinks;
			links.RemoveAll();

			using (StreamReader streamReader = new StreamReader(filePath))
			{
				try
				{
					m_Window.CurrentOperation |= Operation.LoadingProjectLinks;

					if (streamReader.EndOfStream)
						return;

					string readVersion = streamReader.ReadLine();

					System.Version fileVersion;
					if (System.Version.TryParse(readVersion, out fileVersion))
					{
						DeserializeProjectLinksDelegate deserializer = FindProjectLinksDeserializer(fileVersion);
						if (deserializer != null)
						{
							FileFormat fileFormat = m_CurrentFileFormat;
							fileFormat.FileVersion = fileVersion;
							deserializer(fileFormat, streamReader, m_Window.JumpLinksInstance);
						}
						else
						{
							Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[2]));
						}
					}
					else
					{
						Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[2]));
					}
				}
				catch (System.Exception ex)
				{
					Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[3]) + "\n" + ex.Message);
				}
				finally
				{
					streamReader.Close();
					m_Window.CurrentOperation &= ~Operation.LoadingProjectLinks;
				}
			}
		}

		private void LoadHierarchyLinks(int sceneId, Scene scene)
		{
			string sceneGuid = AssetDatabase.AssetPathToGUID(scene.path);
			string filePath = m_CurrentFileFormat.GetHierarchyLinkFilePath(sceneGuid);
			if (!File.Exists(filePath))
				return;

			HierarchyJumpLinkContainer links = m_Window.JumpLinksInstance.AddHierarchyJumpLinkContainer(sceneId);
			links.RemoveAll();

			using (StreamReader streamReader = new StreamReader(filePath))
			{
				try
				{
					if (streamReader.EndOfStream)
						return;

					m_Window.CurrentOperation |= Operation.LoadingHierarchyLinks;

					string readVersion = streamReader.ReadLine();
					System.Version fileVersion;
					if (System.Version.TryParse(readVersion, out fileVersion))
					{
						DeserializeHierarchyLinksDelegate deserializer = FindHierarchyLinkDeserializer(fileVersion);
						if (deserializer != null)
						{
							FileFormat fileFormat = m_CurrentFileFormat;
							fileFormat.FileVersion = fileVersion;
							deserializer(fileFormat, streamReader, m_Window.JumpLinksInstance, scene);
						}
						else
						{
							Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[4]));
						}
					}
					else
					{
						Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[4]));
					}
				}
				catch (System.Exception ex)
				{
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[5]);
					Debug.LogError(string.Format(logFormat, scene.name) + "\n" + ex.Message);
				}
			}

			m_Window.CurrentOperation &= ~Operation.LoadingHierarchyLinks;

			m_Window.Repaint();
		}
		
		private static SerializeProjectLinksDelegate FindProjectLinksSerializer(System.Version fileVersion)
		{
			if (fileVersion.Major >= 2)
			{
				if (fileVersion.Minor == 0)
					return null;
				else if (fileVersion.Minor == 1)
					return LinksFile_2_1_0_0.SerializeProjectLinks;
			}

			return null;
		}

		private DeserializeProjectLinksDelegate FindProjectLinksDeserializer(System.Version fileVersion)
		{
			if (fileVersion.Major >= 2)
			{
				if (fileVersion.Minor == 0)
					return null;
				else if (fileVersion.Minor == 1)
					return LinksFile_2_1_0_0.DeserializeProjectLinks;
			}

			return null;
		}

		private static SerializeHierarchyLinksDelegate FindHierarchyLinksSerializer(System.Version fileVersion)
		{
			if (fileVersion.Major >= 2)
			{
				if (fileVersion.Minor == 0)
					return null;
				else if (fileVersion.Minor == 1)
					return LinksFile_2_1_0_0.SerializeHierarchyLinks;
			}

			return null;
		}

		private DeserializeHierarchyLinksDelegate FindHierarchyLinkDeserializer(System.Version fileVersion)
		{
			if (fileVersion.Major >= 2)
			{
				if (fileVersion.Minor == 0)
					return null;
				else if (fileVersion.Minor == 1)
					return LinksFile_2_1_0_0.DeserializeHierarchyLinks;
			}

			return null;
		}

		private static bool DeleteSaveFile(string filePath)
		{
			bool deleted = true;

			if (File.Exists(filePath))
			{
				try
				{
					File.Delete(filePath);
				}
				catch (PathTooLongException)
				{
					deleted = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[14]);
					Debug.LogError(string.Format(logFormat, filePath));
				}
				catch (IOException)
				{
					deleted = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[15]);
					Debug.LogError(string.Format(logFormat, filePath));
				}
				catch (System.UnauthorizedAccessException)
				{
					deleted = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[16]);
					Debug.LogError(string.Format(logFormat, filePath));
				}
				catch (System.ArgumentException)
				{
					deleted = false;
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[17]);
					Debug.LogError(string.Format(logFormat, filePath));
				}
			}

			return deleted;
		}
	}
}

