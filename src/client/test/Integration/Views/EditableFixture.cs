using System;
using System.Collections.Generic;
using System.Windows.Input;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Test.TestHelpers;
using Caliburn.Micro;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI.Testing;
using View = System.Web.UI.WebControls.View;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture]
	public class EditableFixture
	{
		public class ViewModel
		{
			public bool commited;
			public bool updated;

			public void OfferCommitted()
			{
				commited = true;
			}

			public void OfferUpdated()
			{
				updated = true;
			}
		}

		public class Item : IInlineEditable
		{
			public uint Value { get; set; }
		}

		[Test]
		public void Commit_edit()
		{
			var scheduler = new TestScheduler();
			var viewModel = new ViewModel();
			var items = new List<Item> {
				new Item()
			};

			var grid = new DataGrid2();
			grid.DataContext = viewModel;
			grid.ItemsSource = items;
			grid.SelectedItem = items[0];
			new Editable(scheduler).Attach(grid);
			grid.RaiseEvent(WpfTestHelper.TextArgs("1"));

			Assert.IsTrue(viewModel.updated);
			Assert.IsFalse(viewModel.commited);

			scheduler.AdvanceByMs(2000);
			Assert.IsTrue(viewModel.commited);
			Assert.AreEqual(1, items[0].Value);
		}
	}
}