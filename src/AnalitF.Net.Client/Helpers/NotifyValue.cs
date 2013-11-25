using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using log4net;
using NHibernate;
using ReactiveUI;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client.Helpers
{
	public class NotifyValue<T> : BaseNotify
	{
		private bool respectValue;
		private Func<T> calc;
		private T value;

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

		public NotifyValue(Func<T> calc, IObservable<object> trigger)
			: this(calc)
		{
			trigger.CatchSubscribe(_ => Recalculate());
		}

		public NotifyValue(T value)
		{
			this.value = value;
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
				OnPropertyChanged("Value");
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
		}

		public void Mute(T value)
		{
			this.value = value;
		}

		public static implicit operator T(NotifyValue<T> value)
		{
			return value.value;
		}

		public override string ToString()
		{
			if (Equals(value, null))
				return String.Empty;
			return value.ToString();
		}

		public IObservable<EventPattern<PropertyChangedEventArgs>> ChangedValue()
		{
			return this.ObservableForProperty(v => v.Value)
				.Select(v => v.Value as INotifyPropertyChanged)
				.Select(v => v == null
					? Observable.Empty<EventPattern<PropertyChangedEventArgs>>()
					: Observable.FromEventPattern<PropertyChangedEventArgs>(v, "PropertyChanged"))
				.Switch();
		}

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