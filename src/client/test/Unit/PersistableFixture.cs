using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnalitF.Net.Client.Controls.Behaviors;
using AnalitF.Net.Client.Models;
using NUnit.Framework;
using List = NHibernate.Mapping.List;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture, Apartment(ApartmentState.STA)]
	public class PersistableFixture
	{
		[Test]
		public void Copy_to_clipboard()
		{
			var grid = new DataGrid {
				CanUserAddRows = false,
				Columns = {
					new DataGridTextColumn {
						Binding = new Binding("ProductSynonym"),
						Header = "Наименование",
					},
					new DataGridTextColumn {
						Binding = new Binding("Cost"),
						Header = "Цена"
					}
				}
			};
			grid.ItemsSource = new List<Offer> {
				new Offer(new Price("тест"), 100) {
					ProductSynonym = "Тестовый продукт"
				},
				new Offer(new Price("тест"), 150) {
					ProductSynonym = "Тестовый продукт 2"
				},
			};
			Persistable.CopyToClipboard(grid);
			var content = Clipboard.GetText();
			Assert.AreEqual("Наименование\tЦена\r\nТестовый продукт\t100\r\nТестовый продукт 2\t150\r\n", content);
		}
	}
}