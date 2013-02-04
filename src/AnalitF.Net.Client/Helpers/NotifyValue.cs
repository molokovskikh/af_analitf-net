using System;
using System.ComponentModel;

namespace AnalitF.Net.Client.Helpers
{
	public class NotifyValue<T> : INotifyPropertyChanged
	{
		public NotifyValue()
		{
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

		public event PropertyChangedEventHandler PropertyChanged;

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