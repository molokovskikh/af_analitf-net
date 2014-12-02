using System;
using System.Linq;
using AnalitF.Net.Client.Controls;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class SearchableDataGridColumnFixture
	{
		[Test]
		public void Split()
		{
			var result = SearchableDataGridColumn.Split("АТЕНОЛОЛ табл. 50 мг N100", new [] { "бальзам" });
			Assert.IsEmpty(result);

			var value = "АСКОРБИНОВАЯ КИСЛОТА табл. с сахаром 25 мг N10";
			result = SearchableDataGridColumn.Split(value,
				"аско кислот".Split(' '));
			var humanResult = String.Join("", result.Select(t => {
				var part = value.Substring(t.Item1, t.Item2);
				if (t.Item3)
					part = "<" + part + ">";
				return part;
			}));
			Assert.AreEqual("<АСКО>РБИНОВАЯ <КИСЛОТ>А табл. с сахаром 25 мг N10", humanResult);

			value = "БАЛЬЗАМ КОСМЕТИЧЕСКИЙ Венозус для ног 75мл";
			result = SearchableDataGridColumn.Split(value,
				"бальзам зам".Split(' '));
			humanResult = String.Join("", result.Select(t => {
				var part = value.Substring(t.Item1, t.Item2);
				if (t.Item3)
					part = "<" + part + ">";
				return part;
			}));
			Assert.AreEqual("<БАЛЬЗАМ> КОСМЕТИЧЕСКИЙ Венозус для ног 75мл", humanResult);
		}
	}
}