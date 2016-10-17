using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models
{
	public class AwaitedItem : BaseStatelessObject, IDataErrorInfo2
	{
		public AwaitedItem()
		{
		}

		public AwaitedItem(Catalog catalog, Producer producer = null)
		{
			Catalog = catalog;
			Producer = producer;
		}

		public override uint Id { get; set; }
		public virtual Catalog Catalog { get; set; }
		public virtual Producer Producer { get; set; }

		[Style(Description = "Предложения отсутствуют")]
		public virtual bool DoNotHaveOffers
		{
			get { return Catalog.DoNotHaveOffers; }
		}

		public virtual string ProducerName
		{
			get
			{
				return Producer != null ? Producer.Name : "Все производители";
			}
		}

		public virtual string this[string columnName]
		{
			get
			{
				if (columnName == "Catalog") {
					if (Catalog == null)
						return "Наименование не выбрано";
				}
				return "";
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] { "Catalog" };

		public virtual string TrySave(IStatelessSession session)
		{
			if (session.Query<AwaitedItem>()
				.Any(a => a.Catalog == Catalog && (a.Producer == Producer || a.Producer == null))) {
				return "Выбранное наименование уже присутствует в списке ожидаемых позиций";
			}
			foreach (var field in FieldsForValidate) {
				var error = this[field];
				if (!string.IsNullOrEmpty(error))
					return error;
			}
			session.Insert(this);
			return null;
		}
	}
}