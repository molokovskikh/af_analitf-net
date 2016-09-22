using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class CreateReturnToSupplier : BaseScreen, ICancelable
	{
		public CreateReturnToSupplier(ReturnToSupplier returnToSupplier)
		{
			ReturnToSupplier = returnToSupplier;
			Suppliers = StatelessSession.Query<Supplier>().OrderBy(s => s.Name).ToArray();
			DisplayName = "Создание Возврат поставщику";
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public ReturnToSupplier ReturnToSupplier { get; set; }
		public Supplier[] Suppliers { get; set; }

		public void OK()
		{
			foreach (var field in ReturnToSupplier.FieldsForValidate) {
				var error = ReturnToSupplier[field];
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