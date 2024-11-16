using UnityEditor;
using UnityEngine;


namespace ImpRock.JumpTo
{
	internal abstract class EditorScriptableObject<T> : ScriptableObject where T : EditorScriptableObject<T>
	{
		public static T Create()
		{
			T instance = ScriptableObject.CreateInstance<T>();
			instance.hideFlags = HideFlags.HideAndDontSave;

			instance.Initialize();

			return instance;
		}

		public void Destroy()
		{
			Object.DestroyImmediate(this);
		}


		protected virtual void Initialize() { }

		public virtual void Uninitialize() { }
	}
}
