using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.ViewModels;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class QuickSearchFixture
	{
		private QuickSearch<string> search;
		private string result;
		private List<string> items;

		[SetUp]
		public void Setup()
		{
			QuickSearch<string>.TestScheduler = new TestScheduler();

			items = new List<string>();
			result = null;
			search = new QuickSearch<string>(v => items.FirstOrDefault(i => i.Contains(v)), s => result = s);
			items.Add("123");
		}

		[Test]
		public void Disable_search()
		{
			search.IsEnabled = false;
			search.SearchText = "1";
			Assert.That(result, Is.Null);
			Assert.That(search.SearchText, Is.Null);
		}

		[Test]
		public void Disable_on_search()
		{
			search.SearchText = "1";
			Assert.That(result, Is.Not.Null);
			search.IsEnabled = false;
			Assert.That(search.SearchText, Is.Null);
		}
	}
}