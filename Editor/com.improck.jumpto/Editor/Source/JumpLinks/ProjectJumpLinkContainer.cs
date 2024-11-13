using UnityEditor;
using UnityEngine;


namespace ImpRock.JumpTo
{
	internal sealed class ProjectJumpLinkContainer : JumpLinkContainer<ProjectJumpLink>
	{
		public override void AddLink(UnityEngine.Object linkReference, PrefabAssetType prefabAssetType, PrefabInstanceStatus prefabInstanceStatus)
		{
			//basically, if no linked object in the list has a reference to the passed object
			if (!m_Links.Exists(linked => linked.LinkReference == linkReference))
			{
				ProjectJumpLink link = ScriptableObject.CreateInstance<ProjectJumpLink>();
				link.hideFlags = HideFlags.HideAndDontSave;
				link.LinkReference = linkReference;

				UpdateLinkInfo(link, prefabAssetType, prefabInstanceStatus);

				link.Area.Set(0.0f, m_Links.Count * GraphicAssets.LinkHeight, 100.0f, GraphicAssets.LinkHeight);

				m_Links.Add(link);
			}

			RaiseOnLinksChanged();
		}

		protected override void UpdateLinkInfo(ProjectJumpLink link, PrefabAssetType prefabAssetType, PrefabInstanceStatus prefabInstanceStatus)
		{
			UnityEngine.Object linkReference = link.LinkReference;
			GUIContent linkContent = EditorGUIUtility.ObjectContent(linkReference, linkReference.GetType());
			link.LinkLabelContent.image = linkContent.image;
			link.LinkLabelContent.tooltip = AssetDatabase.GetAssetPath(linkReference);

			//empty prefabs have no content text
			if (linkContent.text == string.Empty)
			{
				//try to get the name from the link reference itself
				if (linkReference.name != string.Empty)
				{
					link.LinkLabelContent.text = linkReference.name;
				}
				//otherwise pull the object name straight from the filename
				else
				{
					string assetName = AssetDatabase.GetAssetPath(linkReference.GetInstanceID());
					int slash = assetName.LastIndexOf('/');
					int dot = assetName.LastIndexOf('.');
					link.LinkLabelContent.text = assetName.Substring(slash + 1, dot - slash - 1);
				}
			}
			else
			{
				link.LinkLabelContent.text = linkContent.text;
			}

			if (linkReference is GameObject)
			{
				GraphicAssets graphicAssets = GraphicAssets.Instance;

				switch (prefabAssetType)
				{
					case PrefabAssetType.NotAPrefab:
						link.ReferenceType = LinkReferenceType.Asset;
						break;
					case PrefabAssetType.Regular:
						link.ReferenceType = LinkReferenceType.Prefab;
						break;
					case PrefabAssetType.Model:
						link.ReferenceType = LinkReferenceType.Model;
						break;
					case PrefabAssetType.Variant:
						link.ReferenceType = LinkReferenceType.Prefab;
						break;
					case PrefabAssetType.MissingAsset:
						link.ReferenceType = LinkReferenceType.PrefabBroken;
						break;
				}
			}
		}
	}
}
