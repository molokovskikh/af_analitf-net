using System;
using System.ComponentModel;

namespace AnalitF.Net.Client.Helpers
{
	public class NotifyValue<T> : INotifyPropertyChanged
	{
		private Func<T> calc;

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

		private void Reclculate(object sender, PropertyChangedEventArgs e)
		{
			Value = calc();
		}

		public NotifyValue(T value)
		{
			this.value = value;
		}

		private T value;

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

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
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
	}
}