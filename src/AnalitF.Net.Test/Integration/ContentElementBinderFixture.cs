using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture]
	public class ContentElementBinderFixture
	{
		[SetUp]
		public void Setup()
		{
			AppBootstrapper.RegisterBinder();
		}

		[Test, RequiresSTA]
		public void Bind_content_elements()
		{
			var model = new ViewModel { Text = "123" };
			var view = new UserControl { DataContext = model };
			var text = new TextBlock();
			var item = new Run { Name = "Text" };
			text.Inlines.Add(item);
			view.Content = text;

			Assert.That(view.DeepChildren().Count(), Is.GreaterThan(0));
			ViewModelBinder.Bind(model, view, null);
			Assert.That(item.Text, Is.EqualTo("123"));
		}

		[Test, RequiresSTA]
		public void Enabled_binder()
		{
			var model = new ViewModel { Text = "123" };
			var view = new UserControl { DataContext = model };
			var checkBox = new CheckBox { Name = "Is" };
			view.Content = checkBox;

			Assert.That(view.DeepChildren().Count(), Is.GreaterThan(0));
			ViewModelBinder.Bind(model, view, null);
			Assert.That(checkBox.IsEnabled, Is.False);
		}

		public class ViewModel
		{
			public string Text { get; set; }

			public bool Is { get; set; }

			public bool IsEnabled { get; set; }
		}
	}
}