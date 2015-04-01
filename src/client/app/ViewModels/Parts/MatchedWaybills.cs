using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class MatchedWaybills : ViewAware
	{
		public MatchedWaybills(BaseScreen screen,
			NotifyValue<SentOrderLine> line,
			NotifyValue<bool> isSentSelected)
		{
			CurrentWaybillLine = new NotifyValue<WaybillLine>();
			WaybillLines = new NotifyValue<List<WaybillLine>>();
			WaybillDetailsVisibility = new NotifyValue<Visibility>(() => {
				if (!isSentSelected)
					return Visibility.Collapsed;
				if (WaybillLines.Value == null || WaybillLines.Value.Count == 0)
					return Visibility.Hidden;
				return Visibility.Visible;
			}, isSentSelected, WaybillLines);
			EmptyLabelVisibility = new NotifyValue<Visibility>(() => {
				if (!isSentSelected)
					return Visibility.Collapsed;
				if (WaybillLines.Value != null && WaybillLines.Value.Count > 0)
					return Visibility.Hidden;
				return Visibility.Visible;
			}, isSentSelected, WaybillLines);

			line.Throttle(Consts.ScrollLoadTimeout, screen.UiScheduler)
				.Select(v => screen.RxQuery(s => LoadMatchedWaybill(v, s)))
				.Switch()
				.ObserveOn(screen.UiScheduler)
				.Subscribe(WaybillLines, screen.CloseCancellation.Token);
		}

		public NotifyValue<Visibility> WaybillDetailsVisibility { get; set; }

		public NotifyValue<Visibility> EmptyLabelVisibility { get; set; }

		public NotifyValue<WaybillLine> CurrentWaybillLine { get; set; }

		public NotifyValue<List<WaybillLine>> WaybillLines { get; set; }

		private List<WaybillLine> LoadMatchedWaybill(SentOrderLine line, IStatelessSession session)
		{
			if (line == null)
				return new List<WaybillLine>();
			var ids = session
				.CreateSQLQuery("select DocumentLineId from WaybillOrders where OrderLineId = :orderLineId")
				.SetParameter("orderLineId", line.ServerId)
				.List()
				.Cast<object>()
				.Select(Convert.ToUInt32)
				.ToList();
			if (ids.Count == 0)
				return new List<WaybillLine>();
			var lines = session.Query<WaybillLine>()
				.Where(l => ids.Contains(l.Id))
				.Fetch(l => l.Waybill)
				.ThenFetch(l => l.Supplier)
				.Fetch(l => l.Waybill)
				.ThenFetchMany(w => w.Lines)
				.ToList();
			if (lines.Count > 0) {
				var result = lines[0].Waybill.Lines.OrderBy(l => l.Product).ToList();
				//будь бдителен - хотя с точки зрения бд lines[0] и result.First(l => l.Id == lines[0].Id) один и тот же объект
				//nhibernate интерпретирует их как два разных объекта и выделение строки в ui не будет работать
				CurrentWaybillLine.Value = result.First(l => l.Id == lines[0].Id);
				return result;
			}
			return lines;
		}

		public static Dictionary<uint, WaybillLine[]> GetLookUp(IStatelessSession session, IEnumerable<SentOrderLine> lines)
		{
				var ids = lines.Where(l => l.ServerId != null).Select(l => l.ServerId.Value).ToArray();
				var waybillOrders = session.Query<WaybillOrder>().Where(o => ids.Contains(o.OrderLineId)).ToArray();
				ids = waybillOrders.Select(o => o.DocumentLineId).ToArray();
				var waybillLines = session.Query<WaybillLine>().Where(o => ids.Contains(o.Id)).ToArray();
				return waybillOrders
					.GroupBy(g => g.OrderLineId, m => m.DocumentLineId)
					.ToDictionary(g => g.Key, g => waybillLines.Where(l => g.Contains(l.Id)).ToArray());
		}
	}
}