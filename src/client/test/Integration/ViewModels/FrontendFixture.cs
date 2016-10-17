using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using Common.NHibernate;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class FrontendFixture : ViewModelFixture
	{
		private Frontend model;

		private Stock stock;

		[SetUp]
		public void Setup()
		{
			model = Open(new Frontend());
			stock = new Stock()
			{
				Product = "Папаверин",
				Status = StockStatus.Available,
				Address = address,
				RetailCost = 1,
				Quantity = 5,
				ReservedQuantity = 0
			};
			stateless.Insert(stock);

			session.DeleteEach<Check>();
			session.Flush();
		}

		[Test]
		public void Doc_Close_SaleBuyer()
		{
			//На складе есть Папаверин в количестве 5шт.
			Assert.AreEqual(stock.Quantity, 5);

			//добавляем строку на 3 упаковки
			var line = new CheckLine(stock, 3, CheckType.SaleBuyer);
			model.Lines.Add(line);
			Assert.AreEqual(stock.Quantity, 2);

			// Оплата по чеку
			var result = model.Close().GetEnumerator();
			result.MoveNext();
			var dialog = ((Checkout)((DialogResult)result.Current).Model);
			dialog.Amount.Value = 10;
			result.MoveNext();

			// после оплаты на складе остается 2
			var check = session.Query<Check>().First();
			Assert.AreEqual(stock.Quantity, 2);
			Assert.AreEqual(check.Sum, 3);
			Assert.AreEqual(check.CheckType, CheckType.SaleBuyer);
		}

		[Test]
		public void Doc_Close_CheckReturn()
		{
			//На складе есть Папаверин в количестве 5шт.
			Assert.AreEqual(stock.Quantity, 5);
			model.Trigger();

			//добавляем строку на 3 упаковки
			var line = new CheckLine(stock, 3, CheckType.CheckReturn);
			model.Lines.Add(line);
			Assert.AreEqual(stock.Quantity, 8);

			// Возврат по чеку
			var result = model.Close().GetEnumerator();
			result.MoveNext();

			// после возврата на складе остается 8
			var check = session.Query<Check>().First();
			Assert.AreEqual(stock.Quantity, 8);
			Assert.AreEqual(check.Sum, 3);
			Assert.AreEqual(check.CheckType, CheckType.CheckReturn);
		}
	}
}
