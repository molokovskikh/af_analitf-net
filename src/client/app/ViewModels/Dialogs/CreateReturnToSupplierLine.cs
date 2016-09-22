using System;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class CreateReturnToSupplierLine : BaseScreen, ICancelable
	{
		public CreateReturnToSupplierLine(ReturnToSupplierLine returnToSupplierLine)
		{
			ReturnToSupplierLine = returnToSupplierLine;
			Stocks = StatelessSession.Query<Stock>().OrderBy(s => s.Product).ToArray();
			DisplayName = "Создание строки Возврат поставщику";
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public ReturnToSupplierLine ReturnToSupplierLine { get; set; }
		public Stock[] Stocks { get; set; }

		public void OK()
		{
			foreach (var field in ReturnToSupplierLine.FieldsForValidate) {
				var error = ReturnToSupplierLine[field];
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