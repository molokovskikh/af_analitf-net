using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection.Emit;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class ReceivingDetails : BaseScreen2
	{
		private uint id;
		private List<ReceivingLine> deletedLines = new List<ReceivingLine>();
		private List<ReceivingDetail> deletedDetails = new List<ReceivingDetail>();

		public ReceivingDetails()
		{
			DisplayName = "Заказ на приемку";
		}

		public ReceivingDetails(uint id)
			: this()
		{
			this.id = id;
		}

		public NotifyValue<ReceivingOrder> Header { get; set; }
		public NotifyValue<ReceivingLine> CurrentLine { get; set; }
		public NotifyValue<ObservableCollection<ReceivingLine>> Lines { get; set; }
		public NotifyValue<ObservableCollection<ReceivingDetail>> Details { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (Header.Value == null) {
				RxQuery(x => x.Query<ReceivingOrder>()
						.Fetch(y => y.Supplier)
						.Fetch(y => y.Address)
						.FirstOrDefault(y => y.Id == id))
					.Subscribe(Header);
				RxQuery(x => {
						var lines = x.Query<ReceivingLine>().Where(y => y.ReceivingOrderId == id).OrderBy(y => y.Product)
							.ToList()
							.ToObservableCollection();
						var details = x.Query<ReceivingDetail>().Where(y => y.ReceivingOrderId == id).ToList();
						foreach (var line in lines)
							line.Details = details.Where(y => y.LineId == line.Id).ToList();
						return lines;
					})
					.Subscribe(Lines);
			}
			CurrentLine.Throttle(Consts.ScrollLoadTimeout, UiScheduler)
				.Select(x => x?.Details?.ToObservableCollection())
				.Subscribe(Details);
		}

		public void Save()
		{
			Header.Value.UpdateStat(Lines.Value);
			if (Header.Value.Id == 0) {
				StatelessSession.Insert(Header.Value);
			} else {
				try {
					StatelessSession.Update(Header.Value);
				} catch(StaleObjectStateException) {
				}
			}
			foreach (var stock in Lines.Value) {
				if (stock.Id == 0) {
					stock.ReceivingOrderId = Header.Value.Id;
					StatelessSession.Insert(stock);
				} else {
					try {
						StatelessSession.Update(stock);
					} catch(StaleObjectStateException) {
					}
				}
			}
			foreach (var stock in deletedLines.Where(x => x.Id > 0))
				StatelessSession.Delete(stock);
			deletedLines.Clear();
			Header.Refresh();
			Lines.Refresh();
		}

		public IEnumerable<IResult> AddLine()
		{
			var stock = new Stock();
			var edit = new EditStock(stock);
			yield return new DialogResult(edit);
			var line = new ReceivingLine();
			line.CopyFromStock(stock);
			Lines.Value.Add(line);
		}

		public void DeleteLine()
		{
			if (CurrentLine.Value == null)
				return;
			if (CurrentLine.Value.Status != ReceivingLineStatus.New) {
				Manager.Warning("Удалить можно только в строку в статусе 'Новый'");
				return;
			}
			deletedLines.Add(CurrentLine.Value);
			Lines.Value.Remove(CurrentLine.Value);
		}

		public IEnumerable<IResult> EditLine()
		{
			if (CurrentLine.Value == null)
				yield break;
			var stock = new Stock();
			CurrentLine.Value.CopyToStock(stock);
			var edit = new EditStock(stock);
			yield return new DialogResult(edit);
			CurrentLine.Value.CopyFromStock(stock);
		}

		public IEnumerable<IResult> EnterLine()
		{
			return EditLine();
		}

		public void Receive()
		{
			var stocks = Header.Value.Receive(Lines.Value);
			StatelessSession.SaveEach(stocks);
			Save();
		}

		public static IScreen FromWaybill(uint waybillId)
		{
			var model = new ReceivingDetails();
			var waybill = model.Session.Load<Waybill>(waybillId);
			var order = new ReceivingOrder {
				Date = DateTime.Now,
				Address = waybill.Address,
				Supplier = waybill.Supplier,
				WaybillDate = waybill.DocumentDate,
				WaybillId = waybill.Id,
			};
			var lines = waybill.Lines.Select(x => new ReceivingLine {
					Product = x.Product,
					Producer = x.Producer,
					Count = x.Quantity.GetValueOrDefault(),
					Cost = x.SupplierCost.GetValueOrDefault(),
					RetailCost = x.RetailCost.GetValueOrDefault(),
					Details = new List<ReceivingDetail> {
						new ReceivingDetail {
							Product = x.Product,
							Producer = x.Producer,
							Count = x.Quantity.GetValueOrDefault(),
							Cost = x.SupplierCost.GetValueOrDefault(),
							RetailCost = x.RetailCost.GetValueOrDefault(),
						}
					}
				})
				.ToArray();
			foreach (var line in lines)
				line.UpdateStatus();
			order.UpdateStat(lines);

			model.Header.Value = order;
			model.Lines.Value = lines.ToObservableCollection();
			return model;
		}
	}
}