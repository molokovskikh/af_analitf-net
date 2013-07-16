using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Common.Tools;

namespace AnalitF.Net.Client.Helpers
{
	public static class CollectionHelper
	{
		public static void Bind(IList source, IList target)
		{
			((INotifyCollectionChanged)source).CollectionChanged += (sender, args) => {
				if (args.Action == NotifyCollectionChangedAction.Reset) {
					target.Clear();
					args.NewItems.Cast<object>().Each(i => target.Add(i));
				}
				else {
					if (args.OldItems != null)
						args.OldItems.Cast<object>().Each(i => target.Remove(i));

					if (args.NewItems != null)
						args.NewItems.Cast<object>().Each(i => target.Add(i));
				}
			};
		}

		public static IList<T> LinkTo<T>(this IEnumerable<T> src, IList<T> dst)
		{
			var result = new ObservableCollection<T>(src.ToList());
			Bind(result, (IList)dst);
			return result;
		}
	}
}