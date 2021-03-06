﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Inflector;
using ReactiveUI;

namespace AnalitF.Net.Client.Config.Caliburn
{
	public class Caliburn
	{
		log4net.ILog log = log4net.LogManager.GetLogger(typeof(Caliburn));

		public void Init(bool failfast)
		{
			MessageBus.Current.RegisterScheduler<string>(ImmediateScheduler.Instance, "db");

			ConventionManager.Singularize = s => {
				if (s == "Taxes")
					return "Tax";
				if (s == "Value")
					return s;
				return s.Singularize() ?? s;
			};
			//нужно затем что бы можно было делать модели без суффикса ViewModel
			//достаточно что бы они лежали в пространстве имен ViewModels
			ViewLocator.NameTransformer.AddRule(
				@"(?<nsbefore>([A-Za-z_]\w*\.)*)(?<subns>ViewModels\.)"
				+ @"(?<nsafter>([A-Za-z_]\w*\.)*)(?<basename>[A-Za-z_]\w*)"
				+ @"(?!<suffix>ViewModel)$",
				"${nsbefore}Views.${nsafter}${basename}View");
			//что бы не нужно было использовать суффиксы View и ViewModel
			ViewLocator.NameTransformer.AddRule(
				@"(?<nsbefore>([A-Za-z_]\w*\.)*)(?<subns>ViewModels\.)"
				+ @"(?<nsafter>([A-Za-z_]\w*\.)*)(?<basename>[A-Za-z_]\w*)"
				+ @"(?!<suffix>)$",
				"${nsbefore}Views.${nsafter}${basename}");

			//безумие - сам по себе Caliburn если не найден view покажет текст Cannot find view for
			//ни исключения ни ошибки в лог
			ViewLocator.LocateForModelType = (modelType, displayLocation, context) => {
				var viewType = ViewLocator.LocateTypeForModelType(modelType, displayLocation, context);
				if (viewType == null) {
					if (failfast)
						throw new Exception(String.Format("Не удалось найти вид для отображения {0}", modelType));
					else
						log.ErrorFormat("Не удалось найти вид для отображения {0}", modelType);
				}

				return viewType == null
							? new TextBlock()
							: ViewLocator.GetOrCreateViewType(viewType);
			};

			Conventions.Register();
			SaneCheckboxEditor.Register();
			NotifyValueSupport.Register();

			var customPropertyBinders = new Action<IEnumerable<FrameworkElement>, Type>[] {
				EnabledBinder.Bind,
				VisibilityBinder.Bind,
			};
			var customBinders = new Action<Type, IEnumerable<FrameworkElement>, List<FrameworkElement>>[] {
				//сначала должен обрабатываться поиск и только потом переход
				SearchBinder.Bind,
				NavBinder.Bind,
				EnterBinder.Bind,
			};

			var defaultBindProperties = ViewModelBinder.BindProperties;
			var defaultBindActions = ViewModelBinder.BindActions;
			var defaultBind = ViewModelBinder.Bind;
			ViewModelBinder.Bind = (viewModel, view, context) => {
				if ((bool)view.GetValue(ViewModelBinder.ConventionsAppliedProperty)) {
					return;
				}

				defaultBind(viewModel, view, context);
				ContentElementBinder.Bind(viewModel, view, context);
				Commands.Bind(viewModel, view, context);
				var baseScreen = viewModel as BaseScreen;
				//этот метод будет вызваться каждый раз при активации\деактивации формы
				//что бы избежать множественных подписок
				if (baseScreen != null && !baseScreen.ResultsSink.HasObservers) {
					baseScreen.ResultsSink
						.CatchSubscribe(r => Coroutine.BeginExecute(new List<IResult> { r }.GetEnumerator(), new ActionExecutionContext {
							View = view
						}), baseScreen.CloseCancellation);
				}
				var baseShell = viewModel as BaseShell;
				if (baseShell != null && !baseShell.ResultsSink.HasObservers) {
					baseShell.ResultsSink
						.CatchSubscribe(r => Coroutine.BeginExecute(new List<IResult> { r }.GetEnumerator(), new ActionExecutionContext {
							View = view
						}), baseShell.CancelDisposable);
				}
			};

			ViewModelBinder.BindProperties = (elements, type) => {
				foreach (var binder in customPropertyBinders) {
					binder(elements, type);
				}
				return defaultBindProperties(elements, type);
			};

			ViewModelBinder.BindActions = (elements, type) => {
				var binded = defaultBindActions(elements, type).ToList();

				foreach (var binder in customBinders) {
					binder(type, elements, binded);
				}
				return elements;
			};
		}
	}
}