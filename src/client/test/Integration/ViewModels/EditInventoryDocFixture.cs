using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.ViewModels.Offers;
using Common.NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	/// <summary>
	/// Класс для проверки модели <see cref="EditInventoryDoc"/>.
	/// </summary>
	[TestFixture]
	public class EditInventoryDocFixture : ViewModelFixture<InventoryDocs>
	{
		/// <summary>
		/// Объект проверяемой модели.
		/// </summary>
		private EditInventoryDoc editModel;

		/// <summary>
		/// Подготавливает модель к проверке.
		/// </summary>
		[SetUp]
		public void Setup()
		{
			//Переходим к модели редактирования излишков.
			shell.Navigate(model);
			model.Address = address;
			var seq = model.Create().GetEnumerator();
			seq.MoveNext();
			seq.MoveNext();
			editModel = shell.ActiveItem as EditInventoryDoc;
		}

		/// <summary>
		/// Проверка добавления излишков со склада
		/// </summary>
		[Test]
		public void Add_AddStock_StockInDoc()
		{
			//Arrange
			var stock = new Stock()
			{
				Product = "Папаверин",
				Status = StockStatus.Available,
				Address = address,
				Quantity = 5,
				ReservedQuantity = 0,
				SupplyQuantity = 5
			};
			session.Save(stock);
			stock = session.Query<Stock>().First();
			var addSeq = editModel.Add().GetEnumerator();

			//Act
			addSeq.MoveNext();
			var stockSearch = (StockSearch) ((DialogResult) addSeq.Current).Model;
			stockSearch.CurrentItem = new NotifyValue<Stock>(stock);
			addSeq.MoveNext();
			var editStock = (EditStock) ((DialogResult) addSeq.Current).Model;
			editStock.OK();
			addSeq.MoveNext();
			var addedDoc = editModel.Doc;
			var resultStock = addedDoc.Lines[0].Stock;

			//Assert
			Assert.AreEqual(stock.Id, resultStock.Id);
		}

		/// <summary>
		/// Проверка добавления излишков из каталога
		/// </summary>
		[Test]
		public void AddFromCatalog_AddStock_StockInDoc()
		{
			//Arrange
			var catalog = session.Query<Catalog>().First();
			var addSeq = editModel.AddFromCatalog().GetEnumerator();
			var stock = new Stock()
			{
				Product = catalog.FullName,
				Status = StockStatus.Available,
				Address = address,
				Quantity = 5,
				ReservedQuantity = 0,
				SupplyQuantity = 5
			};

			//Act
			addSeq.MoveNext();
			var addStockFromCatalog = ((AddStockFromCatalog)((DialogResult)addSeq.Current).Model);
			addStockFromCatalog.Item = stock;
			addStockFromCatalog.OK();
			addSeq.MoveNext();
			var editStock = (EditStock) ((DialogResult) addSeq.Current).Model;
			editStock.OK();
			addSeq.MoveNext();
			var addedDoc = editModel.Doc;
			var resultStock = addedDoc.Lines[0].Stock;

			//Assert
			Assert.AreEqual(catalog.FullName, resultStock.Product);
		}
	}
}
