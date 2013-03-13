using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using AnalitF.Net.Client;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Extentions;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Test.Integration.Views;
using Caliburn.Micro;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration
{
	[TestFixture, RequiresSTA]
	public class BinderFixture
	{
		private UserControl view;
		private ViewModel model;

		[SetUp]
		public void Setup()
		{
			model = new ViewModel();
			view = new UserControl { DataContext = model };
		}

		[Test]
		public void Bind_content_elements()
		{
			model.Text = "123";
			var text = new TextBlock();
			var item = new Run { Name = "Text" };
			text.Inlines.Add(item);
			view.Content = text;

			Assert.That(view.DeepChildren().Count(), Is.GreaterThan(0));
			ViewModelBinder.Bind(model, view, null);
			Assert.That(item.Text, Is.EqualTo("123"));
		}

		[Test]
		public void Select_children()
		{
			view.Content = new TabControl {
				Items = {
					new TabItem { Content = new DataGrid() },
					new TabItem { Content = new DataGrid() },
				}
			};
			var count = view.DeepChildren().OfType<DataGrid>().Count();
			Assert.That(count, Is.EqualTo(2));
		}

		[Test]
		public void Enabled_binder()
		{
			model.Text = "123";
			var checkBox = new CheckBox { Name = "Is" };
			view.Content = checkBox;

			Assert.That(view.DeepChildren().Count(), Is.GreaterThan(0));
			ViewModelBinder.Bind(model, view, null);
			Assert.That(checkBox.IsEnabled, Is.False);
		}

		[Test]
		public void Visible_binder()
		{
			model.Text = "123";
			var checkBox = new CheckBox { Name = "Is" };
			view.Content = checkBox;

			Assert.That(view.DeepChildren().Count(), Is.GreaterThan(0));
			ViewModelBinder.Bind(model, view, null);
			Assert.That(checkBox.Visibility, Is.EqualTo(Visibility.Collapsed));
		}

		[Test]
		public void Bind_password()
		{
			model.Text = "123";
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
			model.Text = "123";
			ViewModelBinder.Bind(model, view, null);

			Assert.That(view.CommandBindings.Count, Is.EqualTo(2));
			Assert.That(view.CommandBindings[0].Command, Is.EqualTo(Commands.InvokeViewModel));
		}

		[Test]
		public void Bind_notify_value()
		{
			model.Term = new NotifyValue<string>("123");
			var textBox = new TextBox { Name = "Term" };
			view.Content = textBox;
			ViewModelBinder.Bind(model, view, null);

			Assert.That(textBox.Text, Is.EqualTo("123"));
		}

		[Test]
		public void Invoke_view_model_with_parameters()
		{
			ViewModelHelper.InvokeDataContext(view, new { Method = "Reset", i = 1 });
			Assert.That(model.ResetValue, Is.EqualTo(1));
		}

		[Test]
		public void Invoke_view_model()
		{
			ViewModelHelper.InvokeDataContext(view, "Reset", EventArgs.Empty);
			Assert.That(model.ResetValue, Is.EqualTo(100));
		}

		public class ViewModel
		{
			public int ResetValue;

			public string Text { get; set; }

			public NotifyValue<string> Term { get; set; }

			public bool Is { get; set; }

			public bool IsEnabled { get; set; }

			public bool IsVisible { get; set; }

			public void Reset()
			{
				ResetValue = 100;
			}

			public void Reset(int i)
			{
				ResetValue = i;
			}
		}
	}
}