using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.UI;
using AnalitF.Net.Client.ViewModels;
using Caliburn.Micro;
using Inflector;
using NHibernate.Mapping;
using ReactiveUI;

namespace AnalitF.Net.Client.Config.Initializers
{
	public class Caliburn
	{
		public void Init()
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
				defaultBind(viewModel, view, context);
				ContentElementBinder.Bind(viewModel, view, context);
				Commands.Bind(viewModel, view, context);
				var baseScreen = viewModel as BaseScreen;
				if (baseScreen != null) {
					baseScreen.ResultsSink
						.CatchSubscribe(r => Coroutine.BeginExecute(new List<IResult> { r }.GetEnumerator()),
							baseScreen.CloseCancellation);
				}
				var baseShell = viewModel as BaseShell;
				if (baseShell != null) {
					baseShell.ResultsSink
						.CatchSubscribe(r => Coroutine.BeginExecute(new List<IResult> { r }.GetEnumerator()),
							baseShell.CancelDisposable);
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