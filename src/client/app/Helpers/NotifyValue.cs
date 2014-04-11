using System;
using System.ComponentModel;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using log4net;
using NHibernate;
using ReactiveUI;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client.Helpers
{
	public class NotifyValue<T> : BaseNotify, IObservable<T>
	{
		private bool respectValue;
		private Func<T> calc;
		private T value;
		private Subject<object> refreshSubject;

		public NotifyValue()
		{
		}

		/// <summary>
		/// этот конструктор предназначен для создания поля с изменяемым начальным значением
		/// суть в том что это поле которое можно редактировать, но у него есть начальное значение
		/// которое зависит от других полей
		/// </summary>
		public NotifyValue(bool respectValue, Func<T> calc, params INotifyPropertyChanged[] props)
			: this(calc, props)
		{
			this.respectValue = respectValue;
		}

		/// <summary>
		/// конструктор создает поле с начальным значением и функцией которая вычисляет следующее значение
		/// следующее значение поля будет вычислено когда изменится одно из зависимых полей
		/// или будет вызван метод Recalculate
		/// </summary>
		public NotifyValue(T value, Func<T> calc, params INotifyPropertyChanged[] props)
		{
			this.value = value;
			this.calc = calc;
			foreach (var prop in props)
				prop.PropertyChanged += (s, a) => Recalculate();
		}

		public NotifyValue(Func<T> calc, params INotifyPropertyChanged[] props)
			: this(default(T), calc, props)
		{
			Recalculate();
		}

		public NotifyValue(T value)
		{
			this.value = value;
		}

		public NotifyValue(IObservable<T> observable, CancellationDisposable cancellation = null, Subject<object> refreshSubject = null)
		{
			this.refreshSubject = refreshSubject;
			observable.CatchSubscribe(v => Value = v, cancellation);
		}

		public T Value
		{
			get
			{
				return value;
			}
			set
			{
				if (Equals(this.value, value))
					return;

				if (respectValue)
					calc = null;

				this.value = value;
				OnPropertyChanged("HasValue");
				OnPropertyChanged();
			}
		}

		public bool HasValue
		{
			get
			{
				return !Equals(value, default(T));
			}
		}

		public void Recalculate()
		{
			if (calc != null) {
				var origin = respectValue;
				respectValue = false;
				Value = calc();
				respectValue = origin;
			}
			if (refreshSubject != null) {
				refreshSubject.OnNext(null);
			}
		}

		public void Mute(T value)
		{
			this.value = value;
		}

		public static implicit operator T(NotifyValue<T> value)
		{
			return value.value;
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			return this.ToObservable().Merge(Observable.Return(Value)).Subscribe(observer);
		}

		public override string ToString()
		{
			if (Equals(value, null))
				return String.Empty;
			return value.ToString();
		}

		/// <summary>
		/// Будь бдителен если внутри NotifyValue лежит список то метод не даст ни какого результата
		/// </summary>
		public void Refresh()
		{
			OnPropertyChanged("Value");
		}
	}

	public class NotifyValueHelper
	{
		public static IDisposable LiveValue<T>(NotifyValue<T> value,
			IMessageBus bus,
			IScheduler scheduler,
			ISession session)
		{
			return bus.Listen<T>()
				.ObserveOn(scheduler)
				.Subscribe(_ => {
					session.Refresh(value.Value);
					value.Refresh();
				});
		}
	}
}