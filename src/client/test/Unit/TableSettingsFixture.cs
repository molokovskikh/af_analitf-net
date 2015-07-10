using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Interactivity;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class TableSettingsFixture
	{
		private TableSettings settings;
		private DataGrid grid;
		private ContentControl content;

		[SetUp]
		public void Setup()
		{
			settings = new TableSettings();
			content = new ContentControl();
			InitGrid();
		}

		[Test]
		public void Assign_default_index_for_new_column()
		{
			grid.Columns.Add(new DataGridTextColumn { Header = "Название", DisplayIndex = 0 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель", DisplayIndex = 1 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Цена", DisplayIndex = 2 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Сумма", DisplayIndex = 3 });
			settings.SaveView(content);

			grid.Columns.Add(new DataGridTextColumn { Header = "Название" });
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель" });
			grid.Columns.Add(new DataGridTextColumn { Header = "Цена поставщика" });
			grid.Columns.Add(new DataGridTextColumn { Header = "Цена" });
			grid.Columns.Add(new DataGridTextColumn { Header = "Сумма" });
			settings.RestoreView(content);
			Assert.AreEqual(grid.Columns[0].DisplayIndex, 0);
			Assert.AreEqual(grid.Columns[2].DisplayIndex, 2);
		}

		[Test]
		public void Ignore_removed_column()
		{
			grid.Columns.Add(new DataGridTextColumn { Header = "Название", DisplayIndex = 0 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель", DisplayIndex = 1 });
			settings.SaveView(content);

			InitGrid();
			grid.Columns.Add(new DataGridTextColumn { Header = "Название" });
			settings.RestoreView(content);
			Assert.AreEqual(1, grid.Columns.Count);
		}

		[Test]
		public void Remove_column()
		{
			grid.Columns.Add(new DataGridTextColumn { Header = "Название", DisplayIndex = 0 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель", DisplayIndex = 1 });
			settings.SaveView(content);

			InitGrid();
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель" });
			content.Content = grid;
			settings.RestoreView(content);
			Assert.AreEqual(1, grid.Columns.Count);
		}

		[Test]
		public void Persis_sort_order()
		{
			grid.Columns.Add(new DataGridTextColumn {
				Header = "Наименование",
				SortDirection = ListSortDirection.Descending,
				SortMemberPath = "Name"
			});
			grid.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Descending));
			settings.SaveView(content);

			InitGrid();
			grid.Columns.Add(new DataGridTextColumn {
				Header = "Наименование",
				SortMemberPath = "Name"
			});
			settings.RestoreView(content);
			var sortDescription = grid.Items.SortDescriptions[0];
			Assert.AreEqual("Name", sortDescription.PropertyName);
			Assert.AreEqual(ListSortDirection.Descending, sortDescription.Direction);
			Assert.AreEqual("Name", grid.Columns[0].SortMemberPath);
			Assert.AreEqual(ListSortDirection.Descending, grid.Columns[0].SortDirection);
		}

		[Test]
		public void Reset_sort_direction_if_sort_direction_defined()
		{
			grid.Columns.Add(new DataGridTextColumn {
				Header = "Наименование",
				SortDirection = ListSortDirection.Descending,
				SortMemberPath = "Name",
			});
			grid.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Descending));
			grid.Columns.Add(new DataGridTextColumn {
				Header = "Срок годности",
				SortMemberPath = "Exp",
			});

			grid.Items.SortDescriptions.Clear();
			grid.Items.SortDescriptions.Add(new SortDescription("Exp", ListSortDirection.Descending));
			grid.Columns[0].SortDirection = null;
			grid.Columns[1].SortDirection = ListSortDirection.Descending;
			settings.SaveView(content);

			InitGrid();
			grid.Columns.Add(new DataGridTextColumn {
				Header = "Наименование",
				SortDirection = ListSortDirection.Descending,
				SortMemberPath = "Name"
			});
			grid.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Descending));
			grid.Columns.Add(new DataGridTextColumn {
				Header = "Срок годности",
				SortMemberPath = "Exp",
			});
			settings.RestoreView(content);

			Assert.AreEqual(1, grid.Items.SortDescriptions.Count);
			var sortDescription = grid.Items.SortDescriptions[0];
			Assert.AreEqual("Exp", sortDescription.PropertyName);
			Assert.AreEqual(ListSortDirection.Descending, sortDescription.Direction);
			var column = grid.Columns[1];
			Assert.AreEqual("Exp", column.SortMemberPath);
			Assert.AreEqual(ListSortDirection.Descending, column.SortDirection);
			column = grid.Columns[0];
			Assert.IsNull(column.SortDirection);
		}

		[Test]
		public void Restore_display_index()
		{
			grid.Columns.Add(new DataGridTextColumn { Header = "Наименование", DisplayIndex = 0 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Остаток", DisplayIndex = 1 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Заказ", DisplayIndex = 2 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Сумма", DisplayIndex = 3 });

			grid.Columns[1].DisplayIndex = 2;
			//ui может захотеть удалить колонку тк пользователю нельзя видить ее
			//восстановление настроек должно корректно обрабатывать эту ситуацию
			grid.Columns.Remove(grid.Columns[0]);
			settings.SaveView(content);

			InitGrid();
			grid.Columns.Add(new DataGridTextColumn { Header = "Наименование", DisplayIndex = 0 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Остаток", DisplayIndex = 1 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Заказ", DisplayIndex = 2 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Сумма", DisplayIndex = 3 });
			settings.RestoreView(content);
			Assert.AreEqual(0, grid.Columns[0].DisplayIndex);
			Assert.AreEqual(2, grid.Columns[1].DisplayIndex);
			Assert.AreEqual(1, grid.Columns[2].DisplayIndex);
			Assert.AreEqual(3, grid.Columns[3].DisplayIndex);
		}


		private void InitGrid()
		{
			grid = new DataGrid();
			Interaction.GetBehaviors(grid).Add(new Persistable());
			content.Content = grid;
		}
	}
}