using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using AnalitF.Net.Client.Views.Dialogs;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.ViewModels.Parts
{
	public class PromotionPopup : ViewAware
	{
		private IStatelessSession session;
		private Config.Config config;

		public uint? FilterBySupplierId;

		public PromotionPopup(IStatelessSession session, Config.Config config)
		{
			Name = new NotifyValue<CatalogName>();
			Visible = new NotifyValue<bool>();
			Promotions = new NotifyValue<List<Promotion>>(new List<Promotion>());
			this.session = session;
			this.config = config;
		}

		public NotifyValue<CatalogName> Name { get; set; }
		public NotifyValue<List<Promotion>> Promotions { get; set; }
		public NotifyValue<bool> Visible { get; set; }

		public void Activate(CatalogName name)
		{
			if (name == null) {
				Visible.Value = false;
				return;
			}
			Name.Value = name;
			var nameId = name.Id;
			var query = session.Query<Promotion>().Where(p => p.Catalogs.Any(c => c.Name.Id == nameId));
			if (FilterBySupplierId != null)
				query = query.Where(p => p.Supplier.Id == FilterBySupplierId);
			Promotions.Value = query
				.OrderBy(p => p.Name)
				.Fetch(p => p.Supplier)
				.ToList();
			Promotions.Value.Each(p => p.Init(config));
			Visible.Value = Promotions.Value.Count > 0;
		}

		public void Deactivate()
		{
			Visible.Value = false;
		}

		public void Hide()
		{
			Visible.Value = false;
		}

		public IResult Open(Promotion promotion)
		{
			return new DialogResult(new DocModel<Promotion>(promotion.Id));
		}
	}
}