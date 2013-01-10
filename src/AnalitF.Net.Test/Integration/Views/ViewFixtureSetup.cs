using System;
using System.CodeDom.Compiler;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	public class ViewFixtureSetup
	{
		public static bool init;

		//nunit не умеет вызывать инициализатор в sta
		//и игнорируте атрибут RequiresSTA по этому надо вызывать руками
		public static void Setup()
		{
			if (init)
				return;
			init = true;
			var app = new Client.App();
			System.Windows.Application.LoadComponent(app, new Uri("/AnalitF.Net.Client;component/app.xaml", UriKind.Relative));
			app.RegisterResources();
			//при инициализации caliburn будет думать что у нас есть dispatcher
			//и пытатся выболнять все уведомления в ui thread
			//на самом деле у нас dispatcher`а нет
			Execute.ResetWithoutDispatcher();
		}
	}
}