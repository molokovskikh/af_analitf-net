using NHibernate;

namespace AnalitF.Net.Client.Models
{
	public class Settings
	{
		public virtual int Id { get; set; }

		public virtual bool CanViewOffersByCatalogName { get; set; }
		public virtual bool GroupByProduct { get; set; }
		public virtual bool ShowPriceName { get; set; }
		public virtual int BaseFromCategory { get; set; }

		public virtual void ApplyChanges(ISession session)
		{
			session
				.CreateSQLQuery("update prices set BasePrice = Category > :baseCategory")
				.SetParameter("baseCategory", BaseFromCategory)
				.ExecuteUpdate();
		}
	}
}