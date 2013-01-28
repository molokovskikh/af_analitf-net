using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class PriceFixture
	{
		[Test]
		public void Date_time_value_should_be_converted_to_local()
		{
			using(var session = SetupFixture.Factory.OpenSession()) {
				var price = session.Query<Price>().First();
				Assert.That(price.PriceDate.Kind, Is.EqualTo(DateTimeKind.Local));
			}
		}
	}
}