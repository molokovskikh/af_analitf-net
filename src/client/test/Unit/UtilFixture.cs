using System;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Common.Tools;
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
	}
}