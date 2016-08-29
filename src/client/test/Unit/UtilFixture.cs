using System;
using System.IO;
using AnalitF.Net.Client.Helpers;
using Common.NHibernate;
using Newtonsoft.Json;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class UtilFixture
	{
		public class Test
		{
			public Test Item { get; set; }

			public string Name { get; set; }

			public int I;
		}

		[Test]
		public void Set_value()
		{
			var d = new Test { Item = new Test { Name = "123" } };
			Util.SetValue(d, "Item.Name", "456");
			Assert.AreEqual("456", Util.GetValue(d, "Item.Name"));
			Util.SetValue(d, "I", 1);
			Assert.AreEqual(1, d.I);
		}

		[Test]
		public void Get_value()
		{
			Assert.AreEqual(1, Util.GetValue(new Test { I = 1 }, "I"));
		}

		[Test]
		public void Error_helper()
		{
			Assert.IsNull(ErrorHelper.TranslateException(new Exception("test")));
		}

		public class TestClass
		{
			public NotifyValue<string> Value { get; set; } = new NotifyValue<string>();
		}

		[Test]
		public void Json()
		{
			var model = new TestClass();
			var serializer = new JsonSerializer {
				ContractResolver = new NHibernateResolver()
			};
			serializer.Converters.Add(new NotifyValueConvert());
			serializer.Populate(new StringReader("{Value: null}"), model);
			Assert.IsNotNull(model.Value);

			serializer.Populate(new StringReader("{Value: \"123\"}"), model);
			Assert.AreEqual("123", model.Value.Value);
		}
	}
}