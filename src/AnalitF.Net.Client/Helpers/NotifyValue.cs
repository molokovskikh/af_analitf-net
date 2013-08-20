using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using NHibernate;
using ReactiveUI;

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

		public NotifyValue(bool respectValue, Func<T> calc, params INotifyPropertyChanged[] props)
			: this(calc, props)
		{
			this.respectValue = respectValue;
		}

		public NotifyValue(Func<T> calc, params INotifyPropertyChanged[] props)
		{
			this.calc = calc;
			Recalculate();

			foreach (var prop in props)
				prop.PropertyChanged += (s, a) => Recalculate();
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

		public IObservable<EventPattern<PropertyChangedEventArgs>> Changed()
		{
			return Observable.FromEventPattern<PropertyChangedEventArgs>(this, "PropertyChanged");
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