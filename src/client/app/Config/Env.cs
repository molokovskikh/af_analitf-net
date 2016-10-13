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
	public class Env : IDisposable
	{
		private TaskScheduler tplUiScheduler;
		/// <summary>
		/// использовать только через RxQuery,
		/// живет на протяжении жизни всего приложения и будет закрыто при завершении приложения
		/// </summary>
		private IStatelessSession BackgroundSession;

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

		public IObservable<T> RxQuery<T>(Func<IStatelessSession, T> select, CancellationToken token = default(CancellationToken))
		{
			if (Factory == null)
				return Observable.Empty<T>();
			var task = Query(@select, token);
			//игнорируем отмену задачи, она произойдет если закрыли форму
			return Observable.FromAsync(() => task).Catch<T, TaskCanceledException>(_ => Observable.Empty<T>())
				.ObserveOn(UiScheduler);
		}

		public Task<T> Query<T>(Func<IStatelessSession, T> @select, CancellationToken token = default(CancellationToken))
		{
			var task = new Task<T>(() => {
				if (Factory == null)
					return default(T);
				if (BackgroundSession == null)
					BackgroundSession = Factory.OpenStatelessSession();
				return @select(BackgroundSession);
			});
			//в жизни это невозможно, но в тестах мы можем отменить задачу до того как она запустится
			if (!task.IsCanceled)
				task.Start(QueryScheduler);
			return task;
		}

		public Task Query(Action<IStatelessSession> action, CancellationToken token = default(CancellationToken))
		{
			var task = new Task(() => {
				if (Factory == null)
					return;
				if (BackgroundSession == null)
					BackgroundSession = Factory.OpenStatelessSession();
				action(BackgroundSession);
			}, token);
			//в жизни это невозможно, но в тестах мы можем отменить задачу до того как она запустится
			if (!task.IsCanceled)
				task.Start(QueryScheduler);
			return task;
		}

		public void Dispose()
		{
			if (BackgroundSession != null) {
				var task = new Task(() => {
					BackgroundSession?.Dispose();
					BackgroundSession = null;
				});
				task.Start(QueryScheduler);
				try {
					if (!task.Wait(TimeSpan.FromSeconds(20))) {
						BackgroundSession?.Dispose();
						BackgroundSession = null;
					}
				} catch(Exception) {
//есть смысл кидать только в дебаге тк в реальной жизни мы ничего сделать не сможем
#if DEBUG
					throw;
#endif
				}
			}
		}

		~Env()
		{
			GC.SuppressFinalize(this);
			Dispose();
		}
	}
}