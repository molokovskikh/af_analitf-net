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
	public class ProducerPromotionPopup
	{
		public uint? FilterByProducerId;

		public ProducerPromotionPopup(Config.Config config,
			IObservable<CatalogName> catalog,
			Env env)
		{
			Name = new NotifyValue<CatalogName>(catalog);
			Visible = new NotifyValue<bool>();
			ProducerPromotions = new NotifyValue<List<ProducerPromotion>>(new List<ProducerPromotion>());
			catalog
				.Throttle(Consts.ScrollLoadTimeout)
				.SelectMany(x => env.RxQuery(s => {
					if (x == null)
						return new List<ProducerPromotion>();
					var nameId = x.Id;
					var query = s.Query<ProducerPromotion>().Where(p => p.Catalogs.Any(c => c.Name.Id == nameId));
					return query
						.OrderBy(p => p.Name)
						.Fetch(p => p.Producer)
						.ToList();
				}))
				.Subscribe(x => {
					ProducerPromotions.Value = x;
					ProducerPromotions.Value?.Each(p => p.Init(config));
					Visible.Value = ProducerPromotions.Value?.Count > 0;
				});
		}

		public NotifyValue<CatalogName> Name { get; set; }
		public NotifyValue<List<ProducerPromotion>> ProducerPromotions { get; set; }
		public NotifyValue<bool> Visible { get; set; }

		public void Hide()
		{
			Visible.Value = false;
		}

		public IResult Open(ProducerPromotion ProducerPromotion)
		{
			return new DialogResult(new DocModel<ProducerPromotion>(ProducerPromotion.Id), fixedSize: true);
		}

	}
}
