using System;
using System.Linq;
using System.Windows.Documents;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class PriceResultFixture
	{
		[Test]
		public void Do_build_documents_only_once()
		{
			var result = new PrintResult("тест", Enumerable.Range(0, 1).Select(r => new DefaultDocument(new FlowDocument())));
			var paginator1 = result.Paginator;
			var paginator2 = result.Paginator;
			Assert.IsTrue(ReferenceEquals(paginator1.Source, paginator2.Source));
		}
	}
}