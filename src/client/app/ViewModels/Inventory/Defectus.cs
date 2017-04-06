using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using System;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels.Orders;
using NHibernate;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Defectus : BaseScreen2
	{
		public Defectus()
		{
			DisplayName = "Дефектура";
			CurrentItem.Subscribe(x => {
				CanDelete.Value = CanPost.Value = x != null;
			});
			TrackDb(typeof(Stock));
		}

		public NotifyValue<List<DefectusLine>> Items { get; set; }
		public NotifyValue<DefectusLine> CurrentItem { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanPost { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			Bus.Listen<string>("reload").Cast<object>()
				.Merge(DbReloadToken)
				.SelectMany(_ => RxQuery(LoadItems))
				.Subscribe(Items, CloseCancellation.Token);
		}

		protected override async void OnDeactivate(bool close)
		{
			await Env.Query(s => s.UpdateEach(Items.Value.Where(x => x.IsDirty)));
			base.OnDeactivate(close);
		}

		public List<DefectusLine> LoadItems(IStatelessSession session)
		{
			if (Address == null)
				return new List<DefectusLine>();
			// возвращает остатки по текущему адресу
			return session.CreateSQLQuery(@"
select IFNULL(SUM(s.Quantity), 0) as {d.Quantity}, {d.*}
from defectuslines d
left outer join stocks s on s.ProductId = d.ProductId and s.AddressId = :address
group by d.ProductId
order by d.Product")
			.AddEntity("d", typeof(DefectusLine))
			.SetParameter("address", Address.Id)
			.List<DefectusLine>()
			.ToList();
		}

		public IEnumerable<IResult> Add()
		{
			while (true) {
				var search = new AddDefectusLine();
				yield return new DialogResult(search);
				if (!search.Item.IsValid)
					continue;
				SaveDefectusLine(search.Item);
			}
		}

		private async void SaveDefectusLine(DefectusLine line)
		{
			await Env.Query(s => s.Insert(line));
			Update();
		}

		public async void Delete()
		{
			await Env.Query(s => s.Delete(CurrentItem.Value));
			Update();
		}

		private async void SaveBatchLines()
		{
			// должны быть настройки автозаказа MultiAddressSource = false
			var lines = Items.Value
 			.Where(x => x.Quantity <= x.Threshold && x.OrderQuantity > 0)
 			.Select(x => new BatchLine {
 				Address = Address,
 				ProductId = x.ProductId,
 				CatalogId = x.CatalogId,
 				ProductSynonym = x.Product,
 				ProducerId = x.ProducerId,
 				Producer = x.Producer,
 				ProducerSynonym = x.Producer,
 				Quantity = x.OrderQuantity,
 				ExportId = (uint)GetHashCode(),
 				Comment = "Заказано по минимальным остаткам",
 				Status = ItemToOrderStatus.NotOrdered,
			 }).ToList();

			await Env.Query(s => s.InsertEach(lines));
			//Bus.SendMessage(nameof(BatchLine), "db");
			Update();
		}


		public IEnumerable<IResult> Post()
		{
			SaveBatchLines();
			return Shell.Batch(mode: BatchMode.ReloadUnordered);
		}
	}
}