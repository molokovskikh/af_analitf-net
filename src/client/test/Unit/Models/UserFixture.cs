using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Orders;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit.Models
{
	[TestFixture]
	public class UserFixture
	{
		[Test]
		public void Can_print()
		{
			var user = new User();
			Assert.IsFalse(user.CanPrint<RejectsDocument>());
			user.Permissions.Add(new Permission("PBP"));
			Assert.IsTrue(user.CanPrint<RejectsDocument>());

			Assert.IsFalse(user.CanPrint<OrderLinesDocument, OrderLine>());
			user.Permissions.Add(new Permission("PCCO"));
			Assert.IsTrue(user.CanPrint<OrderLinesDocument, OrderLine>());
		}

		[Test]
		public void Can_export_order_details()
		{
			var user = new User();
			Assert.IsFalse(user.CanExport(typeof(OrderDetailsViewModel).Name + "." + typeof(SentOrder).Name));
		}

		[Test]
		public void Can_export()
		{
			var user = new User();
			Assert.IsTrue(user.CanExport("TestViewMode.Items"));
			Assert.IsFalse(user.CanExport("CatalogSearchViewModel.Items"));
		}

		[Test]
		public void Check_default_permissions()
		{
			var user = new User();
			Assert.IsFalse(user.HasStockPermission());
			Assert.IsTrue(user.HasOrderPermission());
			user.Permissions.Add(new Permission("STCK"));
			Assert.IsTrue(user.HasStockPermission());
			Assert.IsFalse(user.HasOrderPermission());
		}
	}
}