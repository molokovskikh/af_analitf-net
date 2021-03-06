﻿using System;
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
			if (UseSupplierList)
				RxQuery(x => x.Query<Supplier>().OrderBy(s => s.Name).ToArray()).Subscribe(Suppliers);
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public Waybill Waybill { get; set; }
		public NotifyValue<Supplier[]> Suppliers { get; set; }

		public bool UseSupplierList => !(User?.IsStockEnabled ?? false);
		public bool DontUseSupplierList => !UseSupplierList;

		public void OK()
		{
			if (!IsValide(Waybill))
				return;
			WasCancelled = false;
			TryClose();
		}
	}
}