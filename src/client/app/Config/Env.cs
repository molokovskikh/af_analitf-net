using System;
using System.Reactive.Linq;
using System.Threading;
using Common.Tools.Calendar;
using NHibernate;

namespace AnalitF.Net.Client.Config
{
	public class Env
	{
		public TimeSpan RequestDelay = TimeSpan.Zero;
		//механизм синхронизации для тестов
		public Barrier Barrier;

		public ISessionFactory Factory
		{
			get { return AppBootstrapper.NHibernate.Factory; }
		}

		public Tuple<IObservable<T1>, IObservable<T2>>
			WrapRequest<T1, T2>(Tuple<IObservable<T1>, IObservable<T2>> result)
		{
			if (RequestDelay != TimeSpan.Zero)
				return Tuple.Create(result.Item1, result.Item2.Delay(RequestDelay));
			if (Barrier != null)
				return Tuple.Create(result.Item1, result.Item2.Do(_ => {
					Barrier.SignalAndWait();
				}, _ => {
					Barrier.RemoveParticipant();
				}));
			return result;
		}
	}
}