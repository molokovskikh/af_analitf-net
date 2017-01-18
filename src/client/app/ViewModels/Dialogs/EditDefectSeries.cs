using System;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Inventory;
using System.Collections.Generic;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using System.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class EditDefectSeries : BaseScreen2, ICancelable
	{
		public bool WasCancelled { get; private set; }
		public Stock Stock { get; set; }
		List<Tuple<uint, uint>> Link { get; set; }
		public NotifyValue<IEnumerable<Reject>> Rejects { get; set; }

		public EditDefectSeries()
		{
			DisplayName = "Проверить позицию";
			WasCancelled = true;
		}

		public EditDefectSeries(Stock stock, List<Tuple<uint, uint>> link)
		{
			Stock = stock;
			Link = link;
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			RxQuery(GetRejectsByStock).Subscribe(Rejects);
		}

		public async Task Ok()
		{
			Stock.RejectStatus = RejectStatus.Defective;
			await Env.Query(s => s.Update(Stock));
			WasCancelled = false;
			TryClose();
		}

		public async Task Not()
		{
			Stock.RejectStatus = RejectStatus.NotDefective;
			await Env.Query(s => s.Update(Stock));
			WasCancelled = false;
			TryClose();
		}

		public void Close()
		{
			TryClose();
		}

		private IEnumerable<Reject> GetRejectsByStock(IStatelessSession session)
		{
			var rejectIds = Link.Where(x => x.Item1 == Stock.Id).Select(x => x.Item2).ToList();
			return session.Query<Reject>().Where(x => rejectIds.Contains(x.Id)).ToList();
		}
	}
}
