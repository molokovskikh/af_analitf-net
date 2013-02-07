using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models
{
	public enum DiffCalcMode
	{
		[Description("От предыдущего предложения")] PrevOffer,
		[Description("От минимальной цены")] MinCost,
		[Description("От минимальной цены в осноных поставщиках")] MinBaseCost,
	}

	public class Settings
	{
		public Settings()
		{
			OverCountWarningFactor = 5;
			OverCostWarningPercent = 5;
		}

		public virtual int Id { get; set; }

		public virtual bool CanViewOffersByCatalogName { get; set; }
		public virtual bool GroupByProduct { get; set; }
		public virtual bool ShowPriceName { get; set; }
		public virtual int BaseFromCategory { get; set; }

		public virtual decimal OverCountWarningFactor { get; set; }

		public virtual decimal OverCostWarningPercent { get; set; }

		public virtual decimal MaxOverCostOnRestoreOrder { get; set; }

		public virtual DiffCalcMode DiffCalcMode { get; set; }

		public virtual string UserName { get; set; }

		public virtual string Password { get; set; }

		public virtual DateTime? LastUpdate { get; set; }

		public virtual bool IsValid
		{
			get { return !String.IsNullOrEmpty(Password) && !String.IsNullOrEmpty(UserName); }
		}

		public virtual void ApplyChanges(ISession session)
		{
			session
				.CreateSQLQuery("update prices set BasePrice = Category > :baseCategory")
				.SetParameter("baseCategory", BaseFromCategory)
				.ExecuteUpdate();

			var prices = session.Query<Price>().ToList();

			UpdatePriceNames(prices);
		}

		public virtual void UpdatePriceNames(List<Price> prices)
		{
			if (ShowPriceName) {
				prices.Each(p => p.Name = String.Format("{0} {1}", p.SupplierName, p.PriceName));
			}
			else {
				var groups = prices.GroupBy(p => p.SupplierId).ToDictionary(g => g.Key, g => g.Count());
				prices.Each(p => {
					if (groups[p.SupplierId] > 1)
						p.Name = String.Format("{0} {1}", p.SupplierName, p.PriceName);
					else
						p.Name = p.SupplierName;
				});
			}
		}
	}
}