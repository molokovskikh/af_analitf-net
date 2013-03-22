using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.Helpers
{
	public class NotifyValue<T> : INotifyPropertyChanged
	{
		private Func<T> calc;
		private T value;

		public event PropertyChangedEventHandler PropertyChanged;

		public NotifyValue()
		{
		}

		public NotifyValue(Func<T> calc, params INotifyPropertyChanged[] props)
		{
			this.calc = calc;
			Value = calc();
			foreach (var prop in props) {
				prop.PropertyChanged += Reclculate;
			}
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

				this.value = value;
				OnPropertyChanged("Value");
			}
		}

		private void Reclculate(object sender, PropertyChangedEventArgs e)
		{
			Value = calc();
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
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

		public IObservable<EventPattern<PropertyChangedEventArgs>> ValueUpdated()
		{
			return this.ObservableForProperty(v => v.Value)
				.Select(v => v.Value as INotifyPropertyChanged)
				.Select(v => v == null
					? Observable.Empty<EventPattern<PropertyChangedEventArgs>>()
					: Observable.FromEventPattern<PropertyChangedEventArgs>(v, "PropertyChanged"))
				.Switch();
		}
	}
}