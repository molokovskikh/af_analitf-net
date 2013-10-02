using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using ReactiveUI;

namespace AnalitF.Net.Client.Helpers
{
	public static class ObservableHelper
	{
		public static IObservable<EventPattern<PropertyChangedEventArgs>> Changed(this INotifyPropertyChanged self)
		{
			return Observable.FromEventPattern<PropertyChangedEventArgs>(self, "PropertyChanged");
		}

		public static IObservable<EventPattern<ListChangedEventArgs>> Changed<T>(this IList<T> value)
		{
			return Observable.FromEventPattern<ListChangedEventArgs>(value, "ListChanged");
		}
	}
}