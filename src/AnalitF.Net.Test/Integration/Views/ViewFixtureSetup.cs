using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using AnalitF.Net.Client;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	public class TestTraceListner : TraceListener
	{
		private List<string> traces;

		public TestTraceListner(List<string> traces)
		{
			this.traces = traces;
		}

		public override void Write(string message)
		{
			traces.Add(message);
		}

		public override void WriteLine(string message)
		{
			traces.Add(message);
		}
	}

	public class ViewFixtureSetup
	{
		private static bool init;

		public static List<string> BindingErrors = new List<string>();

		//nunit не умеет вызывать инициализатор в sta
		//и игнорируте атрибут RequiresSTA по этому надо вызывать руками
		public static void Setup()
		{
			if (init)
				return;

			init = true;
			PresentationTraceSources.Refresh();
			PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
			PresentationTraceSources.DataBindingSource.Listeners.Add(new TestTraceListner(BindingErrors));

			var app = new Client.App();
			AssemblySource.Instance.Add(typeof(App).Assembly);
			System.Windows.Application.LoadComponent(app, new Uri("/AnalitF.Net.Client;component/app.xaml", UriKind.Relative));
			app.RegisterResources();
			//при инициализации caliburn будет думать что у нас есть dispatcher
			//и пытатся выболнять все уведомления в ui thread
			//на самом деле у нас dispatcher`а нет
			Execute.ResetWithoutDispatcher();
		}
	}
}