using System.ComponentModel;
using AnalitF.Net.Client.ViewModels;
using NHibernate;

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
		public virtual int Id { get; set; }

		public virtual bool CanViewOffersByCatalogName { get; set; }
		public virtual bool GroupByProduct { get; set; }
		public virtual bool ShowPriceName { get; set; }
		public virtual int BaseFromCategory { get; set; }

		public virtual DiffCalcMode DiffCalcMode { get; set; }

		public virtual void ApplyChanges(ISession session)
		{
			session
				.CreateSQLQuery("update prices set BasePrice = Category > :baseCategory")
				.SetParameter("baseCategory", BaseFromCategory)
				.ExecuteUpdate();
		}
	}
}