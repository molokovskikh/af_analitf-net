using System;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Controls;
using System.Collections.Generic;
using Common.Tools;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class CreateDreamReport : BaseScreen, ICancelable
	{
		public CreateDreamReport(DreamReportSettings settings)
		{
			InitFields();
			DrSettings = settings;
			Begin.Value = settings.Begin;
			End.Value = settings.End;
			DisplayName = "Движение товара по накладным";
			WasCancelled = true;
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			Addresses2.Value = Addresses.Select(x => new Selectable<Address>(x)).ToList();
			Env.RxQuery(s => s.Query<Waybill>()
					.Select(x => x.Supplier).ToList()
					.Distinct().OrderBy(x => x.Name).ToList()
					.Select(x => new Selectable<Supplier>(x)).ToList())
				.Subscribe(Suppliers);
			Env.RxQuery(s => {
					var producerIds = s.Query<WaybillLine>()
						.Where(x => DrSettings.CatalogIds.ToList().Contains(x.CatalogId.Value))
						.Select(x => x.ProducerId).Distinct().ToList();
					return s.Query<Producer>().Where(x => producerIds.Contains(x.Id)).Select(x => new Selectable<Producer>(x)).ToList();
				})
				.Subscribe(Producers);
		}

		public bool WasCancelled { get; private set; }
		public DreamReportSettings DrSettings { get; set; }
		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<IList<Selectable<Address>>> Addresses2 { get; set; }
		public NotifyValue<IList<Selectable<Supplier>>> Suppliers { get; set; }
		public NotifyValue<IList<Selectable<Producer>>> Producers { get; set; }

		public void OK()
		{
			if (!Addresses2.Value.All(x => x.IsSelected)) {
				DrSettings.AddressIds = Addresses2.Value.Where(x => x.IsSelected).Select(x => x.Item.Id).ToArray();
				DrSettings.AddressNames = Addresses2.Value.Where(x => x.IsSelected).Select(x => x.Item.Name).ToList().Implode();
			}
			if (!Suppliers.Value.All(x => x.IsSelected)) {
				DrSettings.SupplierIds = Suppliers.Value.Where(x => x.IsSelected).Select(x => x.Item.Id).ToArray();
				DrSettings.SupplierNames = Suppliers.Value.Where(x => x.IsSelected).Select(x => x.Item.FullName).ToList().Implode();
			}
			if (!Producers.Value.All(x => x.IsSelected)) {
				DrSettings.ProducerIds = Producers.Value.Where(x => x.IsSelected).Select(x => x.Item.Id).ToArray();
				DrSettings.CatalogNames = Producers.Value.Where(x => x.IsSelected).Select(x => x.Item.Name).ToList().Implode();
			}

			DrSettings.Begin = Begin.Value;
			DrSettings.End = End.Value;

			WasCancelled = false;
			TryClose();
		}
	}
}