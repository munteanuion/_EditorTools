using UnityEditor;
using UnityEngine;


namespace ImpRock.JumpTo
{
	internal abstract class GuiBase : ScriptableObject
	{
		protected Vector2 m_Size;


		public virtual void OnWindowEnable(EditorWindow window) { }
		public virtual void OnWindowDisable(EditorWindow window) { }
		public virtual void OnWindowClose(EditorWindow window) { }

		protected abstract void OnGui();

		protected virtual void OnCreate() { }
		protected virtual void OnDestroy() { }

		public virtual void OnSerialize() { }
		public virtual void OnDeserialize() { }

		/// <summary>
		/// Reset internal draw position to (0,0), m_Size to position size
		/// </summary>
		/// <param name="position">Width & height determine size</param>
		public void Draw(RectRef position)
		{
			//make all gui things within OnGui() relative
			//	to position
			GUI.BeginGroup(position);

			m_Size.x = position.width;
			m_Size.y = position.height;
			OnGui();

			GUI.EndGroup();
		}

		public static T Create<T>() where T : GuiBase
		{
			T instance = ScriptableObject.CreateInstance<T>();
			instance.hideFlags = HideFlags.HideAndDontSave;

			instance.OnCreate();

			return instance;
		}
	}
}
