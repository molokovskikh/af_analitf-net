using System;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;
namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class RequirementWaybillName
	{
		public RequirementWaybillName()
		{
		}

		public string DemandPos { get; set; }
		public string DemandFio { get; set; }
		public string ReceiptPos { get; set; }
		public string ReceiptFio { get; set; }
		public string ResolPos { get; set; }
		public string ResolFio { get; set; }
		public string RemisPos { get; set; }
		public string RemisFio { get; set; }
		public string ExecutorPos { get; set; }
		public string ExecutorFio { get; set; }
	}

	public class RequirementWaybill : BaseScreen2, ICancelable
	{
		public bool WasCancelled { get; private set; }
		public RequirementWaybillName requirementWaybillName { get; set; }

		public RequirementWaybill()
		{
			DisplayName = "Данные для печати требование-накладная";
			WasCancelled = true;
			requirementWaybillName = new RequirementWaybillName();
		}
		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}

		public void Close()
		{
			requirementWaybillName = null;
			TryClose();
		}
	}
}
