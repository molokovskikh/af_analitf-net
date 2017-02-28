using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
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
			InitFields();
			Waybill = waybill;
			DisplayName = "Создание накладной";
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public Waybill Waybill { get; set; }

		public void OK()
		{
			if (!IsValide(Waybill))
				return;
			WasCancelled = false;
			TryClose();
		}
	}
}