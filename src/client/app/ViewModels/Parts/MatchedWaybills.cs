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
		public MatchedWaybills(IStatelessSession session,
			NotifyValue<SentOrderLine> line,
			NotifyValue<bool> isSentSelected,
			IScheduler uiScheduler)
		{
			CurrentWaybillLine = new NotifyValue<WaybillLine>();
			WaybillLines = line.Throttle(Consts.ScrollLoadTimeout, uiScheduler)
				.Select(v => LoadMatchedWaybill(v, session))
				.ToValue();
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
				CurrentWaybillLine.Value = lines[0];
				return lines[0].Waybill.Lines.OrderBy(l => l.Product).ToList();
			}
			return lines;
		}
	}
}