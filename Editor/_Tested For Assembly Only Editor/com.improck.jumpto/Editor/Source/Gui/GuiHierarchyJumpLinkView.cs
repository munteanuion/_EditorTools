using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


namespace ImpRock.JumpTo
{
	internal sealed class GuiHierarchyJumpLinkView : GuiJumpLinkViewBase<HierarchyJumpLink>
	{
		private GUIContent m_MenuFrameLink;
		private GUIContent m_MenuFrameLinkPlural;
		private GUIContent m_MenuSaveLinks;
		private string m_TitleSuffix;
		private SceneState m_SceneState = null;
		private int m_SaveIconIndex = -1;
		
		[SerializeField] private bool m_IsDirty = false;
		[SerializeField] private int m_SceneId = 0;

		public int SceneId { get { return m_SceneId; } set { m_SceneId = value; } }


		public override void OnWindowEnable(EditorWindow window)
		{
			//NOTE: set SceneId property before this is called
			m_LinkContainer = (window as JumpToEditorWindow).JumpLinksInstance.HierarchyLinks[m_SceneId];
			
			base.OnWindowEnable(window);

			if (m_MarkedForClose)
				return;
			
			m_LinkContainer.OnLinksChanged += OnHierarchyLinksChanged;

			m_Title = new GUIContent(GraphicAssets.Instance.IconHierarchyView);
			m_MenuFrameLink = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextFrameLink));
			m_MenuFrameLinkPlural = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextFrameLinkPlural));
			m_MenuSaveLinks = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextSaveLinks));

			m_TitleSuffix = " " + JumpToResources.Instance.GetText(ResId.LabelHierarchyLinksSuffix);

			m_IsDirty = (m_Window.CurrentOperation & Operation.CreatingLinkViaDragAndDrop) == Operation.CreatingLinkViaDragAndDrop;

			ControlIcon controlIcon = new ControlIcon()
			{
				Enabled = m_IsDirty,
				Icon = JumpToResources.Instance.GetImage(ResId.ImageDiskette),
				OnClick = SaveLinks
			};

			m_SaveIconIndex = m_ControlIcons.Count;
			m_ControlIcons.Add(controlIcon);

			SceneSaveDetector.OnSceneDeleted += OnSceneDeleted;
			SceneSaveDetector.OnSceneWillSave += OnSceneWillSave;

			SetupSceneState();
		}
		
		public override void OnWindowClose(EditorWindow window)
		{
			base.OnWindowClose(window);
			
			m_LinkContainer.OnLinksChanged -= OnHierarchyLinksChanged;
			m_SceneState.OnNameChange -= OnSceneNameChanged;
			m_SceneState.OnPathChange -= OnScenePathChanged;
			m_SceneState.OnIsDirtyChange -= OnSceneIsDirtyChanged;
			m_SceneState.OnIsLoadedChange -= OnSceneIsLoadedChanged;
			m_SceneState.OnClose -= OnSceneClosed;
			SceneSaveDetector.OnSceneDeleted -= OnSceneDeleted;
			SceneSaveDetector.OnSceneWillSave -= OnSceneWillSave;
		}

		protected override Color DetermineNormalTextColor(HierarchyJumpLink link)
		{
			if (!link.Active)
				return GraphicAssets.Instance.LinkTextColors[(int)link.ReferenceType] - GraphicAssets.Instance.DisabledColorModifier;
			else
				return GraphicAssets.Instance.LinkTextColors[(int)link.ReferenceType];
		}

		protected override Color DetermineOnNormalTextColor(HierarchyJumpLink link)
		{
			if (!link.Active)
				return GraphicAssets.Instance.SelectedLinkTextColors[(int)link.ReferenceType] - GraphicAssets.Instance.DisabledColorModifier;
			else
				return GraphicAssets.Instance.SelectedLinkTextColors[(int)link.ReferenceType];
		}

		protected override void ShowLinkContextMenu()
		{
			GenericMenu menu = new GenericMenu();

			//NOTE: a space followed by an underscore (" _") will cause all text following that
			//		to appear right-justified and all caps in a GenericMenu. the name is being
			//		parsed for hotkeys, and " _" indicates 'no modifiers' in the hotkey string.
			//		See: http://docs.unity3d.com/ScriptReference/MenuItem.html
			m_MenuPingLink.text = JumpToResources.Instance.GetText(ResId.MenuContextPingLink) + " \""
				+ m_LinkContainer.ActiveSelectedObject.LinkReference.name + "\"";

			int selectionCount = m_LinkContainer.SelectionCount;
			if (selectionCount == 1)
			{
				menu.AddItem(m_MenuPingLink, false, PingSelectedLink);
			
				if (ValidateSceneView())
					menu.AddItem(m_MenuFrameLink, false, FrameLink);
				else
					menu.AddDisabledItem(m_MenuFrameLink);

				menu.AddSeparator(string.Empty);

				menu.AddItem(m_MenuRemoveLink, false, RemoveSelected);
			}
			else if (selectionCount > 1)
			{
				menu.AddItem(m_MenuPingLink, false, PingSelectedLink);

				if (ValidateSceneView())
					menu.AddItem(m_MenuFrameLinkPlural, false, FrameLink);
				else
					menu.AddDisabledItem(m_MenuFrameLinkPlural);

				menu.AddSeparator(string.Empty);

				menu.AddItem(m_MenuRemoveLinkPlural, false, RemoveSelected);
			}

			menu.ShowAsContext();
		}

		protected override void ShowTitleContextMenu()
		{
			if (m_LinkContainer.Links.Count > 0)
			{
				GenericMenu menu = new GenericMenu();

				if (m_IsDirty)
					menu.AddItem(m_MenuSaveLinks, false, SaveLinks);
				else
					menu.AddDisabledItem(m_MenuSaveLinks);

				menu.AddSeparator(string.Empty);

				AddCommonTitleContextMenuItems(menu);

				menu.ShowAsContext();
			}
		}

		protected override void OnDoubleClick()
		{
			FrameLink();
		}

		protected override void OnRemoveAll()
		{
			m_Window.SerializationControlInstance.SaveHierarchyLinks(m_SceneId);
			m_MarkedForClose = true;
		}

		private void SetupSceneState()
		{
			if (m_SceneState != null)
			{
				m_SceneState.OnNameChange -= OnSceneNameChanged;
				m_SceneState.OnPathChange -= OnScenePathChanged;
				m_SceneState.OnIsDirtyChange -= OnSceneIsDirtyChanged;
				m_SceneState.OnIsLoadedChange -= OnSceneIsLoadedChanged;
				m_SceneState.OnClose -= OnSceneClosed;

				m_SceneState = null;
			}

			if (m_SceneId != 0)
			{
				m_SceneState = SceneStateMonitor.Instance.GetSceneState(m_SceneId);
				if (m_SceneState != null)
				{
					RefreshControlTitle();

					m_SceneState.OnNameChange += OnSceneNameChanged;
					m_SceneState.OnPathChange += OnScenePathChanged;
					m_SceneState.OnIsDirtyChange += OnSceneIsDirtyChanged;
					m_SceneState.OnIsLoadedChange += OnSceneIsLoadedChanged;
					m_SceneState.OnClose += OnSceneClosed;
				}
				else
				{
					string logFormat = JumpToResources.Instance.GetText(ResId.LogStatements[18]);
					Debug.LogError(string.Format(logFormat, m_SceneId));
				}
			}
			else
			{
				Debug.LogError(JumpToResources.Instance.GetText(ResId.LogStatements[19]));
			}
		}

		private void SetSaveDirty(bool isDirty)
		{
			if ((m_Window.CurrentOperation & Operation.LoadingHierarchyLinks) == Operation.LoadingHierarchyLinks ||
				m_SceneState.Path.Length == 0)
			{
				m_IsDirty = false;
			}
			else
			{
				m_IsDirty = isDirty;
			}

			ControlIcon saveIcon = m_ControlIcons[m_SaveIconIndex];
			saveIcon.Enabled = m_IsDirty;
			m_ControlIcons[m_SaveIconIndex] = saveIcon;

			RefreshControlTitle();
		}
		
		private void RefreshControlTitle()
		{
			string title = (m_SceneState.Name.Length != 0 ? m_SceneState.Name : "(Untitled)") + m_TitleSuffix;
			if (m_IsDirty)
				title += '*';

			m_Title.text = title;
		}
		
		private void FrameLink()
		{
			SceneView sceneView = SceneView.lastActiveSceneView;
			if (sceneView == null)
				sceneView = SceneView.currentDrawingSceneView;

			if (sceneView != null)
			{
				sceneView.FrameSelected();
			}
		}

		private void SaveLinks()
		{
			if ((m_LinkContainer as HierarchyJumpLinkContainer).HasLinksToUnsavedInstances)
			{
				JumpToResources resources = JumpToResources.Instance;
				if (EditorUtility.DisplayDialog(resources.GetText(ResId.DialogSaveLinksWarningTitle),
					resources.GetText(ResId.DialogSaveLinksWarningMessage),
					resources.GetText(ResId.DialogSaveLinks), resources.GetText(ResId.DialogCancel)))
				{
					m_Window.SerializationControlInstance.SaveHierarchyLinks(m_SceneId);

					//keep the scene marked dirty so that they can save the scene, and then the links again
					SetSaveDirty(true);
				}
			}
			else
			{
				SetSaveDirty(!m_Window.SerializationControlInstance.SaveHierarchyLinks(m_SceneId));
			}
		}

		private bool ValidateSceneView()
		{
			System.Collections.ArrayList sceneViews = SceneView.sceneViews;
			return sceneViews != null && sceneViews.Count > 0;
		}

		private void OnHierarchyLinksChanged()
		{
			if (m_LinkContainer.Links.Count > 0)
			{
				SetSaveDirty(true);
				FindTotalHeight();
			}
			else
			{
				m_MarkedForClose = true;
			}
		}

		private void OnSceneNameChanged(SceneState sceneState, string oldSceneName)
		{
			if (oldSceneName != string.Empty)
			{
				RefreshControlTitle();
			}
		}

		private void OnScenePathChanged(SceneState sceneState, string oldScenePath)
		{
			if (oldScenePath == string.Empty)
			{
				SetSaveDirty(true);
			}
		}

		private void OnSceneIsDirtyChanged(SceneState sceneState, bool oldIsDirty)
		{
			SetSaveDirty(m_IsDirty || sceneState.IsDirty);
		}

		private void OnSceneIsLoadedChanged(SceneState sceneState, bool oldIsLoaded)
		{
			if (!sceneState.IsLoaded)
			{
				m_LinkContainer.OnLinksChanged -= OnHierarchyLinksChanged;
				m_MarkedForClose = true;

				//NOTE: can't save here because the scene is already gone
				//if (m_IsDirty)
				//	m_Window.SerializationControlInstance.SaveHierarchyLinks(m_SceneId);
			}
		}

		private void OnSceneClosed(SceneState sceneState)
		{
			m_LinkContainer.OnLinksChanged -= OnHierarchyLinksChanged;
			m_MarkedForClose = true;
		}

		private void OnSceneWillSave(string sceneAssetPath)
		{
			if (m_MarkedForClose)
				return;

			if (m_SceneState.Path == sceneAssetPath)
			{
				SetSaveDirty(true);
			}
		}

		private void OnSceneDeleted(string sceneAssetPath)
		{
			if (m_MarkedForClose)
				return;

			if (m_SceneState.Path == sceneAssetPath)
			{
				//cannot save links file with no saved scene
				SetSaveDirty(false);
			}
		}
	}
}
