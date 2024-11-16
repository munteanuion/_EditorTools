using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


namespace ImpRock.JumpTo
{
	public struct ControlIcon
	{
		public bool Enabled;
		public Texture2D Icon;
		public System.Action OnClick;
	}

	internal abstract class GuiJumpLinkViewBase<T> : GuiBase where T : JumpLink
	{
		[SerializeField] protected bool m_HasFocus = false;
		
		protected Rect m_FoldoutRect = new Rect();
		protected Rect m_DrawRect;
		protected Rect m_LinksAreaRect;
		protected Rect m_InsertionDrawRect;
		protected Rect m_ControlRect;
		protected int m_Grabbed = -1;
		protected int m_InsertionIndex = -1;
		protected float m_TotalHeight = 0.0f;
		protected bool m_ContextClick = false;
		protected bool m_DragOwner = false;
		protected bool m_DragInsert = false;
		protected bool m_Foldout = true;
		protected bool m_MarkedForClose = false;
		protected Vector2 m_GrabPosition = Vector2.zero;
		protected Color m_HeaderColorModifier = new Color(1.0f, 1.0f, 1.0f, 0.9f);

		protected GUIContent m_Title;
		protected GUIContent m_MenuSelectAll;
		protected GUIContent m_MenuSelectInverse;
		protected GUIContent m_MenuSelectNone;
		protected GUIContent m_MenuPingLink;
		protected GUIContent m_MenuRemoveLink;
		protected GUIContent m_MenuRemoveAll;
		protected GUIContent m_MenuRemoveLinkPlural;

		protected List<ControlIcon> m_ControlIcons = new List<ControlIcon>();

		protected JumpToEditorWindow m_Window;

		protected JumpLinkContainer<T> m_LinkContainer = null;


		public bool Foldout { get { return m_Foldout; } set { m_Foldout = value; } }
		public bool IsDragOwner { get { return m_DragOwner; } }
		public bool HasFocus { get { return m_HasFocus; } set { m_HasFocus = value; } }
		public bool IsFocusedControl { get { return m_Window == EditorWindow.focusedWindow && m_HasFocus; } }
		public bool MarkedForClose { get { return m_MarkedForClose; } }
		public float TotalHeight { get { return m_TotalHeight; } }
		

		protected abstract void ShowLinkContextMenu();
		protected abstract void ShowTitleContextMenu();
		protected abstract void OnDoubleClick();

		protected virtual void OnRemoveAll() { }


		public void GlobalSelectAll()
		{
			if (m_Foldout)
			{
				m_LinkContainer.LinkSelectionAll();
			}
		}

		public override void OnWindowEnable(EditorWindow window)
		{
			m_Window = window as JumpToEditorWindow;

			m_MenuSelectAll = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextSelectAll));
			m_MenuSelectInverse = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextSelectInverse));
			m_MenuSelectNone = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextSelectNone));
			m_MenuPingLink = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextPingLink));
			m_MenuRemoveLink = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextRemoveLink));
			m_MenuRemoveAll = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextRemoveAll));
			m_MenuRemoveLinkPlural = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextRemoveLinkPlural));

			ControlIcon controlIcon = new ControlIcon()
			{
				Enabled = true,
				Icon = JumpToResources.Instance.GetImage(ResId.ImageHamburger),
				OnClick = ShowTitleContextMenu
			};

			m_ControlIcons.Add(controlIcon);
			
			m_ControlRect.width = 10.0f;
			m_ControlRect.height = 10.0f;
			m_ControlRect.y = (GraphicAssets.LinkViewTitleBarHeight - m_ControlRect.height) * 0.5f;

			m_FoldoutRect.x = 0.0f;
			m_FoldoutRect.y = 1.0f;
			m_FoldoutRect.width = 16.0f;
			m_FoldoutRect.height = 16.0f;

			FindTotalHeight();
		}

		protected override void OnGui()
		{
			m_LinksAreaRect.width = m_Size.x;
			m_LinksAreaRect.height = m_TotalHeight - GraphicAssets.LinkViewTitleBarHeight;

			HandleTitleHeader();

			if (m_Foldout)
			{
				HandleLinks();
			}
		}

		protected void HandleTitleHeader()
		{
			m_DrawRect.Set(0.0f, 0.0f, m_Size.x, GraphicAssets.LinkViewTitleBarHeight);

			if (m_Foldout)
				m_DrawRect.height += 2.0f;

			m_ControlRect.x = m_Size.x - (m_ControlRect.width + 6.0f);

			//TODO: make this more efficient
			int titlePaddingRight = 8;
			for (int i = 0; i < m_ControlIcons.Count; i++)
			{
				if (m_ControlIcons[i].Enabled)
					titlePaddingRight += (int)m_ControlRect.width + 4;
			}

			GraphicAssets.Instance.LinkViewTitleStyle.padding.right = titlePaddingRight;
			
			Event currentEvent = Event.current;
			switch (currentEvent.type)
			{
			case EventType.MouseDown:
				{
					if (currentEvent.button == 0)
					{
						for (int i = 0; i < m_ControlIcons.Count; i++)
						{
							if (!m_ControlIcons[i].Enabled)
								continue;

							if (m_ControlRect.Contains(currentEvent.mousePosition))
							{
								m_ControlIcons[i].OnClick();
								currentEvent.Use();	//changes the event type to Used
								break;
							}

							m_ControlRect.x -= m_ControlRect.width + 4.0f;
						}

						if (currentEvent.type != EventType.Used && m_DrawRect.Contains(currentEvent.mousePosition))
						{
							m_HasFocus = true;
							m_Window.Repaint();
						}
					}
				}
				break;
			case EventType.Repaint:
				{
					Color guiColor = GUI.color;
					GUI.color *= m_HeaderColorModifier;
					GraphicAssets.Instance.LinkViewTitleStyle.Draw(m_DrawRect, m_Title, false, false, false, false);
					GUI.color = guiColor;

					m_DrawRect.x = 14.0f;
					m_DrawRect.width = 16.0f;
					m_DrawRect.height = GraphicAssets.LinkViewTitleBarHeight;
					GUI.DrawTexture(m_DrawRect, m_Title.image, ScaleMode.ScaleToFit);

					for (int i = 0; i < m_ControlIcons.Count; i++)
					{
						if (!m_ControlIcons[i].Enabled)
							continue;

						GUI.DrawTexture(m_ControlRect, m_ControlIcons[i].Icon);
						m_ControlRect.x -= m_ControlRect.width + 4.0f;
					}
				}
				break;
			};

			m_FoldoutRect.width = m_Size.x - titlePaddingRight;

			bool foldout = EditorGUI.Foldout(m_FoldoutRect, m_Foldout, GUIContent.none, true, GraphicAssets.Instance.FoldoutStyle);
			if (foldout != m_Foldout)
			{
				m_Foldout = foldout;

				if (m_Foldout)
				{
					m_LinkContainer.RefreshLinksY();
				}

				FindTotalHeight();
			}
		}

		protected void HandleLinks()
		{
			List<T> links = m_LinkContainer.Links;

			m_DrawRect.x = 0.0f;
			m_DrawRect.y += GraphicAssets.LinkViewTitleBarHeight;
			m_DrawRect.width = m_Size.x;
			m_DrawRect.height = m_Size.y - m_DrawRect.y;

			//makes the link area rects relative to (0,0)
			GUI.BeginGroup(m_DrawRect);

			#region Event Switch
			switch (Event.current.type)
			{
			case EventType.KeyDown:
				{
					Event currentEvent = Event.current;
					if (currentEvent.keyCode == KeyCode.Delete &&
						(Application.platform != RuntimePlatform.OSXEditor || currentEvent.command))
					{
						RemoveSelected();
						m_Window.Repaint();
					}
				}
				break;
			case EventType.MouseDown:
				{
					OnMouseDown();
				}
				break;
			//not raised during DragAndDrop operation
			case EventType.MouseUp:
				{
					OnMouseUp();
				}
				break;
			//MouseDrag for inter-/intra-window dragging
			case EventType.MouseDrag:
				{
					OnMouseDrag(links);
				}
				break;
			case EventType.DragUpdated:
				{
					OnDragUpdated(links);
				}
				break;
			case EventType.DragPerform:
				{
					OnDragPerform();
				}
				break;
			case EventType.DragExited:
				{
					OnDragExited();
				}
				break;
			case EventType.Repaint:
				{
					OnRepaint(links);
				}
				break;
			}
			#endregion

			GUI.EndGroup();
		}

		protected void OnMouseDown()
		{
			Event currentEvent = Event.current;

			if (!m_HasFocus)
				m_Window.Repaint();

			m_HasFocus = true;

			if (currentEvent.button != 2)
			{
				//find the object under the mouse pointer
				int hit = m_LinkContainer.LinkHitTest(currentEvent.mousePosition);
				if (currentEvent.button == 0)
				{
					//if a link was there
					if (hit > -1)
					{
						//if links are currently selected
						if (m_LinkContainer.HasSelection)
						{
							//the click was on a link and the control/command key was down
							if (currentEvent.control || (Application.platform == RuntimePlatform.OSXEditor && currentEvent.command))
							{
								//toggle clicked link selection state
								if (!m_LinkContainer[hit].Selected)
									m_LinkContainer.LinkSelectionAdd(hit);
								else
									m_LinkContainer.LinkSelectionRemove(hit);

								m_LinkContainer.LinkSelectionAddToUnitySelection();
							}
							//or the click was on a link and the shift key was down
							else if (currentEvent.shift)
							{
								m_LinkContainer.LinkSelectionSetRange(m_LinkContainer.ActiveSelection, hit);

								m_LinkContainer.LinkSelectionSetUnitySelection();
							}
							//or the clicked link was not already selected
							else if (!m_LinkContainer[hit].Selected)
							{
								//set the selection to the clicked link
								m_LinkContainer.LinkSelectionSet(hit);
							}
						}
						//no links are selected
						else
						{
							//set the selection to the clicked link
							m_LinkContainer.LinkSelectionSet(hit);
						}
					} //if a link was hit

					m_Grabbed = hit;
					m_GrabPosition = currentEvent.mousePosition;

					//on double-click
					if (currentEvent.clickCount == 2 && hit > -1 && m_LinkContainer[hit].Selected)
						OnDoubleClick();

					currentEvent.Use();
				} //handle left mouse button
				else if (currentEvent.button == 1)
				{
					//if links are currently selected
					if (m_LinkContainer.HasSelection)
					{
						//if the link that was clicked was already selected
						if (m_LinkContainer[hit].Selected)
						{
							m_LinkContainer.ActiveSelection = hit;
						}
						//or a new link was selected
						else if (hit > -1)
						{
							m_LinkContainer.LinkSelectionSet(hit);
							m_LinkContainer.LinkSelectionSetUnitySelection();
						}
					}
					//no links are selected, and a link was hit
					else if (hit > -1)
					{
						m_LinkContainer.LinkSelectionSet(hit);
						m_LinkContainer.LinkSelectionSetUnitySelection();
					}

					m_ContextClick = true;

					currentEvent.Use();
				} //handle right mouse button
			} //filter out middle mouse button
		}

		protected void OnMouseUp()
		{
			Event currentEvent = Event.current;

			int selectionCount = m_LinkContainer.SelectionCount;
			if (selectionCount > 0)
			{
				if (currentEvent.button == 0)
				{
					if (m_Grabbed > -1 && !m_DragOwner)
					{
						if (!currentEvent.shift && !currentEvent.control &&
							!(Application.platform == RuntimePlatform.OSXEditor && currentEvent.command))
						{
							if (selectionCount > 1)
							{
								m_LinkContainer.LinkSelectionSet(m_Grabbed);
							}

							m_LinkContainer.LinkSelectionSetUnitySelection();
						}
						else if (selectionCount == 1)
						{
							m_LinkContainer.LinkSelectionAddToUnitySelection();
						}

						currentEvent.Use();
					}
				}
				else if (currentEvent.button == 1 && m_ContextClick)
				{
					ShowLinkContextMenu();

					currentEvent.Use();
				}

				m_Grabbed = -1;
				m_ContextClick = false;
			}
		}

		protected void OnMouseDrag(List<T> links)
		{
			Event currentEvent = Event.current;
			if (m_LinksAreaRect.Contains(currentEvent.mousePosition))
			{
				if (m_Grabbed > -1 && Vector2.Distance(m_GrabPosition, currentEvent.mousePosition) > 4.0f)
				//NOTE: this was preventing a drag to other windows when there was only one link in the list
				//	!(links[m_Grabbed].Area.RectInternal.Contains(currentEvent.mousePosition)))
				{
					m_DragOwner = true;
					
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.paths = new string[] { };
					DragAndDrop.objectReferences = m_LinkContainer.SelectedLinkReferences;
					DragAndDrop.StartDrag("JumpToSelection");
					//NOTE: tried to set the visual mode here. always got reset to none.

					currentEvent.Use();
				}
			}
		}

		protected void OnDragUpdated(List<T> links)
		{
			if (m_DragOwner)
			{
				Event currentEvent = Event.current;
				
				if (m_LinksAreaRect.Contains(currentEvent.mousePosition))
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Move;

					m_DragInsert = true;

					int hit = m_LinkContainer.LinkHitTest(currentEvent.mousePosition);
					if (hit > -1)
					{
						m_InsertionIndex = hit;

						m_InsertionDrawRect = links[hit].Area;
						m_InsertionDrawRect.x += 8.0f;
						m_InsertionDrawRect.width -= 8.0f;
						m_InsertionDrawRect.height = GraphicAssets.Instance.DragDropInsertionStyle.fixedHeight;

						if ((currentEvent.mousePosition.y - m_InsertionDrawRect.y) < (GraphicAssets.LinkHeight * 0.5f))
						{
							m_InsertionDrawRect.y -= GraphicAssets.LinkHeight;
						}
						else
						{
							m_InsertionIndex++;
						}
					}
				}

				currentEvent.Use();
			}
		}

		protected void OnDragPerform()
		{
			Event currentEvent = Event.current;
			if (m_DragOwner && m_LinksAreaRect.Contains(currentEvent.mousePosition))
			{
				DragAndDrop.AcceptDrag();

				m_LinkContainer.MoveSelected(m_InsertionIndex);

				if (m_LinkContainer.SelectionCount == 1)
				{
					m_LinkContainer.LinkSelectionSetUnitySelection();
				}

				m_DragInsert = false;
				m_DragOwner = false;
				m_Grabbed = -1;
				m_InsertionIndex = -1;

				currentEvent.Use();
				
				//reset the drag and drop data
				DragAndDrop.PrepareStartDrag();
			}

			m_Window.Repaint();
		}

		protected void OnDragExited()
		{
			if (m_DragOwner && m_DragInsert)
			{
				m_LinkContainer.RefreshLinkSelections();
			}

			m_DragInsert = false;
			m_DragOwner = false;
			m_Grabbed = -1;
			m_InsertionIndex = -1;
		}

		protected virtual Color DetermineNormalTextColor(T link)
		{
			return GraphicAssets.Instance.LinkTextColors[(int)link.ReferenceType];
		}

		protected virtual Color DetermineOnNormalTextColor(T link)
		{
			return GraphicAssets.Instance.SelectedLinkTextColors[(int)link.ReferenceType];
		}

		protected void OnRepaint(List<T> links)
		{
			//draw links
			m_DrawRect.Set(0.0f, 0.0f, m_Size.x, GraphicAssets.LinkHeight);

			GraphicAssets graphicAssets = GraphicAssets.Instance;
			GUIStyle linkLabelStyle = graphicAssets.LinkLabelStyle;

			for (int i = 0; i < links.Count; i++)
			{
				linkLabelStyle.normal.textColor = DetermineNormalTextColor(links[i]);
				linkLabelStyle.hover.textColor = linkLabelStyle.normal.textColor;
				linkLabelStyle.active.textColor = linkLabelStyle.normal.textColor;
				linkLabelStyle.focused.textColor = linkLabelStyle.normal.textColor;
				linkLabelStyle.onNormal.textColor = DetermineOnNormalTextColor(links[i]);
				linkLabelStyle.onHover.textColor = linkLabelStyle.onNormal.textColor;
				linkLabelStyle.onActive.textColor = linkLabelStyle.onNormal.textColor;
				linkLabelStyle.onFocused.textColor = linkLabelStyle.onNormal.textColor;

				links[i].Area.width = m_DrawRect.width;

				linkLabelStyle.Draw(m_DrawRect, links[i].LinkLabelContent, false, false, links[i].Selected, links[i].Selected && IsFocusedControl);

				m_DrawRect.y += m_DrawRect.height;
			}

			//padding on the bottom, in case we draw something under
			//	the links some day
			m_DrawRect.y += 2.0f;

			if (m_DragInsert && m_InsertionIndex > -1)
			{
				graphicAssets.DragDropInsertionStyle.Draw(m_InsertionDrawRect, false, false, false, false);
			}
		}

		protected void AddCommonTitleContextMenuItems(GenericMenu menu)
		{
			if (m_Foldout)
			{
				menu.AddItem(m_MenuSelectAll, false, SelectAll);

				if (m_LinkContainer.HasSelection)
				{
					menu.AddItem(m_MenuSelectInverse, false, SelectInverse);
					menu.AddItem(m_MenuSelectNone, false, SelectNone);
				}
				else
				{
					menu.AddDisabledItem(m_MenuSelectInverse);
					menu.AddDisabledItem(m_MenuSelectNone);
				}
			}
			else
			{
				menu.AddDisabledItem(m_MenuSelectAll);
				menu.AddDisabledItem(m_MenuSelectInverse);
				menu.AddDisabledItem(m_MenuSelectNone);
			}

			menu.AddSeparator(string.Empty);

			menu.AddItem(m_MenuRemoveAll, false, RemoveAll);
		}

		protected void SelectAll()
		{
			m_LinkContainer.LinkSelectionAll();
			m_LinkContainer.LinkSelectionAddToUnitySelection();
		}

		protected void SelectInverse()
		{
			m_LinkContainer.LinkSelectionInvert();
			m_LinkContainer.LinkSelectionAddToUnitySelection();
		}

		protected void SelectNone()
		{
			m_LinkContainer.LinkSelectionClear();
			m_LinkContainer.LinkSelectionAddToUnitySelection();
		}

		protected void RemoveAll()
		{
			//if confirmed, remove all
			if (EditorUtility.DisplayDialog(JumpToResources.Instance.GetText(ResId.DialogRemoveAllTitle),
				JumpToResources.Instance.GetText(ResId.DialogRemoveAllMessage),
				JumpToResources.Instance.GetText(ResId.DialogYes),
				JumpToResources.Instance.GetText(ResId.DialogNo)))
			{
				m_LinkContainer.RemoveAll();

				OnRemoveAll();
			}
		}

		protected void RemoveSelected()
		{
			m_LinkContainer.RemoveSelected();
		}

		protected void PingSelectedLink()
		{
			T activeSelection = m_LinkContainer.ActiveSelectedObject;
			if (activeSelection != null)
				EditorGUIUtility.PingObject(activeSelection.LinkReference);
		}

		protected void FindTotalHeight()
		{
			//toolbar height + (link height * link count) + padding
			m_TotalHeight = GraphicAssets.LinkViewTitleBarHeight;

			if (m_Foldout)
			{
				m_TotalHeight += (m_LinkContainer.Links.Count * GraphicAssets.LinkHeight) + 4.0f;
			}
		}
	}
}
