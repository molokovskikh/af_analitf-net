using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnalitF.Net.Client.Models;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;

namespace AnalitF.Net.Client.Config.Initializers
{
	public class NHibernate
	{
		public static ISessionFactory Factory;
		public static Configuration Configuration;

		public void Init(string connectionStringName = "local")
		{
			var mapper = new ConventionModelMapper();
			mapper.BeforeMapBag += (inspector, member, customizer) => customizer.Key(k => k.Column(member.GetContainerEntity(inspector).Name + "Id"));
			mapper.BeforeMapManyToOne += (inspector, member, customizer) => customizer.Column(member.LocalMember.Name + "Id");
			var assembly = typeof(CatalogForm).Assembly;
			var types = assembly.GetTypes().Where(t => t.GetProperty("Id") != null);
			var mapping = mapper.CompileMappingFor(types);

			Configuration = new Configuration();
			Configuration.AddProperties(new Dictionary<string, string> {
				{Environment.Dialect, "NHibernate.Dialect.MySQL5Dialect"},
				{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
				{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
				{Environment.ConnectionStringName, connectionStringName},
				{Environment.Hbm2ddlKeyWords, "none"},
				//раскомментировать если нужно отладить запросы хибера
				//{Environment.ShowSql, "true"},
				{Environment.Isolation, "ReadCommitted"},
			});
			Configuration.SetNamingStrategy(new PluralizeNamingStrategy());
			Configuration.AddDeserializedMapping(mapping, assembly.GetName().Name);

			Factory = Configuration.BuildSessionFactory();
		}
	}
}