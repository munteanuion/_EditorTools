using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


namespace ImpRock.JumpTo
{
	internal abstract class JumpLinkContainer<T> : ScriptableObject where T : JumpLink
	{
		[SerializeField] protected List<T> m_Links = new List<T>();
		[SerializeField] protected int m_ActiveSelection = -1;
		[SerializeField] protected int m_SelectionCount = 0;
		

		public List<T> Links { get { return m_Links; } }
		public int ActiveSelection { get { return m_ActiveSelection; } set { m_ActiveSelection = Mathf.Clamp(value, -1, m_Links.Count - 1); } }
		public T ActiveSelectedObject { get { return m_ActiveSelection > -1 ? m_Links[m_ActiveSelection] : null; } }
		public bool HasSelection { get { return m_SelectionCount > 0; } }
		public int SelectionCount { get { return m_SelectionCount; } }


		public T this[int index]
		{
			get
			{
				if (index < 0 || index >= m_Links.Count)
					throw new System.IndexOutOfRangeException(typeof(T) + ": Link index out of range (" + index + "/" + m_Links.Count + ")");

				return m_Links[index];
			}
		}

		public T[] SelectedLinks
		{
			get
			{
				if (m_Links.Count == 0)
				{
					return null;
				}
				else
				{
					List<T> selection = new List<T>();
					for (int i = 0; i < m_Links.Count; i++)
					{
						if (m_Links[i].Selected)
							selection.Add(m_Links[i]);
					}

					return selection.ToArray();
				}
			}
		}

		public Object[] SelectedLinkReferences
		{
			get
			{
				if (m_Links.Count == 0)
					return null;
				else
				{
					List<Object> selection = new List<Object>();
					for (int i = 0; i < m_Links.Count; i++)
					{
						if (m_Links[i].Selected)
							selection.Add(m_Links[i].LinkReference);
					}

					return selection.ToArray();
				}
			}
		}

		public Object[] AllLinkReferences
		{
			get
			{
				if (m_Links.Count == 0)
					return null;
				else
				{
					Object[] linkRefs = new Object[m_Links.Count];
					for (int i = 0; i < m_Links.Count; i++)
					{
						linkRefs[i] = m_Links[i].LinkReference;
					}

					return linkRefs;
				}
			}
		}


		public event System.Action OnLinksChanged;


		public abstract void AddLink(UnityEngine.Object linkReference, PrefabAssetType prefabAssetType, PrefabInstanceStatus prefabInstanceStatus);
		protected abstract void UpdateLinkInfo(T link, PrefabAssetType prefabAssetType, PrefabInstanceStatus prefabInstanceStatus);


		public void RemoveLink(int index)
		{
			if (index < 0 || index > m_Links.Count - 1)
				return;

			if (m_Links[index].Selected)
				--m_SelectionCount;

			Object.DestroyImmediate(m_Links[index]);
			m_Links.RemoveAt(index);

			for (int i = index; i < m_Links.Count; i++)
			{
				m_Links[i].Area.y = i * GraphicAssets.LinkHeight;
			}

			OnLinksChanged?.Invoke();
		}

		public void RemoveSelected()
		{
			for (int i = m_Links.Count - 1; i >= 0; i--)
			{
				if (m_Links[i].Selected)
				{
					Object.DestroyImmediate(m_Links[i]);
					m_Links.RemoveAt(i);
				}
			}

			m_ActiveSelection = -1;

			RefreshLinksY();

			m_SelectionCount = 0;

			OnLinksChanged?.Invoke();
		}

		public void RemoveNonSelected()
		{
			T active = ActiveSelectedObject;

			for (int i = m_Links.Count - 1; i >= 0; i--)
			{
				if (!m_Links[i].Selected)
				{
					Object.DestroyImmediate(m_Links[i]);
					m_Links.RemoveAt(i);
				}
			}

			m_ActiveSelection = m_Links.IndexOf(active);

			RefreshLinksY();

			OnLinksChanged?.Invoke();
		}

		public void RemoveAll()
		{
			m_Links.Clear();
			m_ActiveSelection = -1;

			OnLinksChanged?.Invoke();
		}

		public void MoveLink(int from, int to)
		{
			//can't move a link to itself
			if (from == to)
				return;

			//get the link, then remove it from the list
			T link = m_Links[from];
			m_Links.RemoveAt(from);

			//find the range of links that will shift
			//	within the list as a result of the move
			int min;
			int max;
			if (to > from)
			{
				to--;

				min = from;
				max = to + 1;
			}
			else
			{
				min = to;
				max = from + 1;
			}

			//re-insert the link
			m_Links.Insert(to, link);

			//adjust the y-positions of the affected links
			//	instead of all of the links
			for (; min < max; min++)
			{
				m_Links[min].Area.y = min * GraphicAssets.LinkHeight;
			}

			OnLinksChanged?.Invoke();
		}

		public void MoveSelected(int to)
		{
			//get the indices of the selected links
			List<int> selection = new List<int>();
			for (int i = 0; i < m_Links.Count; i++)
			{
				if (m_Links[i].Selected)
				{
					//early return if trying to move the
					//	selection into itself
					if (i == to)
						return;

					selection.Add(i);
				}
			}

			//grab an array of the selected object before removal
			T[] selectionObjects = SelectedLinks;

			//remove the selected links from the list
			int toAdjusted = to;
			for (int i = selection.Count - 1; i >= 0; i--)
			{
				m_Links.RemoveAt(selection[i]);

				//modify the insertion index for selected links
				//	above it
				if (selection[i] < to)
					toAdjusted--;
			}

			//re-insert the selection
			m_Links.InsertRange(toAdjusted, selectionObjects);

			m_ActiveSelection = toAdjusted;

			//fix all of the y-positions
			RefreshLinksY();

			OnLinksChanged?.Invoke();
		}

		public void RefreshLinkSelections()
		{
			for (int i = 0; i < m_Links.Count; i++)
			{
				m_Links[i].Selected = false;
			}
			
			m_SelectionCount = 0;

			Object[] selectedObjects = Selection.objects;
			T link = null;
			for (int i = 0; i < selectedObjects.Length; i++)
			{
				link = m_Links.Find(l => l.LinkReference == selectedObjects[i]);
				if (link != null)
				{
					link.Selected = true;
					++m_SelectionCount;
				}
			}
		}

		public void LinkSelectionSet(int index)
		{
			for (int i = 0; i < m_Links.Count; i++)
			{
				m_Links[i].Selected = i == index;
			}

			m_ActiveSelection = Mathf.Clamp(index, -1, m_Links.Count - 1);
			m_SelectionCount = 1;
		}

		public void LinkSelectionSetRange(int from, int to)
		{
			if (from > to)
			{
				int temp = to;
				to = from;
				from = temp;
			}

			for (int i = 0; i < from; i++)
			{
				m_Links[i].Selected = false;
			}

			for (int i = from; i <= to; i++)
			{
				m_Links[i].Selected = true;
			}

			for (int i = to + 1; i < m_Links.Count; i++)
			{
				m_Links[i].Selected = false;
			}

			m_SelectionCount = (to - from) + 1;
		}

		public void LinkSelectionInvert()
		{
			m_SelectionCount = m_Links.Count - m_SelectionCount;

			T link;
			for (int i = 0; i < m_Links.Count; i++)
			{
				link = m_Links[i];
				link.Selected = !link.Selected;
			}
		}

		public void LinkSelectionAdd(int index)
		{
			if (index >= 0 && index < m_Links.Count)
			{
				m_Links[index].Selected = true;
				m_ActiveSelection = index;
				++m_SelectionCount;
			}
		}

		public void LinkSelectionRemove(int index)
		{
			if (index >= 0 && index < m_Links.Count)
			{
				m_Links[index].Selected = false;
				m_ActiveSelection = index;
				--m_SelectionCount;
			}
		}

		public void LinkSelectionAll()
		{
			for (int i = 0; i < m_Links.Count; i++)
			{
				m_Links[i].Selected = true;
			}

			m_SelectionCount = m_Links.Count;
		}
		
		public void LinkSelectionClear()
		{
			for (int i = 0; i < m_Links.Count; i++)
			{
				m_Links[i].Selected = false;
			}

			m_ActiveSelection = -1;
			m_SelectionCount = 0;
		}

		public void LinkSelectionSetUnitySelection()
		{
			Object[] selectedLinksReferences = SelectedLinkReferences;
			if (selectedLinksReferences == null)
			{
				selectedLinksReferences = new Object[0];
			}

			Selection.objects = selectedLinksReferences;
		}

		public void LinkSelectionAddToUnitySelection()
		{
			//TODO: make this more efficient
			Object[] selectedObjects = Selection.objects;
			List<Object> totalSelectedObjects = new List<Object>();
			for (int i = 0; i < m_Links.Count; i++)
			{
				//add selected objects to the list
				if (m_Links[i].Selected)
				{
					totalSelectedObjects.Add(m_Links[i].LinkReference);
				}
				//filter non-selected from current selection
				else if (selectedObjects.Length > 0)
				{
					int index = System.Array.IndexOf(selectedObjects, m_Links[i].LinkReference);
					if (index > -1)
						selectedObjects[index] = null;
				}
			}

			for (int i = 0; i < selectedObjects.Length; i++)
			{
				if (selectedObjects[i] != null)
					totalSelectedObjects.Add(selectedObjects[i]);
			}
			
			Selection.objects = totalSelectedObjects.ToArray();
		}

		public virtual void RefreshLinks()
		{
			bool linksRemoved = false;

			for (int i = m_Links.Count - 1; i >= 0; i--)
			{
				if (m_Links[i].LinkReference == null)
				{
					Object.DestroyImmediate(m_Links[i]);
					m_Links.RemoveAt(i);

					linksRemoved = true;
				}
				else
				{
					T link = m_Links[i];
					Object linkReference = link.LinkReference;
					UpdateLinkInfo(link, PrefabUtility.GetPrefabAssetType(linkReference), PrefabUtility.GetPrefabInstanceStatus(linkReference));
				}
			}

			RefreshLinksY();

			if (linksRemoved && OnLinksChanged != null)
				OnLinksChanged();
		}

		public void RefreshLinksY()
		{
			for (int i = 0; i < m_Links.Count; i++)
			{
				m_Links[i].Area.y = i * GraphicAssets.LinkHeight;
			}
		}

		public int LinkHitTest(Vector2 position)
		{
			int linkCount = m_Links.Count;
			if (linkCount > 0)
			{
				if (linkCount == 1)
				{
					if (m_Links[0].Area.RectInternal.Contains(position))
						return 0;
				}
				else
				{
					int indexMin = 0;
					int indexMax = linkCount - 1;
					while (indexMax >= indexMin)
					{
						int index = (indexMin + indexMax) >> 1;

						RectRef rect = m_Links[index].Area;

						//if below the link rect
						if (rect.yMax < position.y)
						{
							indexMin = index + 1;
						}
						//if above the link rect
						else if (rect.yMin > position.y)
						{
							indexMax = index - 1;
						}
						//if within the link rect
						else if (position.x >= rect.xMin && position.x <= rect.xMax)
						{
							return index;
						}
						//not within any link rect
						else
						{
							break;
						}
					}
				}
			}

			return -1;
		}

		public bool SelectionHitTest(Vector2 position)
		{
			for (int i = 0; i < m_Links.Count; i++)
			{
				if (m_Links[i].Selected && m_Links[i].Area.RectInternal.Contains(position))
					return true;
			}

			return false;
		}

		protected void RaiseOnLinksChanged()
		{
			OnLinksChanged?.Invoke();
		}
	}
}
