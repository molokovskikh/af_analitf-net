using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate.Mapping;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class MarkupConfigFixture
	{
		[Test]
		public void Validate()
		{
			var markups = new[] {
				new MarkupConfig(0, 100, 20),
				new MarkupConfig(80, 200, 20)
			};
			Assert.AreEqual("Некорректно введены границы цен.", MarkupConfig.Validate(markups));
			Assert.That(markups[1].BeginOverlap, Is.True);
		}

		[Test]
		public void Validate_gap()
		{
			var markups = new[] {
				new MarkupConfig(0, 100, 20),
				new MarkupConfig(100, 200, 20),
				new MarkupConfig(200, 1000, 20)
			};
			Assert.IsNull(MarkupConfig.Validate(markups));
		}

		[Test]
		public void Reset_validation_error()
		{
			var markups = new[] {
				new MarkupConfig(0, 100, 20),
				new MarkupConfig(80, 200, 20)
			};
			Assert.AreEqual("Некорректно введены границы цен.", MarkupConfig.Validate(markups));

			markups[1].Begin = 100;
			Assert.IsNull(MarkupConfig.Validate(markups));
			Assert.That(markups[1].BeginOverlap, Is.False);
		}

		[Test]
		public void Revalidate_on_edit()
		{
			var settings = new Settings();
			settings.AddMarkup(new MarkupConfig(0, 100, 20));
			settings.AddMarkup(new MarkupConfig(100, 200, 20));
			var markup = new MarkupConfig(150, 300, 20);
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
			var settings = new Settings(defaults: true);
			settings.Markups[2].End = 100;
			Assert.AreEqual("Некорректно введены границы цен.", settings.ValidateMarkups());
		}
	}
}