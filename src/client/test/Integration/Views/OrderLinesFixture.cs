using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
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
			var view = Bind(model);
			model.CurrentLine.Value = model.Lines.Value.First();
			var lineComment = view.Descendants<TextBox>().First(c => c.Name == "CurrentLine_Comment");
			Assert.AreEqual("test line comment", lineComment.Text);
			Assert.IsTrue(lineComment.IsEnabled);
			var orderComment = view.Descendants<TextBox>().First(c => c.Name == "Order_Comment");
			Assert.AreEqual("text order comment", orderComment.Text);
			Assert.IsTrue(orderComment.IsEnabled);
		}
	}
}