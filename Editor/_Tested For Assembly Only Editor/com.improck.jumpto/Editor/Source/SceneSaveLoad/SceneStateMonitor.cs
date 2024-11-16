using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;


namespace ImpRock.JumpTo
{
	internal sealed class SceneState
	{
		public int SceneId = 0;
		public string Name = string.Empty;
		public string Path = string.Empty;
		public int RootCount = 0;
		public bool IsDirty = false;
		public bool IsLoaded = false;
		public Scene Scene;
		
		
		public event System.Action<SceneState, string> OnNameChange;
		public event System.Action<SceneState, string> OnPathChange;
		public event System.Action<SceneState, int> OnRootCountChange;
		public event System.Action<SceneState, bool> OnIsDirtyChange;
		public event System.Action<SceneState, bool> OnIsLoadedChange;
		public event System.Action<SceneState> OnClose;
		

		public SceneState(Scene scene)
		{
			Scene = scene;
			SceneId = Scene.GetHashCode();
			Name = scene.name;
			Path = scene.path;
			RootCount = scene.rootCount;
			IsDirty = scene.isDirty;
			IsLoaded = scene.isLoaded;
		}

		public void UpdateInfo()
		{
			string oldName = Name;
			string oldPath = Path;
			int oldRootCount = RootCount;
			bool oldIsDirty = IsDirty;
			bool oldIsLoaded = IsLoaded;

			SceneId = Scene.GetHashCode();
			Name = Scene.name;
			Path = Scene.path;
			RootCount = Scene.rootCount;
			IsDirty = Scene.isDirty;
			IsLoaded = Scene.isLoaded;

			if (OnRootCountChange != null && oldRootCount != RootCount)
				OnRootCountChange(this, oldRootCount);
			if (OnIsDirtyChange != null && oldIsDirty != IsDirty)
				OnIsDirtyChange(this, oldIsDirty);
			if (OnIsLoadedChange != null && oldIsLoaded != IsLoaded)
				OnIsLoadedChange(this, oldIsLoaded);
			if (OnPathChange != null && oldPath != Path)
				OnPathChange(this, oldPath);
			if (OnNameChange != null && oldName != Name)
				OnNameChange(this, oldName);
		}

		public void SceneClosed()
		{
			OnClose?.Invoke(this);
		}

		public override string ToString()
		{
			return SceneId + " " + Name + " " + IsLoaded;
		}
	}

	
	[System.Serializable]
	internal sealed class SceneStateMonitor
	{
		#region Pseudo-Singleton
		private static SceneStateMonitor s_Instance = null;

		public static SceneStateMonitor Instance { get { return s_Instance; } }

		public static SceneStateMonitor Create()
		{
			if (s_Instance == null)
				s_Instance = new SceneStateMonitor();

			return s_Instance;
		}


		private SceneStateMonitor() { s_Instance = this; Initialize(); }
		#endregion


		[SerializeField] private int m_SceneCount = 0;
		[SerializeField] private int m_LoadedSceneCount = 0;

		
		private Dictionary<int, SceneState> m_SceneStates = new Dictionary<int, SceneState>();


		public static event System.Action<int, int> OnSceneCountChanged;
		public static event System.Action<int, int> OnLoadedSceneCountChanged;
		public static event System.Action<SceneState> OnSceneOpened;
		

		private static void Initialize() { }

		
		public SceneState GetSceneState(int sceneId)
		{
			SceneState sceneState;
			m_SceneStates.TryGetValue(sceneId, out sceneState);

			return sceneState;
		}

		public SceneState[] GetSceneStates()
		{
			SceneState[] sceneStates = new SceneState[m_SceneStates.Count];
			m_SceneStates.Values.CopyTo(sceneStates, 0);

			return sceneStates;
		}

		public void InitializeSceneStateData()
		{
			EditorApplication.hierarchyChanged += OnHierarchyWindowChanged;

			m_SceneCount = EditorSceneManager.sceneCount;

#if UNITY_2022_2_OR_NEWER
			m_LoadedSceneCount = SceneManager.loadedSceneCount;
#else
			m_LoadedSceneCount = EditorSceneManager.loadedSceneCount;
#endif

			for (int i = 0; i < m_SceneCount; i++)
			{
				Scene scene = EditorSceneManager.GetSceneAt(i);
				m_SceneStates[scene.GetHashCode()] = new SceneState(scene);
			}
		}

		private void OnHierarchyWindowChanged()
		{
			int currentSceneCount = EditorSceneManager.sceneCount;

			//TODO: there has got to be a more efficient way to do this!

			//find newly opened scenes
			bool sceneStateChanged = false;
			int[] currentSceneIds = new int[currentSceneCount];
			for (int i = 0; i < currentSceneCount; i++)
			{
				Scene scene = EditorSceneManager.GetSceneAt(i);
				currentSceneIds[i] = scene.GetHashCode();
				if (!m_SceneStates.ContainsKey(currentSceneIds[i]))
				{
					sceneStateChanged = true;
					SceneState sceneState = new SceneState(scene);
					m_SceneStates[currentSceneIds[i]] = sceneState;
					OnSceneOpened?.Invoke(sceneState);
				}
			}

			//find newly closed scenes
			Dictionary<int, SceneState> currentSceneStates = new Dictionary<int, SceneState>();
			foreach (KeyValuePair<int, SceneState> sceneState in m_SceneStates)
			{
				if (!ArrayUtility.Contains(currentSceneIds, sceneState.Key))
				{
					sceneStateChanged = true;
					sceneState.Value.SceneClosed();
				}
				else
				{
					currentSceneStates.Add(sceneState.Key, sceneState.Value);
				}
			}

			if (sceneStateChanged)
			{
				m_SceneStates = currentSceneStates;
			}

			if (m_SceneCount != currentSceneCount)
			{
				OnSceneCountChanged?.Invoke(m_SceneCount, currentSceneCount);

				m_SceneCount = currentSceneCount;
			}
#if UNITY_2022_2_OR_NEWER
			currentSceneCount = SceneManager.loadedSceneCount;
#else
			currentSceneCount = EditorSceneManager.loadedSceneCount;
#endif
			if (m_LoadedSceneCount != currentSceneCount)
			{
				OnLoadedSceneCountChanged?.Invoke(m_LoadedSceneCount, currentSceneCount);
				
				m_LoadedSceneCount = currentSceneCount;
			}

			UpdateSceneData();
		}
		
		private void UpdateSceneData()
		{
			foreach (KeyValuePair<int, SceneState> sceneStatePair in m_SceneStates)
			{
				sceneStatePair.Value.UpdateInfo();
			}
		}
	}
}
