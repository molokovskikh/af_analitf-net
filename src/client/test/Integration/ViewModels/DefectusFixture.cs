using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Inventory;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class DefectusFixture : ViewModelFixture<Defectus>
	{
		[SetUp]
		public void Setup()
		{
			shell.Navigate(model);
			model.Address = address;
		}

		[Test]
		public void Add()
		{
			//Arrange
			var product = session.Query<Product>().First();
			var addSeq = model.Add().GetEnumerator();
			var defectusLine = new DefectusLine() {
				Product = product.Name,
				ProductId = product.Id,
				CatalogId = product.CatalogId,
			};

			//Act
			addSeq.MoveNext();
			var addDefectusLine = ((AddDefectusLine)((DialogResult)addSeq.Current).Model);
			addDefectusLine.Item = defectusLine;
			addDefectusLine.OK();
			addSeq.MoveNext();
			var resultLine = model.Items.Value[0];

			//Assert
			Assert.AreEqual(product.Id, resultLine.ProductId);
		}
	}
}
