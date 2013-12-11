using System;
using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	//планировщик для использования в тестах
	//фактически это просто обертка вокруг testscheduler
	//отличие в том что когда планировщик используется не для отложенного выполнения а для синхронизации
	//testscheduler непригоден тк событие которое потребует синхронизации произойдет в неопределенный момент времени
	//и узнать когда нужно вызвать advenceby в общем случае непозможно
	//этот планировщик обходит эту проблему делегирую выполнение в ситуации синхронзиции
	public class MixedScheduler : IScheduler
	{
		private TestScheduler testScheduler;
		private IScheduler delegateScheduler;

		public MixedScheduler(TestScheduler testScheduler, IScheduler delegateScheduler)
		{
			this.testScheduler = testScheduler;
			this.delegateScheduler = delegateScheduler;
		}

		public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
		{
			return delegateScheduler.Schedule(state, action);
		}

		public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
		{
			return testScheduler.Schedule(state, dueTime, action);
		}

		public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
		{
			return testScheduler.Schedule(state, dueTime, action);
		}

		public DateTimeOffset Now
		{
			get { return testScheduler.Now; }
		}
	}
}