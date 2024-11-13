using UnityEditor;
using UnityEngine;


namespace ImpRock.JumpTo
{
	[System.Serializable]
	internal sealed class HierarchyJumpLinkContainerFaketionary : Faketionary<int, HierarchyJumpLinkContainer> { }


	internal sealed class HierarchyJumpLinkContainer : JumpLinkContainer<HierarchyJumpLink>
	{
		[SerializeField] bool m_HasLinksToUnsavedInstances = false;


		public bool HasLinksToUnsavedInstances { get { return m_HasLinksToUnsavedInstances; } }


		public override void AddLink(UnityEngine.Object linkReference, PrefabAssetType prefabAssetType, PrefabInstanceStatus prefabInstanceStatus)
		{
			//basically, if no linked object in the list has a reference to the passed object
			if (!m_Links.Exists(linked => linked.LinkReference == linkReference))
			{
				HierarchyJumpLink link = ScriptableObject.CreateInstance<HierarchyJumpLink>();
				link.hideFlags = HideFlags.HideAndDontSave;
				link.LinkReference = linkReference;

				UpdateLinkInfo(link, prefabAssetType, prefabInstanceStatus);

				link.Area.Set(0.0f, m_Links.Count * GraphicAssets.LinkHeight, 100.0f, GraphicAssets.LinkHeight);

				m_Links.Add(link);

				RaiseOnLinksChanged();
			}
		}

		public override void RefreshLinks()
		{
			m_HasLinksToUnsavedInstances = false;

			base.RefreshLinks();
		}

		protected override void UpdateLinkInfo(HierarchyJumpLink link, PrefabAssetType prefabAssetType, PrefabInstanceStatus prefabInstanceStatus)
		{
			UnityEngine.Object linkReference = link.LinkReference;

			GUIContent linkContent = EditorGUIUtility.ObjectContent(linkReference, linkReference.GetType());
			link.LinkLabelContent.text = linkContent.text != string.Empty ? linkContent.text : "[Unnamed]";
			link.LinkLabelContent.image = linkContent.image;

			if (linkReference is GameObject)
			{
				link.Active = (link.LinkReference as GameObject).activeInHierarchy;

				switch (prefabInstanceStatus)
				{
					case PrefabInstanceStatus.NotAPrefab:
						link.ReferenceType = LinkReferenceType.GameObject;
						break;
#if !UNITY_2022_1_OR_NEWER
					case PrefabInstanceStatus.Disconnected:
#endif
					case PrefabInstanceStatus.MissingAsset:
						link.ReferenceType = LinkReferenceType.PrefabInstanceBroken;
						break;
					case PrefabInstanceStatus.Connected:
						link.ReferenceType = prefabAssetType == PrefabAssetType.Model ? LinkReferenceType.ModelInstance : LinkReferenceType.PrefabInstance;
						break;
				}

				Transform linkTransform = (linkReference as GameObject).transform;
				link.LinkLabelContent.tooltip = JumpToUtility.GetTransformPath(linkTransform);
				
				if (prefabAssetType == PrefabAssetType.Model ||
					prefabAssetType == PrefabAssetType.Regular ||
					prefabAssetType == PrefabAssetType.Variant)
				{
					linkReference = PrefabUtility.GetPrefabInstanceHandle(linkReference);
				}

				SerializedObject serializedObject = new SerializedObject(linkReference);
				serializedObject.SetInspectorMode(InspectorMode.Debug);
				if (serializedObject.GetLocalIdInFile() == 0 && prefabInstanceStatus != PrefabInstanceStatus.MissingAsset)
				{
					m_HasLinksToUnsavedInstances = true;
				}
			}
		}
	}
}
