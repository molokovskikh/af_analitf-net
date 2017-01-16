using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using NUnit.Framework;
using AnalitF.Net.Client.Models;
using Common.NHibernate;

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
			session.DeleteEach<Order>();
			var order = MakeOrder();
			order.Frozen = true;
			session.Flush();

			var line = order.Lines[0];
			var offer = session.Load<Offer>(line.OfferId);
			var productId = line.ProductId;
			// второй заказ того же самого товара, незамороженный
			var order2 = MakeOrder(offer);
			var productId2 = order2.Lines[0].ProductId;
			Assert.IsTrue(productId == productId2);

			// детализация текущего заказа
			var productInFrozenOrders = order.Lines.Select(x => x.ProductId).ToList();
			var model = new OrderDetailsViewModel(order2, productInFrozenOrders);
			var view = Bind(model);
			scheduler.Start();
			var grid = view.Descendants<DataGrid>().First(g => g.Name == "Lines");
			var item = grid.Items.Cast<OrderLine>().First(x => x.ProductId == productId);
			Assert.IsTrue(item.InFrozenOrders);

			// сводный заказ
			var model2 = new OrderLinesViewModel();
			var view2 = Bind(model2);
			var grid2 = view2.Descendants<DataGrid>().First(g => g.Name == "Lines");
			var item2 = grid2.Items.Cast<OrderLine>().First(x => x.ProductId == productId);
			Assert.IsTrue(item2.InFrozenOrders);
		}

		[Test]
		public void Line_is_MinCost()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder();
			var line = order.Lines[0];
			line.OptimalFactor = 0;
			session.Flush();
			var productId = line.ProductId;

			// детализация текущего заказа
			var model = new OrderDetailsViewModel(order);
			var view = Bind(model);
			scheduler.Start();
			var grid = view.Descendants<DataGrid>().First(g => g.Name == "Lines");
			var item = grid.Items.Cast<OrderLine>().First(x => x.ProductId == productId);
			Assert.IsTrue(item.IsMinCost);

			// сводный заказ
			var model2 = new OrderLinesViewModel();
			var view2 = Bind(model2);
			var grid2 = view2.Descendants<DataGrid>().First(g => g.Name == "Lines");
			var item2 = grid2.Items.Cast<OrderLine>().First(x => x.ProductId == productId);
			Assert.IsTrue(item2.IsMinCost);
		}
	}
}