using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Common.Tools;
using Devart.Common;

namespace AnalitF.Net.Client.Helpers
{
	public static class CollectionHelper
	{
		public static void Bind(IList source, IList target, Action<object> add = null, Action<object> remove = null)
		{
			add = add ?? (i => target.Add(i));
			remove = remove ?? (target.Remove);
			((INotifyCollectionChanged)source).CollectionChanged += (sender, args) => {
				if (args.Action == NotifyCollectionChangedAction.Reset) {
					target.Clear();
					args.NewItems.Cast<object>().Each(i => target.Add(i));
				}
				else {
					if (args.OldItems != null)
						args.OldItems.Cast<object>().Each(remove);

					if (args.NewItems != null)
						args.NewItems.Cast<object>().Each(add);
				}
			};
		}

		public static IList<T> LinkTo<T>(this IEnumerable<T> src, IList<T> dst,
			Action<object> add = null, Action<object> remove = null)
		{
			var result = src.ToObservableCollection();
			Bind(result, (IList)dst, add, remove);
			return result;
		}

		public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> collection)
		{
			return new ObservableCollection<T>(collection);
		}
	}
}