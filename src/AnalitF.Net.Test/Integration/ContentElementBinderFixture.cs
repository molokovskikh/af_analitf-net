using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture, RequiresSTA]
	public class ContentElementBinderFixture
	{
		[Test]
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

		[Test]
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

		[Test]
		public void Bind_password()
		{
			var model = new ViewModel { Text = "123" };
			var view = new UserControl { DataContext = model };
			var password = new PasswordBox { Name = "Text" };
			view.Content = password;

			ViewModelBinder.Bind(model, view, null);
			Assert.That(password.Password, Is.EqualTo("123"));
			password.Password = "456";
			Assert.That(model.Text, Is.EqualTo("456"));
		}

		[Test]
		public void Create_command_binding()
		{
			var model = new ViewModel { Text = "123" };
			var view = new UserControl { DataContext = model };
			ViewModelBinder.Bind(model, view, null);

			Assert.That(view.CommandBindings.Count, Is.EqualTo(1));
			Assert.That(view.CommandBindings[0].Command, Is.EqualTo(Commands.InvokeViewModel));
		}

		public class ViewModel
		{
			public string Text { get; set; }

			public bool Is { get; set; }

			public bool IsEnabled { get; set; }
		}
	}
}