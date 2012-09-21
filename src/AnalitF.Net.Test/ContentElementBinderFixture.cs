using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test
{
	[TestFixture]
	public class ContentElementBinderFixture
	{
		[Test, RequiresSTA]
		public void Bind_content_elements()
		{
			var view = new UserControl();
			var text = new TextBlock();
			var item = new Run { Name = "Text" };
			text.Inlines.Add(item);
			view.Content = text;

			Assert.That(XamlExtentions.DeepChildren(view).Count(), Is.GreaterThan(0));
			ContentElementBinder.Bind(new ViewModel { Text = "123" }, view, null);
			Assert.That(item.Text, Is.EqualTo("123"));
		}

		public class ViewModel
		{
			public string Text { get; set; }
		}
	}
}