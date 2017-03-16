using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	class CreateReturnToSupplier : BaseScreen, ICancelable
	{
		public CreateReturnToSupplier(ReturnDoc doc)
		{
			InitFields();
			Doc = doc;
			Suppliers = Session.Query<Supplier>().ToArray();
			DisplayName = "Создание накладной возврата";
			WasCancelled = true;
		}

		public ReturnDoc Doc { get; set; }
		public bool WasCancelled { get; private set; }
		public Supplier[] Suppliers { get; set; }

		public void OK()
		{
			if (!IsValide(Doc))
				return;
			WasCancelled = false;
			TryClose();
		}
	}
}
