using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnalitF.Net.Client.Models;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using Environment = NHibernate.Cfg.Environment;

namespace AnalitF.Net.Client.Config.Initializers
{
	public class IgnoreAttribute : Attribute
	{}

	public class NHibernate
	{
		public ISessionFactory Factory;
		public Configuration Configuration;

		public void Init(string connectionStringName = "local", bool debug = false)
		{
			var mapper = new ConventionModelMapper();
			var basInspector = new SimpleModelInspector();
			var simpleModelInspector = ((SimpleModelInspector)mapper.ModelInspector);
			simpleModelInspector.IsPersistentProperty((m, declared) => {
				return ((IModelInspector)basInspector).IsPersistentProperty(m)
					&& m.GetCustomAttributes(typeof(IgnoreAttribute), false).Length == 0;
			});
			simpleModelInspector.IsRootEntity((type, declared) => {
				var modelInspector = ((IModelInspector)simpleModelInspector);
				return declared || (type.IsClass
					//если наследуемся от класса который не маплен то это простое наследование
					&& (typeof(object) == type.BaseType || !modelInspector.IsEntity(type.BaseType)))
					&& modelInspector.IsEntity(type);
			});

			mapper.Class<Order>(m => m.Bag(o => o.Lines, c => c.Cascade(Cascade.DeleteOrphans | Cascade.All)));
			mapper.Class<SentOrder>(m => m.Bag(o => o.Lines, c => c.Cascade(Cascade.DeleteOrphans | Cascade.All)));

			mapper.AfterMapClass += (inspector, type, customizer) => {
				customizer.Id(m => m.Generator(Generators.Native));
			};
			mapper.BeforeMapProperty += (inspector, member, customizer) => {
				if (member.GetContainerEntity(inspector) == typeof(ProductDescription)) {
					if (((PropertyInfo)member.LocalMember).PropertyType == typeof(string)) {
						customizer.Length(10000);
					}
				}
			};
			mapper.BeforeMapBag += (inspector, member, customizer) => customizer.Key(k => k.Column(member.GetContainerEntity(inspector).Name + "Id"));
			mapper.BeforeMapManyToOne += (inspector, member, customizer) => customizer.Column(member.LocalMember.Name + "Id");
			var assembly = typeof(Offer).Assembly;
			var types = assembly.GetTypes().Where(t => t.GetProperty("Id") != null);
			var mapping = mapper.CompileMappingFor(types);
			if (debug)
				Console.WriteLine(mapping.AsString());

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