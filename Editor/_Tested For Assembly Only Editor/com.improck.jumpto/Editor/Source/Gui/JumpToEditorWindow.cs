using UnityEditor;
using UnityEngine;


namespace ImpRock.JumpTo
{
	internal sealed class JumpToEditorWindow : EditorWindow
	{
		private static int s_InstanceCount = 0;


		[SerializeField] private JumpLinks m_JumpLinks;
		[SerializeField] private GuiJumpLinkListView m_JumpLinkListView;
		[SerializeField] private SceneStateMonitor m_SceneStateMonitor;

		//not static because it needs to serialize
		public Operation CurrentOperation = Operation.Idle;

		[System.NonSerialized] private bool m_Initialized = false;
		[System.NonSerialized] private RectRef m_Position = new RectRef();
		[System.NonSerialized] private double m_LastHierarchyRefreshTime = 0.0f;
		[System.NonSerialized] private SerializationControl m_SerializationControl = null;


		public JumpLinks JumpLinksInstance { get { return m_JumpLinks; } }
		public SceneStateMonitor SceneStateMonitorInstance { get { return m_SceneStateMonitor; } }
		public SerializationControl SerializationControlInstance { get { return m_SerializationControl; } }


		//NOTE: not using these right now
		//public static event EditorApplication.CallbackFunction OnWindowOpen;
		//public static event EditorApplication.CallbackFunction OnWillEnable;
		//public static event EditorApplication.CallbackFunction OnWillDisable;
		//public static event EditorApplication.CallbackFunction OnWillClose;


		//called when window is first open
		//called before deserialization due to a compile
		//public JumpToEditorWindow()
		//{
		//	SerializationControl.CreateInstance();

		//	if (OnWindowOpen != null)
		//		OnWindowOpen();
		//}

		//called when window is first open
		//called after deserialization due to a compile
		private void OnEnable()
		{
			s_InstanceCount++;

			JumpToResources.Instance.LoadResources();

			if (m_SceneStateMonitor == null)
			{
				m_SceneStateMonitor = SceneStateMonitor.Create();
			}

			m_SceneStateMonitor.InitializeSceneStateData();

			if (m_JumpLinks == null)
			{
				m_JumpLinks = JumpLinks.Create();
			}

			if (m_JumpLinkListView == null)
			{
				m_JumpLinkListView = GuiBase.Create<GuiJumpLinkListView>();
			}

			EditorApplication.delayCall += OnPostEnable;

			if (m_SerializationControl == null)
			{
				m_SerializationControl = new SerializationControl();
				m_SerializationControl.Initialize(this);
			}

			EditorApplication.projectChanged += OnProjectWindowChange;

			//NOTE: this DOES NOT WORK because closing unity will serialize the
			//		jumpto window if it's open. that means that when unity is
			//		restarted, the m_FirstOpen flag gets deserialized to equal
			//		FALSE.
			//Debug.Log("first open = " + m_FirstOpen);
			//if (m_FirstOpen)
			//{
			//	m_FirstOpen = false;
			//
			//	if (OnWindowOpen != null)
			//		OnWindowOpen();
			//}

			m_SerializationControl.OnWindowEnable();

			RefreshMinSize();

			m_JumpLinks.RefreshProjectLinks();
			m_JumpLinks.RefreshHierarchyLinks();
			m_LastHierarchyRefreshTime = EditorApplication.timeSinceStartup;

			m_JumpLinkListView.OnWindowEnable(this);

			GUIContent titleContent = this.titleContent;
			titleContent.text = "JumpTo";
			titleContent.image = JumpToResources.Instance.GetImage(ResId.ImageTabIcon);
		}

		private void OnPostEnable()
		{
			GUIContent titleContent = this.titleContent;
			titleContent.text = "JumpTo";
			titleContent.image = JumpToResources.Instance.GetImage(ResId.ImageTabIcon);

			EditorApplication.hierarchyChanged += OnHierarchyWindowChange;
		}

		//called before window closes
		//called before serialization due to a compile
		private void OnDisable()
		{
			//NOTE: nobody's using it right now
			//if (OnWillDisable != null)
			//	OnWillDisable();

			m_SerializationControl.OnWindowDisable();

			m_JumpLinkListView.OnWindowDisable(this);

			EditorApplication.projectChanged -= OnProjectWindowChange;
			EditorApplication.hierarchyChanged -= OnHierarchyWindowChange;

			m_Initialized = false;
		}

		//NOT called when unity editor is closed
		//called when window is closed
		private void OnDestroy()
		{
			s_InstanceCount--;

			//NOTE: nobody's using it right now
			//if (OnWillClose != null)
			//	OnWillClose();

			m_SerializationControl.OnWindowClose();

			m_JumpLinkListView.OnWindowClose(this);

			m_SerializationControl.Uninitialize();
		}

		private void Init()
		{
			m_Initialized = true;

			GraphicAssets.Instance.InitGuiStyle();
		}

		private void OnGUI()
		{
			//NOTE: it's ridiculous that I have to do this here, but some
			//		things can only be called from inside OnGUI().
			if (!m_Initialized)
				Init();

			//in 2019.3, the dark theme (aka "pro skin") became available to all licenses
#if !UNITY_2019_3_OR_NEWER && FORCE_PRO_SKIN
#pragma warning disable CS0162 // Unreachable code detected
			if (GraphicAssets.ForceProSkin)
				GUILayout.BeginVertical(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).GetStyle("hostview"));
#pragma warning restore CS0162 // Unreachable code detected
#endif

			//position.x & y are the position of the window in Unity, i think
			//	maybe it's the window position on the desktop
			//	either way it wasn't the value i expected, so i force (0, 0)
			m_Position.Set(0.0f, 0.0f, position.width, position.height);
			m_JumpLinkListView.Draw(m_Position);

#if !UNITY_2019_3_OR_NEWER && FORCE_PRO_SKIN
#pragma warning disable CS0162 // Unreachable code detected
			if (GraphicAssets.ForceProSkin)
				GUILayout.EndVertical();
#pragma warning restore CS0162 // Unreachable code detected
#endif
		}

		private void OnBecameVisible()
		{
			if (m_JumpLinks != null)
			{
				m_JumpLinks.RefreshHierarchyLinks();
				m_JumpLinks.RefreshProjectLinks();
			}
		}

		private void OnProjectWindowChange()
		{
			m_JumpLinks.RefreshProjectLinks();
			Repaint();
		}

		private void OnHierarchyWindowChange()
		{
			if (EditorApplication.isPlaying)
				return;

			//TODO: remember why i needed this
			if (EditorApplication.timeSinceStartup - m_LastHierarchyRefreshTime >= 0.2f)
			{
				m_LastHierarchyRefreshTime = EditorApplication.timeSinceStartup;

				m_JumpLinks.RefreshHierarchyLinks();
				Repaint();
			}
		}

		private void OnSelectionChange()
		{
			m_JumpLinks.RefreshAllLinkSelections();
			Repaint();
		}

		public void RefreshMinSize()
		{
			//TODO: need to notify the parent HostView's ContainerWindow of a minSize change
			//	See ContainerWindow.OnResize()
			this.minSize = new Vector2(139.0f, 228.0f);

			//NOTE: if docked, this makes the window pop out to a new container
			//	See this.set_position(), and this.CreateNewWindowForEditorWindow()
			//Rect pos = position;

			//if (minSize.x > pos.width)
			//{
			//	pos.width = minSize.x;
			//}

			//if (minSize.y > pos.height)
			//{
			//	pos.height = minSize.y;
			//}

			//position = pos;

			Repaint();
		}


		private void CreateMultipleJumpLinks(UnityEngine.Object[] linkReferences)
		{
			#region Removed Filtering
			//NOTE: already fully tested, but decided against it due
			//		to a potentially negative user experience
			//switch (m_Window.JumpToSettingsInstance.Visibility)
			//{
			//case JumpToSettings.VisibleList.ProjectAndHierarchy:
			//	{
			//		for (int i = 0; i < linkReferences.Length; i++)
			//		{
			//			m_JumpLinks.CreateJumpLink(linkReferences[i]);
			//		}
			//	}
			//	break;
			//case JumpToSettings.VisibleList.HierarchyOnly:
			//	{
			//		for (int i = 0; i < linkReferences.Length; i++)
			//		{
			//			m_JumpLinks.CreateOnlyHierarchyJumpLink(linkReferences[i]);
			//		}
			//	}
			//	break;
			//case JumpToSettings.VisibleList.ProjectOnly:
			//	{
			//		for (int i = 0; i < linkReferences.Length; i++)
			//		{
			//			m_JumpLinks.CreateOnlyProjectJumpLink(linkReferences[i]);
			//		}
			//	}
			//	break;
			//}
			#endregion

			for (int i = 0; i < linkReferences.Length; i++)
			{
				m_JumpLinks.CreateJumpLink(linkReferences[i]);
			}

			m_JumpLinks.RefreshProjectLinks();
			m_JumpLinks.RefreshHierarchyLinks();

			Repaint();
		}


		public static JumpToEditorWindow GetOrCreateWindow()
		{
			return EditorWindow.GetWindow<JumpToEditorWindow>("JumpTo");
		}

		public static bool IsOpen()
		{
			JumpToEditorWindow[] windows = Resources.FindObjectsOfTypeAll<JumpToEditorWindow>();
			return windows != null && windows.Length > 0 && windows[0] != null && s_InstanceCount > 0;
		}

		public static void RepaintOpenWindows()
		{
			JumpToEditorWindow[] windows = Resources.FindObjectsOfTypeAll<JumpToEditorWindow>();
			if (windows != null && windows.Length > 0)
			{
				for (int i = 0; i < windows.Length; i++)
				{
					windows[i].Repaint();
				}
			}
		}

		[MenuItem("Window/JumpTo")]
		public static void JumpTo_InitMainMenu()
		{
			JumpToEditorWindow window = GetOrCreateWindow();
			window.Show();
		}

		[MenuItem("Assets/Create/JumpTo Link", true)]
		public static bool JumpTo_AssetsCreateJumpLink_Validate()
		{
			JumpToEditorWindow[] windows = Resources.FindObjectsOfTypeAll<JumpToEditorWindow>();

			Object[] selected = Selection.objects;
			return windows.Length > 0 && windows[0].m_JumpLinks != null && selected != null && selected.Length > 0;
		}

		[MenuItem("Assets/Create/JumpTo Link", false)]
		public static void JumpTo_AssetsCreateJumpLink()
		{
			Object[] selected = Selection.objects;
			JumpToEditorWindow window = GetOrCreateWindow();
			window.CreateMultipleJumpLinks(selected);
		}

		[MenuItem("GameObject/JumpTo Link", priority = 11, validate = true)]
		public static bool JumpTo_GameObjectCreateOtherJumpLink_Validate()
		{
			JumpToEditorWindow[] windows = Resources.FindObjectsOfTypeAll<JumpToEditorWindow>();

			Object[] selected = Selection.objects;
			return windows.Length > 0 && windows[0].m_JumpLinks != null && selected != null && selected.Length > 0;
		}

		[MenuItem("GameObject/JumpTo Link", priority = 11, validate = false)]
		public static void JumpTo_GameObjectCreateOtherJumpLink()
		{
			Object[] selected = Selection.objects;
			JumpToEditorWindow window = GetOrCreateWindow();
			window.CreateMultipleJumpLinks(selected);
		}
	}
}
