using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.UI.WebControls;
using System.Windows.Controls;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class EditStock : BaseScreen2, ICancelable
	{
		public EditStock(uint id)
		{
			Stock = Session.Get<Stock>(id);

			DisplayName = "Информация о товаре";
			WasCancelled = true;
		}

		public bool WasCancelled { get; private set; }
		public Stock Stock { get; set; }

		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}

		public void Close()
		{
			Session.Refresh(Stock);
			TryClose();
		}
	}
}
