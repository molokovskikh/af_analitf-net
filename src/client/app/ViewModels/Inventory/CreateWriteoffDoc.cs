using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class CreateWriteoffDoc : BaseScreen, ICancelable
	{
		public CreateWriteoffDoc(WriteoffDoc doc)
		{
			InitFields();
			Doc = doc;
			Reasons = Session.Query<WriteoffReason>().ToArray();
			DisplayName = "Создание документа списания";
			WasCancelled = true;
		}

		public WriteoffDoc Doc { get; set; }
		public bool WasCancelled { get; private set; }
		public WriteoffReason[] Reasons { get; set; }

		public void OK()
		{
			if (!IsValide(Doc))
				return;
			WasCancelled = false;
			TryClose();
		}
	}
}
