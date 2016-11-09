using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	class RequirementNegotiationProtocol : BaseScreen2, ICancelable
	{
		public bool WasCancelled { get; private set; }
		public string Fio { get; set; }

		public RequirementNegotiationProtocol()
		{
			DisplayName = "ФИО уполномоченного лица поставщика:";
			WasCancelled = true;
		}
		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}

		public void Close()
		{
			TryClose();
		}
	}

}
