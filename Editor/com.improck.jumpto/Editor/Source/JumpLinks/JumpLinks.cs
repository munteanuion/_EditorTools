using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;


namespace ImpRock.JumpTo
{
	[System.Serializable]
	internal enum LinkReferenceType
	{
		Asset = 0,
		GameObject = 0,
		Model = 1,
		ModelInstance = 1,
		Prefab = 2,
		PrefabInstance = 2,
		PrefabBroken = 3,
		PrefabInstanceBroken = 3
	}

	
	internal abstract class JumpLink : ScriptableObject
	{
		[SerializeField] protected UnityEngine.Object m_LinkReference;
		[SerializeField] protected GUIContent m_LinkLabelContent = new GUIContent();
		[SerializeField] protected LinkReferenceType m_ReferenceType = LinkReferenceType.Asset;
		[SerializeField] protected bool m_Selected = false;


		public UnityEngine.Object LinkReference { get { return m_LinkReference; } set { m_LinkReference = value; } }
		public GUIContent LinkLabelContent { get { return m_LinkLabelContent; } set { m_LinkLabelContent = value; } }
		public LinkReferenceType ReferenceType { get { return m_ReferenceType; } set { m_ReferenceType = value; } }
		public RectRef Area { get; set; }
		public bool Selected { get { return m_Selected; } set { m_Selected = value; } }


		public JumpLink()
		{
			Area = new RectRef();
		}
	}


	internal sealed class ProjectJumpLink : JumpLink
	{
	}


	internal sealed class HierarchyJumpLink : JumpLink
	{
		[SerializeField] private bool m_Active = true;


		public bool Active { get { return m_Active; } set { m_Active = value; } }
	}


	internal sealed class JumpLinks : EditorScriptableObject<JumpLinks>
	{
		[SerializeField] private ProjectJumpLinkContainer m_ProjectLinkContainer;
		//NOTE: scene handle can be used as a unique id, but only while the scene is known to the scene manager
		[SerializeField] private HierarchyJumpLinkContainerFaketionary m_HierarchyLinkContainers = new HierarchyJumpLinkContainerFaketionary();
		

		public ProjectJumpLinkContainer ProjectLinks { get { return m_ProjectLinkContainer; } }
		public HierarchyJumpLinkContainerFaketionary HierarchyLinks { get { return m_HierarchyLinkContainers; } }


		public static event System.Action OnProjectLinkAdded;
		public static event System.Action<int> OnHierarchyLinkAdded;


		protected override void Initialize()
		{
			m_ProjectLinkContainer = ScriptableObject.CreateInstance<ProjectJumpLinkContainer>();
			m_ProjectLinkContainer.hideFlags = HideFlags.HideAndDontSave;
		}

		public HierarchyJumpLinkContainer GetHierarchyJumpLinkContainer(int sceneHandle)
		{
			HierarchyJumpLinkContainer container;
			m_HierarchyLinkContainers.TryGetValue(sceneHandle, out container);
			return container;
		}

		public HierarchyJumpLinkContainer AddHierarchyJumpLinkContainer(int sceneHandle)
		{
			HierarchyJumpLinkContainer container;
			if (!m_HierarchyLinkContainers.TryGetValue(sceneHandle, out container))
			{
				container = ScriptableObject.CreateInstance<HierarchyJumpLinkContainer>();
				container.hideFlags = HideFlags.HideAndDontSave;
				m_HierarchyLinkContainers.Add(sceneHandle, container);
			}

			return container;
		}

		public void RemoveHierarchyJumpLinkContainer(int sceneHandle)
		{
			m_HierarchyLinkContainers.Remove(sceneHandle);
		}

		public void CreateJumpLink(UnityEngine.Object linkReference)
		{
			if (linkReference is GameObject)
			{
				PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(linkReference);
				PrefabInstanceStatus prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(linkReference);
				if (prefabInstanceStatus != PrefabInstanceStatus.NotAPrefab || prefabAssetType == PrefabAssetType.NotAPrefab)
				{
					Scene scene = ((GameObject)linkReference).scene;
					if (!EditorSceneManager.IsPreviewScene(scene))
					{
						//gets existing, or creates, a link container
						HierarchyJumpLinkContainer linkContainer = AddHierarchyJumpLinkContainer(scene.handle);
						linkContainer.AddLink(linkReference, prefabAssetType, prefabInstanceStatus);

						OnHierarchyLinkAdded?.Invoke(scene.handle);
					}
				}
				else
				{
					m_ProjectLinkContainer.AddLink(linkReference, prefabAssetType, prefabInstanceStatus);

					OnProjectLinkAdded?.Invoke();
				}
			}
			else if (!(linkReference is Component))
			{
				m_ProjectLinkContainer.AddLink(linkReference, PrefabAssetType.NotAPrefab, PrefabInstanceStatus.NotAPrefab);

				OnProjectLinkAdded?.Invoke();
			}
		}

		public void CreateOnlyProjectJumpLink(UnityEngine.Object linkReference)
		{
			if (linkReference is Component)
				return;

			PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(linkReference);
			PrefabInstanceStatus prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(linkReference);
			if (!(linkReference is GameObject) ||
				prefabInstanceStatus == PrefabInstanceStatus.NotAPrefab)
			{
				m_ProjectLinkContainer.AddLink(linkReference, prefabAssetType, prefabInstanceStatus);

				OnProjectLinkAdded?.Invoke();
			}
		}

		public void CreateOnlyHierarchyJumpLink(UnityEngine.Object linkReference)
		{
			if (linkReference is Component)
				return;

			if (linkReference is GameObject)
			{
				Scene scene = ((GameObject)linkReference).scene;
				if (!EditorSceneManager.IsPreviewScene(scene))
				{
					PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(linkReference);
					PrefabInstanceStatus prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(linkReference);

					//gets existing, or creates, a link container
					HierarchyJumpLinkContainer linkContainer = AddHierarchyJumpLinkContainer(scene.handle);
					linkContainer.AddLink(linkReference, prefabAssetType, prefabInstanceStatus);

					OnHierarchyLinkAdded?.Invoke(scene.handle);
				}
			}
		}

		public void RefreshAllLinkSelections()
		{
			foreach (KeyValuePair<int, HierarchyJumpLinkContainer> container in m_HierarchyLinkContainers)
			{
				container.Value.RefreshLinkSelections();
			}

			m_ProjectLinkContainer.RefreshLinkSelections();
		}

		public void RefreshHierarchyLinks()
		{
			foreach (KeyValuePair<int, HierarchyJumpLinkContainer> container in m_HierarchyLinkContainers)
			{
				container.Value.RefreshLinks();
			}
		}

		public void RefreshProjectLinks()
		{
			m_ProjectLinkContainer.RefreshLinks();
		}

		public void SetAllSelectedLinksAsUnitySelection()
		{
			List<Object> allLinkReferences = new List<Object>();
			JumpLink link;
			List<ProjectJumpLink> projectJumpLinks = m_ProjectLinkContainer.Links;
			int linkCount = projectJumpLinks.Count;
			for (int i = 0; i < linkCount; i++)
			{
				link = projectJumpLinks[i];
				if (link.Selected)
					allLinkReferences.Add(link.LinkReference);
			}
			
			List<HierarchyJumpLink> hierarchyJumpLinks;
			foreach (KeyValuePair<int, HierarchyJumpLinkContainer> container in m_HierarchyLinkContainers)
			{
				hierarchyJumpLinks = container.Value.Links;
				linkCount = hierarchyJumpLinks.Count;
				for (int i = 0; i < linkCount; i++)
				{
					link = hierarchyJumpLinks[i];
					if (link.Selected)
						allLinkReferences.Add(link.LinkReference);
				}
			}

			if (allLinkReferences.Count > 0)
			{
				Selection.objects = allLinkReferences.ToArray();
			}
		}
	}
}
