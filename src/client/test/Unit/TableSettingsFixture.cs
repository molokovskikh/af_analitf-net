using System;
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

		[SetUp]
		public void Setup()
		{
			settings = new TableSettings();
		}

		[Test]
		public void Assign_default_index_for_new_column()
		{
			var grid = new DataGrid();
			Interaction.GetBehaviors(grid).Add(new Persistable());
			grid.Columns.Add(new DataGridTextColumn { Header = "Название", DisplayIndex = 0 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель", DisplayIndex = 1 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Цена", DisplayIndex = 2 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Сумма", DisplayIndex = 3 });
			var content = new ContentControl();
			content.Content = grid;
			settings.SaveView(content);

			grid = new DataGrid();
			Interaction.GetBehaviors(grid).Add(new Persistable());
			grid.Columns.Add(new DataGridTextColumn { Header = "Название" });
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель" });
			grid.Columns.Add(new DataGridTextColumn { Header = "Цена поставщика" });
			grid.Columns.Add(new DataGridTextColumn { Header = "Цена" });
			grid.Columns.Add(new DataGridTextColumn { Header = "Сумма" });
			content.Content = grid;
			settings.RestoreView(content);
			Assert.AreEqual(grid.Columns[0].DisplayIndex, 0);
			Assert.AreEqual(grid.Columns[2].DisplayIndex, 2);
		}

		[Test]
		public void Ignore_removed_column()
		{
			var grid = new DataGrid();
			Interaction.GetBehaviors(grid).Add(new Persistable());
			grid.Columns.Add(new DataGridTextColumn { Header = "Название", DisplayIndex = 0 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель", DisplayIndex = 1 });
			var content = new ContentControl();
			content.Content = grid;
			settings.SaveView(content);

			grid = new DataGrid();
			Interaction.GetBehaviors(grid).Add(new Persistable());
			grid.Columns.Add(new DataGridTextColumn { Header = "Название" });
			content.Content = grid;
			settings.RestoreView(content);
			Assert.AreEqual(1, grid.Columns.Count);
		}

		[Test]
		public void Remove_column()
		{
			var grid = new DataGrid();
			Interaction.GetBehaviors(grid).Add(new Persistable());
			grid.Columns.Add(new DataGridTextColumn { Header = "Название", DisplayIndex = 0 });
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель", DisplayIndex = 1 });
			var content = new ContentControl();
			content.Content = grid;
			settings.SaveView(content);

			grid = new DataGrid();
			Interaction.GetBehaviors(grid).Add(new Persistable());
			grid.Columns.Add(new DataGridTextColumn { Header = "Производитель" });
			content.Content = grid;
			settings.RestoreView(content);
			Assert.AreEqual(1, grid.Columns.Count);
		}
	}
}