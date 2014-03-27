using AnalitF.Net.Client.Models;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class PriceFixture
	{
		[Test]
		public void Read_emails()
		{
			var price = new Price();
			price.Email = "m1@analit.net, m3@analit.net";
			Assert.That(price.Emails.Count, Is.EqualTo(2), price.Emails.Implode());
			var email = price.Emails[0];
			Assert.That(email.Name, Is.EqualTo("m1@analit.net"));
			Assert.That(email.Uri, Is.EqualTo("mailto:m1@analit.net,m3@analit.net"));
		}
	}
}