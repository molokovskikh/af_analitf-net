using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using Caliburn.Micro;
using NUnit.Framework;
using ReactiveUI;

namespace AnalitF.Net.Client.Test.Integration
{
	[TestFixture, Apartment(ApartmentState.STA)]
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
			var item = new Run { Name = "Text" };
			var notify = new Run { Name = "NotifyItems_Count" };
			model.Text = "123";
			model.NotifyItems.Value = new List<string>();
			var text = new TextBlock {
				Inlines = {
					item,
					notify
				}
			};
			view.Content = text;

			Assert.That(view.Descendants().Count(), Is.GreaterThan(0));
			ViewModelBinder.Bind(model, view, null);
			Assert.That(item.Text, Is.EqualTo("123"));
			Assert.IsNotNull(BindingOperations.GetBinding(notify, Run.TextProperty));
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
			var count = view.Descendants<DataGrid>().Count();
			Assert.That(count, Is.EqualTo(2));
		}

		[Test]
		public void Enabled_binder()
		{
			model.Text = "123";
			var checkBox = new CheckBox { Name = "Is" };
			view.Content = checkBox;

			Assert.That(view.Descendants().Count(), Is.GreaterThan(0));
			ViewModelBinder.Bind(model, view, null);
			Assert.That(checkBox.IsEnabled, Is.False);
		}

		[Test]
		public void Visible_binder()
		{
			model.Text = "123";
			var checkBox = new CheckBox { Name = "Is" };
			view.Content = checkBox;

			Assert.That(view.Descendants().Count(), Is.GreaterThan(0));
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
			Assert.That(view.CommandBindings[0].Command, Is.EqualTo(Client.Config.Caliburn.Commands.InvokeViewModel));
		}

		[Test]
		public void Bind_notify_value()
		{
			model.Term = new NotifyValue<string>("123");
			var textBox = new TextBox { Name = "Term" };
			Bind(textBox);
			Assert.That(textBox.Text, Is.EqualTo("123"));
		}

		[Test]
		public void Bind_multiselector()
		{
			model.Items = new List<string> {"a", "b", "c"};
			var grid = new DataGrid {
				Name = "Items"
			};
			view.Content = grid;
			ViewModelBinder.Bind(model, view, null);

			grid.SelectedItems.Add("a");
			grid.SelectedItems.Add("b");
			Assert.That(model.SelectedItems, Is.EquivalentTo(new[] {"a", "b"}));
		}

		[Test]
		public void Bind_current_item()
		{
			model.Items = new List<string> {"a", "b", "c"};
			var grid = new DataGrid {
				Name = "Items"
			};
			view.Content = grid;
			ViewModelBinder.Bind(model, view, null);
			grid.SelectedItem = "a";
			Assert.That(model.CurrentItem.Value, Is.EqualTo("a"));
		}

		[Test]
		public void Get_parent_for_framework_content_element()
		{
			var block = new TextBlock();
			var item = new Run();
			block.Inlines.Add(item);
			Assert.AreEqual(block, item.Parent());
		}

		[Test]
		public void Bind_enum()
		{
			var box = new ComboBox {
				Name = "Enum"
			};
			Bind(box);

			Assert.AreEqual(2, box.Items.Count);
			box.SelectedItem = box.Items[1];
			Assert.AreEqual(TestEnum.Item2, model.Enum);
		}

		[Test]
		public void Bind_notify_enum()
		{
			var box = new ComboBox {
				Name = "NotifyEnum"
			};
			Bind(box);

			Assert.AreEqual(2, box.Items.Count);
		}

		[Test]
		public void Bind_combobox()
		{
			model.NotifyItems.Value = new List<string> { "1", "2" };
			model.CurrentNotifyItem.Value = "2";
			var box = new ComboBox {
				Name = "NotifyItems"
			};
			Bind(box);

			Assert.IsNotNull(BindingOperations.GetBinding(box, ItemsControl.ItemsSourceProperty));
			Assert.IsNotNull(BindingOperations.GetBinding(box, Selector.SelectedItemProperty));
			Assert.AreEqual(2, box.Items.Count);
			Assert.AreEqual("2", box.SelectedItem);
		}

		[Test]
		public void Bind_array()
		{
			model.ArrayItems = new[] { "1", "2" };
			var box = new ComboBox {
				Name = "ArrayItems"
			};
			Bind(box);

			Assert.AreEqual(2, box.Items.Count);
		}

		[Test]
		public void Set_text_alignment()
		{
			var grid = new DataGrid {
				Name = "ItemItems",
				Columns = {
					new DataGridTextColumnEx {
						Binding = new Binding("I")
					}
				}
			};
			Bind(grid);

			Assert.AreEqual(TextAlignment.Right, ((DataGridTextColumnEx)grid.Columns[0]).TextAlignment);
		}

		[Test]
		public void Set_text_alignment_notify_value()
		{
			var grid = new DataGrid {
				Name = "NotifyItemItems",
				Columns = {
					new DataGridTextColumnEx {
						Binding = new Binding("I")
					}
				}
			};
			Bind(grid);

			Assert.AreEqual(TextAlignment.Right, ((DataGridTextColumnEx)grid.Columns[0]).TextAlignment);
		}

		[Test]
		public void Bind_notify_value_property()
		{
			var label1 = new Label { Name = "NotifyItems_Count" };
			var label2 = new Label { Name = "NotifyItems_Value_Count" };
			Bind(new StackPanel { Children = { label1, label2 } });
			var binding1 = BindingOperations.GetBinding(label1, Label.ContentProperty);
			Assert.IsNotNull(binding1);
			Assert.AreEqual("NotifyItems.Value.Count", binding1.Path.Path);
			var binding2 = BindingOperations.GetBinding(label2, Label.ContentProperty);
			Assert.IsNotNull(binding2);
			Assert.AreEqual("NotifyItems.Value.Count", binding2.Path.Path);
		}

		[Test]
		public void Bind_column_header_tooltip()
		{
			var grid = new DataGrid {
				Name = "ItemItems",
				Columns = {
					new DataGridTextColumnEx {
						Header = "test",
						Binding = new Binding("I")
					}
				}
			};
			Bind(grid);

			var header = grid.Columns[0].Header;
			Assert.IsInstanceOf<TextBlock>(header);
			Assert.AreEqual("test", ((TextBlock)header).ToolTip);
		}

		private void Bind(object content)
		{
			view.Content = content;
			ViewModelBinder.Bind(model, view, null);
		}

		public enum TestEnum
		{
			[System.ComponentModel.Description("Item 1")] Item1,
			[System.ComponentModel.Description("Item 2")] Item2,
		}

		public class Item
		{
			public int I { get; set; }
		}

		public class ViewModel
		{
			public ViewModel()
			{
				NotifyText = new NotifyValue<string>();
				SelectedItems = new ReactiveCollection<string>();
				CurrentItem = new NotifyValue<string>();
				NotifyItems = new NotifyValue<List<string>>();
				CurrentNotifyItem = new NotifyValue<string>();
			}

			public int ResetValue;

			public string Text { get; set; }

			public NotifyValue<string> NotifyText { get; set; }

			public NotifyValue<string> Term { get; set; }

			public bool Is { get; set; }

			public bool IsEnabled { get; set; }

			public bool IsVisible { get; set; }

			public NotifyValue<List<string>> NotifyItems { get; set; }
			public NotifyValue<string> CurrentNotifyItem { get; set; }

			public List<string> Items { get; set; }

			public ReactiveCollection<string> SelectedItems { get; set; }

			public NotifyValue<string> CurrentItem { get; set; }

			public TestEnum Enum { get; set; }

			public NotifyValue<TestEnum> NotifyEnum { get; set; }

			public List<Item> ItemItems { get; set; }
			public NotifyValue<List<Item>> NotifyItemItems { get; set; }

			public string[] ArrayItems { get; set; }

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