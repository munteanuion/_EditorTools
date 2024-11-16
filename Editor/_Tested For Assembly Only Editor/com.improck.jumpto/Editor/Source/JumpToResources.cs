using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;


namespace ImpRock.JumpTo
{
	internal static class ResId
	{
		public static readonly int LabelProjectLinks = "label_projectLinks".GetHashCode();
		public static readonly int LabelHierarchyLinksSuffix = "label_hierarchyLinksSuffix".GetHashCode();

		public static readonly int LabelHelpMessage1 = "label_helpMessage1".GetHashCode();

		public static readonly int MenuContextSelectAll = "menu_contextSelectAll".GetHashCode();
		public static readonly int MenuContextSelectInverse = "menu_contextSelectInverse".GetHashCode();
		public static readonly int MenuContextSelectNone = "menu_contextSelectNone".GetHashCode();
		public static readonly int MenuContextPingLink = "menu_contextPingLink".GetHashCode();
		public static readonly int MenuContextSetAsSelection = "menu_contextSetAsSelection".GetHashCode();
		public static readonly int MenuContextAddToSelection = "menu_contextAddToSelection".GetHashCode();
		public static readonly int MenuContextFrameLink = "menu_contextFrameLink".GetHashCode();
		public static readonly int MenuContextOpenLink = "menu_contextOpenLink".GetHashCode();
		public static readonly int MenuContextRemoveLink = "menu_contextRemoveLink".GetHashCode();
		public static readonly int MenuContextRemoveAll = "menu_contextRemoveAll".GetHashCode();
		
		public static readonly int MenuContextSetAsSelectionPlural = "menu_contextSetAsSelectionPlural".GetHashCode();
		public static readonly int MenuContextAddToSelectionPlural = "menu_contextAddToSelectionPlural".GetHashCode();
		public static readonly int MenuContextFrameLinkPlural = "menu_contextFrameLinkPlural".GetHashCode();
		public static readonly int MenuContextOpenLinkPlural = "menu_contextOpenLinkPlural".GetHashCode();
		public static readonly int MenuContextRemoveLinkPlural = "menu_contextRemoveLinkPlural".GetHashCode();
		public static readonly int MenuContextSaveLinks = "menu_contextSaveLinks".GetHashCode();

		public static readonly int DialogRemoveAllTitle = "dialog_removeAllTitle".GetHashCode();
		public static readonly int DialogRemoveAllMessage = "dialog_removeAllMessage".GetHashCode();

		public static readonly int DialogSaveLinksWarningTitle = "dialog_saveLinksWarningTitle".GetHashCode();
		public static readonly int DialogSaveLinksWarningMessage = "dialog_saveLinksWarningMessage".GetHashCode();

		public static readonly int DialogYes = "dialog_yes".GetHashCode();
		public static readonly int DialogNo = "dialog_no".GetHashCode();
		public static readonly int DialogOk = "dialog_ok".GetHashCode();
		public static readonly int DialogCancel = "dialog_cancel".GetHashCode();
		public static readonly int DialogSaveLinks = "dialog_saveLinks".GetHashCode();
		
		public static readonly int ImageTabIcon = "tabicon.png".GetHashCode();
		public static readonly int ImageHamburger;
		public static readonly int ImageDiskette;

		public static int[] LogStatements;


		static ResId()
		{
			if (EditorGUIUtility.isProSkin || GraphicAssets.ForceProSkin)
			{
				ImageHamburger = "hamburger_pro.png".GetHashCode();
				ImageDiskette = "diskette_pro.png".GetHashCode();
			}
			else
			{
				ImageHamburger = "hamburger.png".GetHashCode();
				ImageDiskette = "diskette.png".GetHashCode();
			}
		}
	}

	internal sealed class JumpToResources
	{
		#region Singleton
		private static JumpToResources s_Instance = null;

		public static JumpToResources Instance { get { if (s_Instance == null) { s_Instance = new JumpToResources(); } return s_Instance; } }


		private JumpToResources() { }

		public static void DestroyInstance()
		{
			if (s_Instance != null)
			{
				s_Instance.CleanUp();
				s_Instance = null;
			}
		}
		#endregion


		private readonly Dictionary<int, string> m_TextResources = new Dictionary<int, string>();
		private readonly Dictionary<int, Texture2D> m_ImageResources = new Dictionary<int, Texture2D>();


		public string GetText(int textId)
		{
			return m_TextResources[textId];
		}

		public Texture2D GetImage(int imageId)
		{
			return m_ImageResources[imageId];
		}

		public void LoadResources()
		{
			//text resources
			switch (Application.systemLanguage)
			{
			case SystemLanguage.English:
				LoadText("jumptolang_en.txt");
				break;
			default:
				LoadText("jumptolang_en.txt");
				break;
			}

			//image resources
			LoadImage("tabicon.png");

			if (EditorGUIUtility.isProSkin || GraphicAssets.ForceProSkin)
			{
				//pro skin
				LoadImage("hamburger_pro.png");
				LoadImage("diskette_pro.png");
			}
			else
			{
				//free skin
				LoadImage("hamburger.png");
				LoadImage("diskette.png");
			}
		}

		private void LoadText(string fileName)
		{
			TextAsset langAsset = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/Plugins/_EditorTools/Editor/com.improck.jumpto/Editor/Lang/{fileName}");
			if (langAsset != null)
			{
				using (StringReader reader = new StringReader(langAsset.text))
				{
					int idHash = 0;
					int equalsPos = 0;
					const string equals = " = ";
					string line = string.Empty;
					string id = string.Empty;
					string text = string.Empty;

					List<int> logIds = new List<int>();

					while ((line = reader.ReadLine()) != null)
					{
						if (line.Length == 0 || line[0] == ';')
							continue;

						equalsPos = line.IndexOf(equals);
						id = line.Substring(0, equalsPos);
						text = line.Substring(equalsPos + 3);

						idHash = id.GetHashCode();

						if (id.StartsWith("log"))
						{
							logIds.Add(idHash);
							text = "JumpTo: " + text;
						}

						if (m_TextResources.ContainsKey(idHash))
						{
							m_TextResources[idHash] = text;
						}
						else
						{
							m_TextResources.Add(idHash, text);
						}
					}

					ResId.LogStatements = logIds.ToArray();
				}
			}
		}

		private void LoadImage(string fileName)
		{
			int fileNameHash = fileName.GetHashCode();
			if (!m_ImageResources.ContainsKey(fileNameHash))
			{
				Texture2D imageAsset = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Plugins/_EditorTools/Editor/com.improck.jumpto/Editor/Images/{fileName}");
				if (imageAsset != null)
				{
					m_ImageResources.Add(fileNameHash, imageAsset);
				}
			}
		}
		
		private void CleanUp()
		{
			//unload images
			foreach (KeyValuePair<int, Texture2D> image in m_ImageResources)
			{
				Object.DestroyImmediate(image.Value);
			}
		}
	}
}
