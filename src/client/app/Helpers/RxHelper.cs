﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Devart.Common;
using ReactiveUI;
using ILog = log4net.ILog;
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

		public static NotifyValue<T> ToValue<T>(this IObservable<T> observable, CancellationDisposable cancellation)
		{
			return new NotifyValue<T>(observable, cancellation);
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
#if !DEBUG
				try {
#endif
					onNext(e);
#if !DEBUG
				}
				catch(Exception ex) {
					log.Error("Ошибка при обработке задачи", ex);
				}
#endif
			});
		}

		public static void CatchSubscribe<T>(this IObservable<T> observable, Action<T> onNext, CancellationDisposable cancellation)
		{
			observable.Subscribe(e => {
#if !DEBUG
				try {
#endif
					onNext(e);
#if !DEBUG
				}
				catch(Exception ex) {
					log.Error("Ошибка при обработке задачи", ex);
				}
#endif
			}, cancellation.Token);
		}

		//фактический это просто переписанный SequentialResult
		public static IObservable<IResult> ToObservable(IEnumerable<IResult> results)
		{
			if (results == null)
				return Observable.Empty<IResult>();

			return Observable.Create<IResult>(o => {
				var cancellation = new CancellationDisposable();
				try {
					var enumerator = results.GetEnumerator();
					CollectResult(enumerator, o, cancellation);
				}
				catch(Exception e) {
					o.OnError(e);
				}
				return cancellation;
			});
		}

		private static void CollectResult(IEnumerator<IResult> enumerator, IObserver<IResult> observer, CancellationDisposable cancellation)
		{
			try {
				if (cancellation.Token.IsCancellationRequested || !enumerator.MoveNext())
					observer.OnCompleted();
				IoC.BuildUp(enumerator.Current);
			}
			catch(Exception e) {
				observer.OnError(e);
				return;
			}
			enumerator.Current.Completed += (sender, args) => {
				if (args.WasCancelled)
					observer.OnCompleted();
				else
					CollectResult(enumerator, observer, cancellation);
			};
			observer.OnNext(enumerator.Current);
		}
	}
}