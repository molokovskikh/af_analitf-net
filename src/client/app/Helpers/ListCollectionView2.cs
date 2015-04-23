using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;

namespace AnalitF.Net.Client.Helpers
{
	public class ListCollectionView2<T> : ListCollectionView, IList<T>
	{
		public ListCollectionView2() : base(new List<T>())
		{
		}

		public ListCollectionView2(IList<T> list) : base((IList)list)
		{
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this.OfType<T>().GetEnumerator();
		}

		public void Add(T item)
		{
			this.AddNewItem(item);
		}

		public void Clear()
		{
			throw new System.NotImplementedException();
		}

		public bool Contains(T item)
		{
			return base.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new System.NotImplementedException();
		}

		public bool Remove(T item)
		{
			base.Remove(item);
			return true;
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public int IndexOf(T item)
		{
			return base.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			throw new System.NotImplementedException();
		}

		public T this[int index]
		{
			get { return (T)GetItemAt(index); }
			set { throw new System.NotImplementedException(); }
		}
	}
}