﻿using System;
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

		public override void TryClose()
		{
			TryClose(true);
		}

		public void OK()
		{
			ValidateAndClose(Waybill);
		}
	}
}