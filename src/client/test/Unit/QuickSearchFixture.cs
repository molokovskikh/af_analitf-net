using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Parts;
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
			var scheduler = new TestScheduler();

			items = new List<string>();
			result = null;
			search = new QuickSearch<string>(scheduler,
				v => items.FirstOrDefault(i => i.IndexOf(v, StringComparison.CurrentCultureIgnoreCase) >= 0),
				s => result = s);
			items.Add("Microsoft");
		}

		[Test]
		public void Search()
		{
			search.SearchText = "m";
			Assert.AreEqual("Microsoft", result);
			Assert.AreEqual("m", search.SearchText);
		}

		[Test]
		public void Disable_search()
		{
			search.IsEnabled = false;
			search.SearchText = "m";
			Assert.That(result, Is.Null);
			Assert.That(search.SearchText, Is.Null);
		}

		[Test]
		public void Disable_on_search()
		{
			search.SearchText = "m";
			Assert.That(result, Is.Not.Null);
			search.IsEnabled = false;
			Assert.That(search.SearchText, Is.Null);
		}

		[Test]
		public void Remap_keyboard_layout()
		{
			search.RemapChars = true;
			items.Add("папаверин");
			search.SearchText = "gfgf";
			Assert.AreEqual("папа", search.SearchText);
			Assert.AreEqual("папаверин", result);
		}
	}
}