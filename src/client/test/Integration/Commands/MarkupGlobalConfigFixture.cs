using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture]
	public class MarkupGlobalConfigFixture : MixedFixture
	{
		[Test]
		public void MarkupGlobalSettingsGetFixture()
		{
			Fixture<MarkupGlobalConfigClientWithFlag>();
			Run(new UpdateCommand());
			var markupGlobalConfigList = localSession.Query<MarkupGlobalConfig>().ToList();
			Assert.IsTrue(markupGlobalConfigList.Count == 6);

			Assert.IsTrue(markupGlobalConfigList.Count(s => s.Type == MarkupType.VitallyImportant) == 3);
			Assert.IsTrue(markupGlobalConfigList.Count(s => s.Type == MarkupType.Nds18) == 1);
			Assert.IsTrue(markupGlobalConfigList.Count(s => s.Type == MarkupType.Special) == 1);
			Assert.IsTrue(markupGlobalConfigList.Count(s => s.Type == MarkupType.Over) == 1);

			var settings = new Settings();
			var addresses = localSession.Query<Address>().ToList();
			Assert.IsTrue(!settings.HasMarkupGlobalConfig);
			settings.SetGlobalMarkupsSettingsForAddress(addresses, markupGlobalConfigList);
			Assert.IsTrue(settings.HasMarkupGlobalConfig);
		}

		[Test]
		public void MarkupGlobalSettingsNoCLientFlagGetFixture()
		{
			Fixture<MarkupGlobalConfigClientWithoutFlag>();
			Run(new UpdateCommand());
			var markupGlobalConfigList = localSession.Query<MarkupGlobalConfig>().ToList();
			Assert.IsTrue(markupGlobalConfigList.Count == 0);
			var settings = new Settings();
			var addresses = localSession.Query<Address>().ToList();
			Assert.IsFalse(settings.HasMarkupGlobalConfig);
			settings.SetGlobalMarkupsSettingsForAddress(addresses, markupGlobalConfigList);
			Assert.IsFalse(settings.HasMarkupGlobalConfig);
		}
	}
}