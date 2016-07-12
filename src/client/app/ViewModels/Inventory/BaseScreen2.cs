using System;
using System.Reactive.Linq;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class BaseScreen2 : BaseScreen
	{
		public BaseScreen2()
		{
			InitFields();
		}

		public override IObservable<T> RxQuery<T>(Func<IStatelessSession, T> @select)
		{
			return base.RxQuery(@select).ObserveOn(UiScheduler);
		}
	}
}