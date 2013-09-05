using System.Dynamic;
using System.Windows.Forms.VisualStyles;
using AnalitF.Net.Client.Helpers;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class UtilFixture
	{
		public class Test
		{
			public Test Item { get; set; }

			public string Name { get; set; }
		}
		[Test]
		public void Set_value()
		{
			var d = new Test { Item = new Test { Name = "123" } };
			Util.SetValue(d, "Item.Name", "456");
			Assert.AreEqual("456", Util.GetValue(d, "Item.Name"));
		}
	}
}