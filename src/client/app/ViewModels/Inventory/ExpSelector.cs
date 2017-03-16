using System;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ExpSelector : Screen, ICancelable
	{
		public ExpSelector(DateTime[] exps)
		{
			WasCancelled = true;
			Exps = exps;
			CurrentExp = exps[0];
			DisplayName = "Укажите срок годности";
		}

		public string Name { get; set; }
		public DateTime[] Exps { get; set; }
		public DateTime CurrentExp { get; set; }

		public bool WasCancelled { get; set; }

		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}
	}
}