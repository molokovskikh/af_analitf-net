using System;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;
using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using System.Linq;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class EditDefectSeries : BaseScreen2, ICancelable
	{
		private uint id;
		public bool WasCancelled { get; private set; }
		public Stock Stock { get; set; }
		public IEnumerable<Reject> Rejects { get; set; }

		public EditDefectSeries()
		{
			DisplayName = "Проверить позицию";
			WasCancelled = true;
		}

		public EditDefectSeries(Stock stock, List<Tuple<uint, uint>> link)
		{
			Stock = stock;
			Rejects = GetRejectsByStock(stock, link);
		}

		public void Ok()
		{
			Stock.RejectStatus = RejectStatus.Defective;
			WasCancelled = false;
			TryClose();
		}

		public void Not()
		{
			Stock.RejectStatus = RejectStatus.NotDefective;
			WasCancelled = false;
			TryClose();
		}

		public void Close()
		{
			if (id > 0)
				Session.Refresh(Stock);
			TryClose();
		}

		private IEnumerable<Reject> GetRejectsByStock(Stock stock, List<Tuple<uint, uint>> link)
		{
			var rejectIds = link.Where(x => x.Item1 == stock.Id).Select(x => x.Item2).ToList();
			return StatelessSession.Query<Reject>().Where(x => rejectIds.Contains(x.Id)).ToList();
		}
	}
}
