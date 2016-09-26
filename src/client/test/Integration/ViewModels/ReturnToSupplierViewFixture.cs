using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Results;
using System.IO;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using System.Collections.Generic;
using System;
using Common.NHibernate;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class ReturnToSupplierViewFixture : ViewModelFixture
	{
		private ReturnToSupplier returnToSupplier;

		private ReturnToSuppliers model;

		private ReturnToSupplierDetails modelDetails;

		[SetUp]
		public void Setup()
		{
			returnToSupplier = CreateReturnToSupplier();
			model = Open(new ReturnToSuppliers());
			modelDetails = Open(new ReturnToSupplierDetails(returnToSupplier.Id));
		}

		[Test]
		public void Export_returnToSupplier()
		{
			var result = (OpenResult)model.ExportExcel();
			Assert.IsTrue(File.Exists(result.Filename));
		}


		[Test]
		public void Export_ReturnToSupplierDetails_details()
		{
			var result = (OpenResult)modelDetails.ExportExcel();
			Assert.IsTrue(File.Exists(result.Filename));
		}

		[Test]
		public void Print_ReturnToSupplierDetails()
		{
			var results = modelDetails.Print().GetEnumerator();
			var preview = Next<DialogResult>(results);
			Assert.IsInstanceOf<PrintPreviewViewModel>(preview.Model);
		}

		private ReturnToSupplier CreateReturnToSupplier()
		{
			session.DeleteEach<ReturnToSupplier>();
			var department = session.Query<Address>().First();
			var supplier = session.Query<Supplier>().First();
			var _returnToSupplier = new ReturnToSupplier
			{
				Date = DateTime.Now,
				Supplier = supplier,
				Address = department
			};
			var returnToSupplierLine = CreateReturnToSupplierLine(_returnToSupplier);
			session.Save(_returnToSupplier);
			session.Save(returnToSupplierLine);
			_returnToSupplier.Lines.Add(returnToSupplierLine);
			return _returnToSupplier;
		}

		private ReturnToSupplierLine CreateReturnToSupplierLine(ReturnToSupplier returnToSupplier)
		{
			return new ReturnToSupplierLine(returnToSupplier.Id)
			{
				ProductId = 10,
				Product = "Тестовый продукт",
				Quantity = 1,
			};
		}
	}
}
