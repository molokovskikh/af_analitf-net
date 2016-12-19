using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using System;
using Common.MySql;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Models.Commands;
using ReactiveUI;
using AnalitF.Net.Client.ViewModels.Dialogs;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class GoodsMovement : BaseScreen2
	{
		public ReactiveCollection<Catalog> Items { get; set; }

		public NotifyValue<Catalog> CurrentItem { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<IList<Selectable<Address>>> Addresses2 { get; set; }
		public NotifyValue<IList<Selectable<Supplier>>> Suppliers { get; set; }
		public NotifyValue<IList<Selectable<Producer>>> Producers { get; set; }
		public bool Can2Add => Suppliers.Value.Any();
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> Can2ExportExcel { get; set; }
		public bool CanShowDescription => CurrentItem.Value?.Name?.Description != null;

		public GoodsMovement()
		{
			DisplayName = "Движение товара по накладным";
			Begin = new NotifyValue<DateTime>(DateTime.Today.AddMonths(-3));
			End = new NotifyValue<DateTime>(DateTime.Today);
			Items = new ReactiveCollection<Catalog>();
			Addresses2.Value = new List<Selectable<Address>>();
			Suppliers.Value = new List<Selectable<Supplier>>();
			Producers.Value = new List<Selectable<Producer>>();

			CurrentItem.Subscribe(x => {
				CanDelete.Value = Can2ExportExcel.Value = x != null;
			});
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Addresses2.Value = Addresses.Select(x => new Selectable<Address>(x)).ToList();

			Env.RxQuery(s => s.Query<Waybill>()
				.Where(x => x.Status == DocStatus.Posted)
				.Select(x => x.Supplier).ToList()
				.Distinct().OrderBy(x => x.Name).ToList()
				.Select(x => new Selectable<Supplier>(x)).ToList())
			.Subscribe(Suppliers);

			Env.RxQuery(s => {
				var producerIds = s.Query<WaybillLine>()
				.Where(x => x.Waybill.Status == DocStatus.Posted)
				.Select(x => x.ProducerId).Distinct().ToList();
				return s.Query<Producer>().Where(x => producerIds.Contains(x.Id))
				.Select(x => new Selectable<Producer>(x)).ToList();
			})
			.Subscribe(Producers);
		}

		public void ShowDescription()
		{
			if (!CanShowDescription)
				return;
			Manager.ShowDialog(new DocModel<ProductDescription>(CurrentItem.Value.Name.Description.Id));
		}

		public IEnumerable<IResult> Add()
		{
			if (!Can2Add) {
				yield return MessageResult.Warn("Движения товаров по накладным не обнаружено");
				yield break;
			}
			var catalogList = new List<Catalog>();
			yield return new DialogResult(new CatalogViewModel(catalogList), fullScreen: true);
			catalogList.AddRange(Items);
			catalogList = catalogList.Distinct().OrderBy(x => x.FullName).ToList();
			Items = new ReactiveCollection<Catalog>(catalogList);
			if (Items.Any())
				CurrentItem.Value = Items.First();
			Refresh();
		}

		public void Delete()
		{
			if (CurrentItem.Value == null)
				return;
			Items.Remove(CurrentItem.Value);
			Refresh();
		}

		public IEnumerable<IResult> ExportExcel()
		{
			if (!Can2ExportExcel) {
				yield return MessageResult.Warn("Не выбраны товары. Используйте \"Добавить товар в список\"");
				yield break;
			}
			var settings = new GoodsMovementReportSettings()
			{
				Begin = Begin.Value,
				End = End.Value,
				FilterByWriteTime = false,
				CatalogIds = Items.Select(x => x.Id).ToArray(),
				CatalogNames = Items.Select(x => x.FullName).ToList().Implode()
			};

			if (!Addresses2.Value.All(x => x.IsSelected)) {
				settings.AddressIds = Addresses2.Value.Where(x => x.IsSelected).Select(x => x.Item.Id).ToArray();
				settings.AddressNames = Addresses2.Value.Where(x => x.IsSelected).Select(x => x.Item.Name).ToList().Implode();
			}
			if (!Suppliers.Value.All(x => x.IsSelected)) {
				settings.SupplierIds = Suppliers.Value.Where(x => x.IsSelected).Select(x => x.Item.Id).ToArray();
				settings.SupplierNames = Suppliers.Value.Where(x => x.IsSelected).Select(x => x.Item.FullName).ToList().Implode();
			}
			if (!Producers.Value.All(x => x.IsSelected)) {
				settings.ProducerIds = Producers.Value.Where(x => x.IsSelected).Select(x => x.Item.Id).ToArray();
				settings.ProducerNames = Producers.Value.Where(x => x.IsSelected).Select(x => x.Item.Name).ToList().Implode();
			}

			var commnand = new GoodsMovementReport(settings);
			yield return new Models.Results.TaskResult(commnand.ToTask(Shell.Config));
			yield return new OpenResult(commnand.Result);
		}
	}
}