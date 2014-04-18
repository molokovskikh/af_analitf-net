using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class CreateWaybill : BaseScreen
	{
		public CreateWaybill(Waybill waybill)
		{
			Waybill = waybill;
			Suppliers = StatelessSession.Query<Supplier>().OrderBy(s => s.Name).ToArray();
		}

		public Waybill Waybill { get; set; }

		public Supplier[] Suppliers { get; set; }

		public void OK()
		{
			var fields = new [] { "ProviderDocumentId", "UserSupplierName" };
			foreach (var field in fields) {
				var error = Waybill[field];
				if (!string.IsNullOrEmpty(error)) {
					Manager.Warning(error);
					return;
				}
			}
			TryClose(true);
		}
	}
}