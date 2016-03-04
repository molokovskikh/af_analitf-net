using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class CreateWaybill : BaseScreen, ICancelable
	{
		public CreateWaybill(Waybill waybill)
		{
			Waybill = waybill;
			Suppliers = StatelessSession.Query<Supplier>().OrderBy(s => s.Name).ToArray();
			DisplayName = "Создание накладной";
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public Waybill Waybill { get; set; }
		public Supplier[] Suppliers { get; set; }

		public void OK()
		{
			foreach (var field in Waybill.FieldsForValidate) {
				var error = Waybill[field];
				if (!string.IsNullOrEmpty(error)) {
					Manager.Warning(error);
					return;
				}
			}
			WasCancelled = false;
			TryClose();
		}
	}
}