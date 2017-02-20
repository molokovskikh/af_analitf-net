using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CreateDisplacementDoc : BaseScreen, ICancelable
	{
		public CreateDisplacementDoc(DisplacementDoc doc)
		{
			InitFields();
			Doc = doc;
			DisplayName = "Создание накладной перемещения";
			WasCancelled = true;
		}

		public DisplacementDoc Doc { get; set; }
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
