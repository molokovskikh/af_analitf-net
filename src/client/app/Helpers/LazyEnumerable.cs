using System;
using System.Collections;
using System.Collections.Generic;

namespace AnalitF.Net.Client.Helpers
{
	public class LazyHelper
	{
		public static IEnumerable<T> Create<T>(Func<IEnumerable<T>> func)
		{
			return new LazyEnumerable<T>(func);
		}
	}

	public class LazyEnumerable<T> : IEnumerable<T>
	{
		public Func<IEnumerable<T>> func;

		public LazyEnumerable(Func<IEnumerable<T>> func)
		{
			this.func = func;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return func().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}