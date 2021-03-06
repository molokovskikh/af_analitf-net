﻿using System;
using System.Linq;
using System.Windows.Input;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class CatalogFixture : BaseViewFixture
	{
		[Test]
		public void Move_focus()
		{
			WpfTestHelper.WithWindow2(async w => {
				var model = new CatalogViewModel();
				var view = Bind(model);
				w.Content = view;

				await view.WaitLoaded();
				scheduler.Start();
				await view.WaitIdle();
				var names = view.Descendants<DataGrid2>().First(x => x.Name == "CatalogNames");
				var forms = view.Descendants<DataGrid2>().First(x => x.Name == "Catalogs");
				names.Focus();
				Assert.IsTrue(names.IsKeyboardFocusWithin);
				Assert.IsFalse(forms.IsKeyboardFocusWithin);
				var id = session.Query<CatalogName>()
					.First(c => c.HaveOffers && session.Query<Catalog>().Count(x => x.HaveOffers && x.Name == c) > 1)
					.Id;
				names.SelectedItem = names.Items.OfType<CatalogName>().First(x => x.Id == id);
				scheduler.Start();
				names.RaiseEvent(WpfTestHelper.KeyArgs(names, Key.Enter));
				await view.WaitIdle();
				Assert.IsFalse(names.IsKeyboardFocusWithin);
				Assert.IsTrue(forms.IsKeyboardFocusWithin);
			});
		}
	}
}