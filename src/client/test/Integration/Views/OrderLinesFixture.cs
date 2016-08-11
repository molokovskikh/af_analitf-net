using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using NUnit.Framework;
using AnalitF.Net.Client.Models;

namespace AnalitF.Net.Client.Test.Integration.Views
{
	[TestFixture]
	public class OrderLinesFixture : BaseViewFixture
	{
		[Test]
		public void Bind_address_selector()
		{
			var view = Bind(new OrderLinesViewModel());

			var all = view.Descendants<Control>().First(e => e.Name == "All");
			Assert.That(all.Visibility, Is.EqualTo(Visibility.Collapsed));
		}

		[Test]
		public void Build_comment()
		{
			var order = MakeOrder();
			order.Comment = "text order comment";
			order.Lines[0].Comment = "test line comment";
			session.Flush();
			var model = new OrderDetailsViewModel(order);
			UseWindow(model, async (w, view) => {
				model.CurrentLine.Value = model.Lines.Value.First();
				var lineComment = view.Descendants<TextBox>().First(c => c.Name == "CurrentLine_Comment");
				Assert.AreEqual("test line comment", lineComment.Text);
				Assert.IsTrue(lineComment.IsEnabled);
				var orderComment = view.Descendants<TextBox>().First(c => c.Name == "Order_Comment");
				Assert.AreEqual("text order comment", orderComment.Text);
				Assert.IsTrue(orderComment.IsEnabled);
			});
		}

		[Test]
		public void Line_in_Frozen_Orders()
		{
			var order = MakeOrder();
			order.Frozen = true;
			session.Flush();

			// детализация текущего заказа
			var model = new OrderDetailsViewModel(order);
			var view = Bind(model);
			var grid = view.Descendants<DataGrid>().First(g => g.Name == "Lines");
			Assert.IsTrue(((OrderLine)grid.Items.CurrentItem).InFrozenOrders);

			// сводный заказ
			var model2 = new OrderLinesViewModel();
			var view2 = Bind(model2);
			var grid2 = view2.Descendants<DataGrid>().First(g => g.Name == "Lines");
			Assert.IsTrue(((OrderLine)grid.Items.CurrentItem).InFrozenOrders);
		}
	}
}