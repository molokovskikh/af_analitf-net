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
			var productId = order.Lines[0].ProductId;

			// детализация текущего заказа
			var productInFrozenOrders = order.Lines.Select(x => x.ProductId).ToList();
			var model = new OrderDetailsViewModel(order, productInFrozenOrders);
			var view = Bind(model);
			scheduler.Start();
			var grid = view.Descendants<DataGrid>().First(g => g.Name == "Lines");
			var item = grid.Items.Cast<OrderLine>().First(x => x.ProductId == productId);
			Assert.IsTrue(item.InFrozenOrders);

			// сводный заказ
			var model2 = new OrderLinesViewModel();
			var view2 = Bind(model2);
			var grid2 = view2.Descendants<DataGrid>().First(g => g.Name == "Lines");
			var item2 = grid.Items.Cast<OrderLine>().First(x => x.ProductId == productId);
			Assert.IsTrue(item2.InFrozenOrders);
		}
	}
}