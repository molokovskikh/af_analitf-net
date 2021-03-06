﻿using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class MarkupConfigFixture
	{
		private Address address;

		[SetUp]
		public void Setup()
		{
			address = new Address("Тестовый");
		}

		[Test]
		public void Validate()
		{
			var markups = new[] {
				new MarkupConfig(address, 0, 100, 20),
				new MarkupConfig(address, 80, 200, 20)
			};
			Assert.AreEqual("Некорректно введены границы цен.", MarkupConfig.Validate(markups)[0][1]);
			Assert.That(markups[1].BeginOverlap, Is.True);
		}

		[Test]
		public void Validate_gap()
		{
			var markups = new[] {
				new MarkupConfig(address, 0, 100, 20),
				new MarkupConfig(address, 100, 200, 20),
				new MarkupConfig(address, 200, 1000, 20),
				new MarkupConfig(address, 0, 50, 20, MarkupType.VitallyImportant),
				new MarkupConfig(address, 50, 500, 20, MarkupType.VitallyImportant),
				new MarkupConfig(address, 500, 1000000, 20, MarkupType.VitallyImportant),
			};
			Assert.IsNull(MarkupConfig.Validate(markups));
		}

		[Test]
		public void Reset_validation_error()
		{
			var markups = new[] {
				new MarkupConfig(address, 0, 100, 20),
				new MarkupConfig(address, 80, 200, 20),
				new MarkupConfig(address, 0, 50, 20, MarkupType.VitallyImportant),
				new MarkupConfig(address, 50, 500, 20, MarkupType.VitallyImportant),
				new MarkupConfig(address, 500, 1000000, 20, MarkupType.VitallyImportant),
			};
			Assert.AreEqual("Некорректно введены границы цен.", MarkupConfig.Validate(markups)[0][1]);

			markups[1].Begin = 100;
			Assert.IsNull(MarkupConfig.Validate(markups));
			Assert.That(markups[1].BeginOverlap, Is.False);
		}

		[Test]
		public void Revalidate_on_edit()
		{
			var settings = new Settings();
			settings.AddMarkup(new MarkupConfig(address, 0, 100, 20));
			settings.AddMarkup(new MarkupConfig(address, 100, 200, 20));
			var markup = new MarkupConfig(address, 150, 300, 20);
			var changes = markup.CollectChanges();
			settings.AddMarkup(markup);
			Assert.IsTrue(markup.BeginOverlap);
			Assert.AreEqual("BeginOverlap", changes.Implode(c => c.PropertyName));

			markup.Begin = 200;
			Assert.IsFalse(markup.BeginOverlap);
			Assert.AreEqual("BeginOverlap, BeginOverlap", changes.Implode(c => c.PropertyName));
		}

		[Test]
		public void Combine_validation_result()
		{
			var settings = new Settings(address);
			settings.Markups[2].End = 100;
			Assert.AreEqual("Некорректно введены границы цен.", settings.Validate()[0][1]);
		}

		[Test]
		public void Reject_withou_vitally_important_markups()
		{
			var settings = new Settings(address);
			settings.Markups.RemoveEach(settings.Markups.Where(m => m.Type == MarkupType.VitallyImportant));
			Assert.AreEqual("Не заданы обязательные интервалы границ цен: [0, 50], [50, 500], [500, 1000000].", settings.Validate()[0][1]);
		}

		[Test]
		public void Check_mandatory_ranges()
		{
			var settings = new Settings(address);
			var markups = settings.Markups.Where(m => m.Type == MarkupType.VitallyImportant).OrderBy(m => m.Begin).ToArray();
			markups[0].End = 40;
			markups[1].Begin = 40;
			Assert.AreEqual("Не заданы обязательные интервалы границ цен: [0, 50], [50, 500], [500, 1000000].", settings.Validate()[0][1]);
		}

		[Test]
		public void Validate_for_address()
		{
			var settings = new Settings(address);
			settings.CopyMarkups(address, new Address("Тестовый") {
				Id = 1
			});
			Assert.IsNull(settings.Validate());
		}
	}
}