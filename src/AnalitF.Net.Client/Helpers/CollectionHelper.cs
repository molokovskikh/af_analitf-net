using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Common.Tools;

namespace AnalitF.Net.Client.Helpers
{
	public class CollectionHelper
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
	}
}