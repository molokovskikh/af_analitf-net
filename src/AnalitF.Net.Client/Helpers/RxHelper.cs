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
	public static class RxHelper
	{
		public static IObservable<T> Dump<T>(this IObservable<T> observable)
		{
			return observable.Do(i => Console.WriteLine(i));
		}

		public static IObservable<EventPattern<PropertyChangedEventArgs>> Changed(this INotifyPropertyChanged self)
		{
			return Observable.FromEventPattern<PropertyChangedEventArgs>(self, "PropertyChanged");
		}

		public static IObservable<EventPattern<ListChangedEventArgs>> Changed<T>(this IList<T> value)
		{
			return Observable.FromEventPattern<ListChangedEventArgs>(value, "ListChanged");
		}

		public static List<PropertyChangedEventArgs> CollectChanges(this INotifyPropertyChanged source)
		{
			return Collect(source.Changed().Select(e => e.EventArgs));
		}

		public static List<T> Collect<T>(this IObservable<T> source)
		{
			var items = new List<T>();
			source.Subscribe(items.Add);
			return items;
		}
	}
}