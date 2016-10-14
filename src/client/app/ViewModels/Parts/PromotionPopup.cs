using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.Views.Dialogs;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class PromotionPopup : ViewAware
	{
		public uint? FilterBySupplierId;

		public PromotionPopup(Config.Config config,
			IObservable<CatalogName> catalog,
			Env env)
		{
			Name = new NotifyValue<CatalogName>(catalog);
			Visible = new NotifyValue<bool>();
			Promotions = new NotifyValue<List<Promotion>>(new List<Promotion>());
			catalog
				.Throttle(Consts.ScrollLoadTimeout, env.Scheduler)
				.Select(x => env.RxQuery(s => {
					if (x == null)
						return new List<Promotion>();
					var nameId = x.Id;
					var query = s.Query<Promotion>().Where(p => p.Catalogs.Any(c => c.Name.Id == nameId));
					if (FilterBySupplierId != null)
						query = query.Where(p => p.Supplier.Id == FilterBySupplierId);
					return query
						.OrderBy(p => p.Name)
						.Fetch(p => p.Supplier)
						.ToList();
				}))
				.Switch()
				.Subscribe(x => {
					Promotions.Value = x;
					Promotions.Value?.Each(p => p.Init(config));
					Visible.Value = Promotions.Value?.Count > 0;
				});
		}

		public NotifyValue<CatalogName> Name { get; set; }
		public NotifyValue<List<Promotion>> Promotions { get; set; }
		public NotifyValue<bool> Visible { get; set; }

		public void Hide()
		{
			Visible.Value = false;
		}

		public IResult Open(Promotion promotion)
		{
			return new DialogResult(new DocModel<Promotion>(promotion.Id), fixedSize: true);
		}
	}
}