using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows.Documents;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Common.Tools;
using NUnit.Framework;
using ReactiveUI.Testing;
using CreateWaybill = AnalitF.Net.Client.Test.Fixtures.CreateWaybill;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels.Inventory;
using System.Collections.Generic;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class TagsFixture : ViewModelFixture
	{
		private Tags model;

		[SetUp]
		public void Setup()
		{
			var tags = new List<TagPrintable>() {
				new TagPrintable{Product = "Диклофенак", Quantity = 1}
			};
			autoStartScheduler = false;
			model = Open(new Tags(null, tags));
		}

		[Test]
		public void Print_racking_map()
		{
			var result = (DialogResult) model.PrintRackingMaps();
			var preview = (PrintPreviewViewModel) result.Model;
			Assert.IsNotNull(preview);
		}

		[Test]
		public void Print_price_tags()
		{
			var result = (DialogResult) model.PrintPriceTags();
			var preview = (PrintPreviewViewModel) result.Model;
			Assert.IsNotNull(preview);
		}

		[Test]
		public void Print_barcode()
		{
			var result = (DialogResult)model.PrintBarcode();
			var preview = (PrintPreviewViewModel)result.Model;
			Assert.IsNotNull(preview);
		}
	}
}