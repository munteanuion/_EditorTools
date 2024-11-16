#pragma warning disable IDE1006 // Naming Styles: properties must be pascal-case

using UnityEngine;


namespace ImpRock.JumpTo
{
	[System.Serializable]
	internal class RectRef
	{
		[SerializeField] private Rect m_RectInternal;

		/// <summary>
		/// Returns a COPY of the internal rect
		/// </summary>
		public Rect RectInternal { get { return m_RectInternal; } }

		//pass-through properties

		public float x { get { return m_RectInternal.x; } set { m_RectInternal.x = value; } }
		public float y { get { return m_RectInternal.y; } set { m_RectInternal.y = value; } }
		public float width { get { return m_RectInternal.width; } set { m_RectInternal.width = value; } }
		public float height { get { return m_RectInternal.height; } set { m_RectInternal.height = value; } }

		public float xMin { get { return m_RectInternal.xMin; } set { m_RectInternal.xMin = value; } }
		public float xMax { get { return m_RectInternal.xMax; } set { m_RectInternal.xMax = value; } }
		public float yMin { get { return m_RectInternal.yMin; } set { m_RectInternal.yMin = value; } }
		public float yMax { get { return m_RectInternal.yMax; } set { m_RectInternal.yMax = value; } }


		public RectRef() { }

		public RectRef(Rect rect)
		{
			m_RectInternal = rect;
		}

		public RectRef(float x, float y, float width, float height)
		{
			m_RectInternal.Set(x, y, width, height);
		}

		public void Set(float x, float y, float width, float height)
		{
			m_RectInternal.Set(x, y, width, height);
		}

		public void Set(Rect rect)
		{
			m_RectInternal = rect;
		}

		public void Set(RectRef rect)
		{
			m_RectInternal = rect.m_RectInternal;
		}

		public override bool Equals(object obj)
		{
			return m_RectInternal.Equals(obj);
		}

		public override int GetHashCode()
		{
			return m_RectInternal.GetHashCode();
		}

		public override string ToString()
		{
			return m_RectInternal.ToString();
		}

		public static bool operator ==(RectRef rectA, RectRef rectB)
		{
			return rectA.m_RectInternal == rectB.m_RectInternal;
		}

		public static bool operator !=(RectRef rectA, RectRef rectB)
		{
			return rectA.m_RectInternal != rectB.m_RectInternal;
		}

		public static implicit operator Rect(RectRef rectRef)
		{
			return rectRef.m_RectInternal;
		}
	}
}

#pragma warning restore IDE1006 // Naming Styles
