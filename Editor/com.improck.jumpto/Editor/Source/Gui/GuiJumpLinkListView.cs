using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


namespace ImpRock.JumpTo
{
	internal sealed class GuiJumpLinkListView : GuiBase
	{
		[SerializeField] private Vector2 m_ScrollViewPosition;
		[SerializeField] private GuiProjectJumpLinkView m_ProjectView;
		[SerializeField] private List<GuiHierarchyJumpLinkView> m_HierarchyViews = new List<GuiHierarchyJumpLinkView>();

		private readonly RectRef m_DrawRect = new RectRef();
		private Rect m_ScrollViewRect = new Rect();
		private string m_HelpMessage1;

		private JumpToEditorWindow m_Window;

		private readonly Vector2 IconSize = new Vector2(16.0f, 16.0f);
		

		public override void OnWindowEnable(EditorWindow window)
		{
			base.OnWindowEnable(window);

			m_Window = window as JumpToEditorWindow;

			m_HelpMessage1 = JumpToResources.Instance.GetText(ResId.LabelHelpMessage1);

			if (m_ProjectView == null && m_Window.JumpLinksInstance.ProjectLinks.Links.Count > 0)
			{
				m_ProjectView = GuiBase.Create<GuiProjectJumpLinkView>();
			}

			if (m_HierarchyViews.Count == 0)
			{
				List<int> sceneIds = m_Window.JumpLinksInstance.HierarchyLinks.Keys;
				for (int i = 0; i < sceneIds.Count; i++)
				{
					GuiHierarchyJumpLinkView view = GuiBase.Create<GuiHierarchyJumpLinkView>();
					view.SceneId = sceneIds[i];

					m_HierarchyViews.Add(view);
				}
			}
			
			if (m_ProjectView != null)
				m_ProjectView.OnWindowEnable(window);

			for (int i = 0; i < m_HierarchyViews.Count; i++)
			{
				m_HierarchyViews[i].OnWindowEnable(window);
			}

			JumpLinks.OnHierarchyLinkAdded += HierarchyLinkAddedHandler;
			JumpLinks.OnProjectLinkAdded += ProjectLinkAddedHandler;
		}

		public override void OnWindowDisable(EditorWindow window)
		{
			base.OnWindowDisable(window);

			if (m_ProjectView != null)
				m_ProjectView.OnWindowDisable(window);

			for (int i = 0; i < m_HierarchyViews.Count; i++)
			{
				m_HierarchyViews[i].OnWindowDisable(window);
			}

			JumpLinks.OnHierarchyLinkAdded -= HierarchyLinkAddedHandler;
			JumpLinks.OnProjectLinkAdded -= ProjectLinkAddedHandler;
		}

		public override void OnWindowClose(EditorWindow window)
		{
			base.OnWindowClose(window);

			if (m_ProjectView != null)
				m_ProjectView.OnWindowClose(window);

			for (int i = 0; i < m_HierarchyViews.Count; i++)
			{
				m_HierarchyViews[i].OnWindowClose(window);
			}
		}

		protected override void OnGui()
		{
			if (m_ProjectView != null &&
				m_ProjectView.MarkedForClose)
			{
				DestroyImmediate(m_ProjectView);
				m_ProjectView = null;
			}

			for (int i = m_HierarchyViews.Count - 1; i >= 0; i--)
			{
				if (m_HierarchyViews[i].MarkedForClose)
				{
					int sceneId = m_HierarchyViews[i].SceneId;
					DestroyImmediate(m_HierarchyViews[i]);
					m_HierarchyViews.RemoveAt(i);

					m_Window.JumpLinksInstance.RemoveHierarchyJumpLinkContainer(sceneId);
				}
			}

			HandleDragAndDrop();

			if (m_ProjectView != null || m_HierarchyViews.Count > 0)
			{
				Vector2 iconSizeBak = EditorGUIUtility.GetIconSize();
				EditorGUIUtility.SetIconSize(IconSize);

				m_DrawRect.Set(0.0f, 0.0f, m_Size.x, m_Size.y);

				//TODO: maybe only calculate this if the height changes?
				float totalHeight = m_ProjectView != null ? m_ProjectView.TotalHeight : 0.0f;
				for (int i = 0; i < m_HierarchyViews.Count; i++)
				{
					totalHeight += m_HierarchyViews[i].TotalHeight;
				}

				m_ScrollViewRect.height = totalHeight;

				m_ScrollViewPosition = GUI.BeginScrollView(m_DrawRect, m_ScrollViewPosition, m_ScrollViewRect);

				//if the vertical scrollbar is visible, adjust view rect
				//	width by the width of the scrollbar (17.0f)
				if (m_ScrollViewRect.height > m_DrawRect.height)
					m_DrawRect.width = m_DrawRect.width - 15.0f;

				//draw the project view
				if (m_ProjectView != null)
				{
					m_DrawRect.height = m_ProjectView.TotalHeight;
					m_ProjectView.Draw(m_DrawRect);
				}
				else
				{
					m_DrawRect.height = 0.0f;
				}

				//draw all hierarchy views
				for (int i = 0; i < m_HierarchyViews.Count; i++)
				{
					m_DrawRect.y += m_DrawRect.height;
					m_DrawRect.height = m_HierarchyViews[i].TotalHeight;
					m_HierarchyViews[i].Draw(m_DrawRect);
				}

				GUI.EndScrollView(true);

				HandleSelectAllCommand();

				EditorGUIUtility.SetIconSize(iconSizeBak);
			}	//project or hierarchy views exist
			else
			{
				EditorGUILayout.HelpBox(m_HelpMessage1, MessageType.Info);
			}
		}

		private void HandleSelectAllCommand()
		{
			if (Event.current.type == EventType.ValidateCommand &&
				Event.current.commandName == "SelectAll")
			{
				if (m_ProjectView != null)
				{
					m_ProjectView.GlobalSelectAll();
				}

				for (int i = 0; i < m_HierarchyViews.Count; i++)
				{
					m_HierarchyViews[i].GlobalSelectAll();
				}

				m_Window.JumpLinksInstance.SetAllSelectedLinksAsUnitySelection();
			}
		}

		private void HandleDragAndDrop()
		{
			Event currentEvent = Event.current;
			switch (currentEvent.type)
			{
			//raised repeatedly while a drag op is hovering
			case EventType.DragUpdated:
				{
					if (m_ProjectView != null && m_ProjectView.IsDragOwner)
					{
						break;
					}
					else
					{
						bool dragOwned = false;
						for (int i = 0; i < m_HierarchyViews.Count; i++)
						{
							if (m_HierarchyViews[i].IsDragOwner)
							{
								dragOwned = true;
								break;
							}
						}

						if (dragOwned)
							break;
					}

					//drag most likely came from another window, figure out
					//	if it can be accepted and where it would go
					if (DragAndDrop.visualMode == DragAndDropVisualMode.None)
					{
						if (DragAndDrop.objectReferences.Length > 0)
						{
							//reject component links
							if (DragAndDrop.objectReferences[0] is Component)
								DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
							else
								DragAndDrop.visualMode = DragAndDropVisualMode.Link;
						}
					}

					currentEvent.Use();
				}	//case DragUpdated
				break;
			//raised on mouse-up if DragAndDrop.visualMode != None or Rejected
			case EventType.DragPerform:
				{
					if (m_ProjectView != null && m_ProjectView.IsDragOwner)
					{
						break;
					}
					else
					{
						bool dragOwned = false;
						for (int i = 0; i < m_HierarchyViews.Count; i++)
						{
							if (m_HierarchyViews[i].IsDragOwner)
							{
								dragOwned = true;
								break;
							}
						}

						if (dragOwned)
							break;
					}

					DragAndDrop.AcceptDrag();

					OnDropObjectOrAsset();
					
					currentEvent.Use();

					//reset drag & drop data
					DragAndDrop.PrepareStartDrag();
				}	//case DragPerform
				break;
			//raised after DragPerformed OR if escape is pressed;
			//	use for any cleanup of stuff initialized during
			//	DragUpdated event
			//***** case EventType.DragExited: *****
			}
		}

		private void OnDropObjectOrAsset()
		{
			m_Window.CurrentOperation |= Operation.CreatingLinkViaDragAndDrop;

			UnityEngine.Object[] objectReferences = DragAndDrop.objectReferences;
			if (objectReferences.Length > 0)
			{
				for (int i = 0; i < objectReferences.Length; i++)
				{
					m_Window.JumpLinksInstance.CreateJumpLink(objectReferences[i]);
				}

				m_Window.JumpLinksInstance.RefreshHierarchyLinks();
				m_Window.JumpLinksInstance.RefreshAllLinkSelections();
			}

			m_Window.CurrentOperation &= ~Operation.CreatingLinkViaDragAndDrop;
		}

		private void HierarchyLinkAddedHandler(int sceneId)
		{
			if (m_HierarchyViews.Find(v => v.SceneId == sceneId) == null)
			{
				GuiHierarchyJumpLinkView view = GuiBase.Create<GuiHierarchyJumpLinkView>();
				view.SceneId = sceneId;
				view.OnWindowEnable(m_Window);

				m_HierarchyViews.Add(view);

				m_Window.Repaint();
			}
		}

		private void ProjectLinkAddedHandler()
		{
			if (m_ProjectView == null)
			{
				m_ProjectView = GuiBase.Create<GuiProjectJumpLinkView>();
				m_ProjectView.OnWindowEnable(m_Window);

				m_Window.Repaint();
			}
		}
	}
}
