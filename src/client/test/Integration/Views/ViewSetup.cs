using System;
using System.Collections.Generic;
using System.Diagnostics;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	public class ViewSetup
	{
		private static bool init;

		public static List<string> BindingErrors = new List<string>();

		//nunit не умеет вызывать инициализатор в sta
		//и игнорирует атрибут RequiresSTA по этому надо вызывать руками
		public static void Setup()
		{
			if (init)
				return;

			init = true;
			LogManager.GetLog = t => new FailFastLog(t);
			PresentationTraceSources.Refresh();
			PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
			PresentationTraceSources.DataBindingSource.Listeners.Add(new MemoryTraceListner(BindingErrors));

			AssemblySource.Instance.Add(typeof(App).Assembly);
			var app = new App();
			System.Windows.Application.LoadComponent(app, new Uri("/AnalitF.Net.Client;component/app.xaml", UriKind.Relative));
			app.RegisterResources();
			//при инициализации caliburn будет думать что у нас есть dispatcher
			//и пытаться выполнять все уведомления в ui thread
			//на самом деле у нас dispatcher`а нет
			Execute.ResetWithoutDispatcher();
		}
	}
}