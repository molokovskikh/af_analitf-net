using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate;

namespace AnalitF.Net.Client.Helpers
{
	public class PersistentList<T> : IList<T>, IList
	{
		private ISession session;
		private IList<T> list;

		public PersistentList(IList<T> list, ISession session)
		{
			this.list = list;
			this.session = session;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
			session.Save(item);
			list.Add(item);
		}

		public int Add(object value)
		{
			Add((T)value);
			return Count - 1;
		}

		public bool Contains(object value)
		{
			return Contains((T)value);
		}

		public void Clear()
		{
			foreach (var item in list)
				session.Delete(item);
			list.Clear();
		}

		public int IndexOf(object value)
		{
			return IndexOf((T)value);
		}

		public void Insert(int index, object value)
		{
			Insert(index, (T)value);
		}

		public void Remove(object value)
		{
			Remove((T)value);
		}

		public bool Contains(T item)
		{
			return list.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			list.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			session.Delete(item);
			return list.Remove(item);
		}

		public void CopyTo(Array array, int index)
		{
			CopyTo((T[])array, index);
		}

		public int Count
		{
			get { return list.Count; }
		}

		public object SyncRoot
		{
			get { return null; }
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return list.IsReadOnly; }
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		public int IndexOf(T item)
		{
			return list.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			session.Save(item);
			list.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			session.Delete(list[index]);
			list.RemoveAt(index);
		}

		object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (T)value; }
		}

		public T this[int index]
		{
			get { return list[index]; }
			set { list[index] = value; }
		}
	}
}