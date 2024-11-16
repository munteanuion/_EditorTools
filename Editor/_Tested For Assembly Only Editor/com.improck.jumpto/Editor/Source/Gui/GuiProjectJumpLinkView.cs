using UnityEditor;
using UnityEngine;


namespace ImpRock.JumpTo
{
	internal sealed class GuiProjectJumpLinkView : GuiJumpLinkViewBase<ProjectJumpLink>
	{
		private GUIContent m_MenuOpenLink;
		private GUIContent m_MenuOpenLinkPlural;


		public override void OnWindowEnable(EditorWindow window)
		{
			m_LinkContainer = (window as JumpToEditorWindow).JumpLinksInstance.ProjectLinks;

			base.OnWindowEnable(window);

			m_LinkContainer.OnLinksChanged += OnProjectLinksChanged;

			m_Title = new GUIContent(JumpToResources.Instance.GetText(ResId.LabelProjectLinks), GraphicAssets.Instance.IconProjectView);
			m_MenuOpenLink = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextOpenLink));
			m_MenuOpenLinkPlural = new GUIContent(JumpToResources.Instance.GetText(ResId.MenuContextOpenLinkPlural));
		}

		public override void OnWindowDisable(EditorWindow window)
		{
			m_LinkContainer.OnLinksChanged -= OnProjectLinksChanged;
		}

		protected override void ShowLinkContextMenu()
		{
			GenericMenu menu = new GenericMenu();

			//NOTE: a space followed by an underscore (" _") will cause all text following that
			//		to appear right-justified and all caps in a GenericMenu. the name is being
			//		parsed for hotkeys, and " _" indicates 'no modifiers' in the hotkey string.
			//		See: http://docs.unity3d.com/ScriptReference/MenuItem.html
			m_MenuPingLink.text = JumpToResources.Instance.GetText(ResId.MenuContextPingLink) + " \"" + m_LinkContainer.ActiveSelectedObject.LinkLabelContent.text + "\"";

			int selectionCount = m_LinkContainer.SelectionCount;
			if (selectionCount == 1)
			{
				menu.AddItem(m_MenuPingLink, false, PingSelectedLink);
				menu.AddItem(m_MenuOpenLink, false, OpenAssets);
				menu.AddSeparator(string.Empty);
				menu.AddItem(m_MenuRemoveLink, false, RemoveSelected);
			}
			else if (selectionCount > 1)
			{
				menu.AddItem(m_MenuPingLink, false, PingSelectedLink);
				menu.AddItem(m_MenuOpenLinkPlural, false, OpenAssets);
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

				AddCommonTitleContextMenuItems(menu);

				menu.ShowAsContext();
			}
		}

		protected override void OnDoubleClick()
		{
			OpenAssets();
		}

		private void OpenAssets()
		{
			ProjectJumpLink activeSelection = m_LinkContainer.ActiveSelectedObject;
			if (activeSelection != null)
				AssetDatabase.OpenAsset(activeSelection.LinkReference);
		}

		private void OnProjectLinksChanged()
		{
			if (m_LinkContainer.Links.Count == 0)
			{
				m_Window.SerializationControlInstance.SaveProjectLinks();

				m_MarkedForClose = true;
			}
			else
			{
				FindTotalHeight();
			}
		}
	}
}
