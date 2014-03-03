using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using log4net;
using ReactiveUI;
using LogManager = log4net.LogManager;

namespace AnalitF.Net.Client.Helpers
{
	public static class RxHelper
	{
		private static ILog log = LogManager.GetLogger(typeof(RxHelper));

		public static IObservable<T> Dump<T>(this IObservable<T> observable)
		{
			return observable.Do(i => Console.WriteLine(i), e => Console.WriteLine(e));
		}

		public static IObservable<EventPattern<PropertyChangedEventArgs>> Changed(this INotifyPropertyChanged self)
		{
			return Observable.FromEventPattern<PropertyChangedEventArgs>(self, "PropertyChanged");
		}

		public static IObservable<EventPattern<PropertyChangedEventArgs>> Changed<T>(this NotifyValue<T> self)
		{
			return Observable.FromEventPattern<PropertyChangedEventArgs>(self, "PropertyChanged")
				.Where(e => e.EventArgs.PropertyName == "Value");
		}

		public static IObservable<EventPattern<NotifyCollectionChangedEventArgs>> Changed<T>(this ObservableCollection<T> self)
		{
			return Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(self, "CollectionChanged");
		}

		public static IObservable<EventPattern<ListChangedEventArgs>> Changed<T>(this IList<T> value)
		{
			if (value == null)
				Observable.Empty<ListChangedEventArgs>();
			return Observable.FromEventPattern<ListChangedEventArgs>(value, "ListChanged");
		}

		public static List<PropertyChangedEventArgs> CollectChanges(this INotifyPropertyChanged source)
		{
			return Collect(source.Changed().Select(e => e.EventArgs));
		}

		public static NotifyValue<T> ToValue<T>(this IObservable<T> observable)
		{
			return new NotifyValue<T>(observable);
		}

		public static List<T> Collect<T>(this IObservable<T> source)
		{
			var items = new List<T>();
			source.Subscribe(items.Add);
			return items;
		}

		public static IDisposable CatchSubscribe<T>(this IObservable<T> observable, Action<T> onNext)
		{
			return observable.Subscribe(e => {
				try {
					onNext(e);
				}
				catch(Exception ex) {
					log.Error("Ошибка при обработке задачи", ex);
				}
			});
		}
	}
}