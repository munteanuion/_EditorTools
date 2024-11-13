using UnityEngine;
using System.Collections.Generic;
using System.Collections;


namespace ImpRock.JumpTo
{
	[System.Serializable]
	public class Faketionary<K, V> : IEnumerable<KeyValuePair<K, V>>
	{
		[SerializeField] private List<K> m_Keys = new List<K>();
		[SerializeField] private List<V> m_Values = new List<V>();


		public List<K> Keys { get { return new List<K>(m_Keys); } }
		public List<V> Values { get { return new List<V>(m_Values); } }
		public int Count { get { return m_Keys.Count; } }

		public V this[K key]
		{
			get
			{
				int index = m_Keys.IndexOf(key);
				if (index == -1)
				{
					return default(V);
				}

				return m_Values[index];
			}

			set
			{
				Add(key, value);
			}
		}


		public void Add(K key, V value)
		{
			int index = m_Keys.IndexOf(key);
			if (index == -1)
			{
				m_Keys.Add(key);
				m_Values.Add(value);
			}
			else
			{
				m_Values[index] = value;
			}
		}

		public bool Remove(K key)
		{
			int index = m_Keys.IndexOf(key);
			if (index > -1)
			{
				m_Keys.RemoveAt(index);
				m_Values.RemoveAt(index);

				return true;
			}

			return false;
		}

		public bool TryGetValue(K key, out V value)
		{
			int index = m_Keys.IndexOf(key);
			if (index == -1)
			{
				value = default(V);
				return false;
			}

			value = m_Values[index];
			return true;
		}

		public bool ContainsKey(K key)
		{
			return m_Keys.Contains(key);
		}

		public void Clear()
		{
			m_Keys.Clear();
			m_Values.Clear();
		}

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
		{
			int count = m_Keys.Count;
			for (int i = 0; i < count; i++)
			{
				yield return new KeyValuePair<K, V>(m_Keys[i], m_Values[i]);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
