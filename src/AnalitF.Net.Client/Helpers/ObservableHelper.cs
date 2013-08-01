using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;

namespace AnalitF.Net.Client.Helpers
{
	public static class ObservableHelper
	{
		public static IObservable<EventPattern<PropertyChangedEventArgs>> Changed(this INotifyPropertyChanged self)
		{
			return Observable.FromEventPattern<PropertyChangedEventArgs>(self, "PropertyChanged");
		}
	}
}