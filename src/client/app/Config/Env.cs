using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Common.Tools.Calendar;
using NHibernate;
using ReactiveUI;

namespace AnalitF.Net.Client.Config
{
	public class Env
	{
		private TaskScheduler tplUiScheduler;

		public TimeSpan RequestDelay = TimeSpan.Zero;
		//механизм синхронизации для тестов
		public Barrier Barrier;
#if DEBUG
		public bool IsUnitTesting;
#endif
		public IScheduler Scheduler;
		public IScheduler UiScheduler;
		public IMessageBus Bus;
		//планировщик для выполнения запросов
		//нужен тк mysql требует что бы запросы производились в той же нитке что и инициализировала подключение
		//фактически это очередь задач которая обрабатывается одной ниткой глобальной для всего приложения
		public TaskScheduler QueryScheduler;
		public ISessionFactory Factory;

		//для тестирования
		public User User;
		public Settings Settings;
		public List<Address> Addresses = new List<Address>();
		public static Env Current;

		public Env(User user, IMessageBus bus, IScheduler scheduler, ISessionFactory factory)
		{
#if DEBUG
			IsUnitTesting = true;
#endif
			Bus = bus;
			Scheduler = scheduler;
			UiScheduler = scheduler;
			User = user;
			Factory = factory;
		}

		public Env()
		{
			Bus = RxApp.MessageBus;
			Scheduler = DefaultScheduler.Instance;
			UiScheduler = DispatcherScheduler.Current;
			Factory = AppBootstrapper.NHibernate.Factory;
			QueryScheduler = new QueueScheduler();
		}

		public TaskScheduler TplUiScheduler
		{
			get { return tplUiScheduler = tplUiScheduler ?? TaskScheduler.FromCurrentSynchronizationContext(); }
			set { tplUiScheduler = value; }
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

		public IObservable<T> RxQuery<T>(Func<IStatelessSession, T> select)
		{
			var task = new Task<T>(() => {
				if (Factory == null)
					return default(T);
				if (BaseScreen.BackgroundSession == null)
					BaseScreen.BackgroundSession = Factory.OpenStatelessSession();
					return @select(BaseScreen.BackgroundSession);
			});
			//в жизни это невозможно, но в тестах мы можем отменить задачу до того как она запустится
			if (!task.IsCanceled)
				task.Start(QueryScheduler);
			//игнорируем отмену задачи, она произойдет если закрыли форму
			return Observable.FromAsync(() => task).Catch<T, TaskCanceledException>(_ => Observable.Empty<T>());
		}
	}
}