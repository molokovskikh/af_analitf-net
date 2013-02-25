using AnalitF.Net.Client.Models;
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
			var isValid = MarkupConfig.Validate(markups);
			Assert.That(isValid, Is.False);
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
			var isValid = MarkupConfig.Validate(markups);
			Assert.That(isValid, Is.True);
		}

		[Test]
		public void Reset_validation_error()
		{
			var markups = new[] {
				new MarkupConfig(0, 100, 20),
				new MarkupConfig(80, 200, 20)
			};
			var isValid = MarkupConfig.Validate(markups);
			Assert.That(isValid, Is.False);

			markups[1].Begin = 100;
			isValid = MarkupConfig.Validate(markups);
			Assert.That(isValid, Is.True);
			Assert.That(markups[1].BeginOverlap, Is.False);
		}
	}
}