using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	class CreateReassessmentDoc : BaseScreen, ICancelable
	{
		public CreateReassessmentDoc(ReassessmentDoc doc)
		{
			InitFields();
			Doc = doc;
			DisplayName = "Создание документа переоценки";
			WasCancelled = true;
		}

		public ReassessmentDoc Doc { get; set; }
		public bool WasCancelled { get; private set; }

		public void OK()
		{
			if (!IsValide(Doc))
				return;
			WasCancelled = false;
			TryClose();
		}
	}
}
