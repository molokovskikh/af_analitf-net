using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Tasks
{
	public class Debug
	{
		[Description("Выводит маппинг nhibernate")]
		public void Mapping()
		{
			var init = new Config.Initializers.NHibernate();
			init.Init(debug: true);
		}

		[Description("Выводит схему данных")]
		public void Schema()
		{
			var init = new Config.Initializers.NHibernate();
			init.Init();
			var export = new SchemaExport(init.Configuration);
			export.Create(true, false);
		}
	}
}