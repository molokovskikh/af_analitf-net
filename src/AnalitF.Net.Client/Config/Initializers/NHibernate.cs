using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using AnalitF.Net.Client.Models;
using Common.Tools;
using Devart.Data.MySql;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Type;
using log4net;
using Cascade = NHibernate.Mapping.ByCode.Cascade;
using Configuration = NHibernate.Cfg.Configuration;
using Environment = NHibernate.Cfg.Environment;

namespace AnalitF.Net.Client.Config.Initializers
{
	public class IgnoreAttribute : Attribute
	{}

	public class NHibernate
	{
		private static ILog log = LogManager.GetLogger(typeof(NHibernate));

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
				return declared || (type.IsClass && type.BaseType != null
					//если наследуемся от класса который не маплен то это простое наследование
					&& (typeof(object) == type.BaseType || !modelInspector.IsEntity(type.BaseType)) || type.BaseType == typeof(BaseStatelessObject))
					&& modelInspector.IsEntity(type);
			});

			mapper.Class<MinOrderSumRule>(m => {
				m.ComposedId(c => {
					c.ManyToOne(p => p.Address);
					c.ManyToOne(p => p.Price, t => t.Columns(cm => cm.Name("PriceId"), cm => cm.Name("RegionId")));
				});
				m.Property(p => p.MinOrderSum);
			});
			mapper.Class<Price>(m => {
				m.ComponentAsId(c => c.Id);
				m.Property(p => p.ContactInfo, c => c.Length(10000));
				m.Property(p => p.OperativeInfo, c => c.Length(10000));
				m.Version(p => p.Timestamp, c => {
					c.Type(new TimestampType());
					c.Column(cc => cc.Default("'0001-01-01 00:00:00'"));
				});
			});
			mapper.Class<Order>(m => {
				m.ManyToOne(o => o.Price, c => c.Columns(cm => cm.Name("PriceId"), cm => cm.Name("RegionId")));
				m.ManyToOne(o => o.MinOrderSum, c => {
					c.Columns(cm => cm.Name("PriceId"), cm => cm.Name("AddressId"), cm => cm.Name("RegionId"));
					c.Insert(false);
					c.Update(false);
				});
				m.Bag(o => o.Lines, c => {
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
					c.Inverse(true);
				});
			});
			mapper.Class<Waybill>(m => {
				m.Bag(o => o.Lines, c => {
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
					c.Inverse(true);
				});
			});
			mapper.Class<Address>(m => m.Bag(o => o.Orders, c => {
				c.Cascade(Cascade.All | Cascade.DeleteOrphans);
				c.Inverse(true);
			}));
			mapper.Class<Offer>(m => {
				m.ManyToOne(o => o.Price, c => {
					c.Columns(cm => cm.Name("PriceId"), cm => cm.Name("RegionId"));
					c.Insert(false);
					c.Update(false);
				});
				m.ManyToOne(o => o.LeaderPrice, c => c.Columns(cm => cm.Name("LeaderPriceId"), cm => cm.Name("LeaderRegionId")));
			});
			mapper.Class<SentOrder>(m => {
				m.ManyToOne(o => o.Price, c => c.Columns(cm => cm.Name("PriceId"), cm => cm.Name("RegionId")));
				m.Bag(o => o.Lines, c => {
					c.Key(k => k.Column("OrderId"));
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
					c.Inverse(true);
				});
			});

			mapper.AfterMapClass += (inspector, type, customizer) => {
				customizer.Id(m => m.Generator(Generators.Native));
			};
			mapper.BeforeMapProperty += (inspector, member, customizer) => {
				var propertyType = ((PropertyInfo)member.LocalMember).PropertyType;
				if (member.GetContainerEntity(inspector) == typeof(ProductDescription)) {
					if (propertyType == typeof(string)) {
						customizer.Length(10000);
					}
				}

				if (propertyType == typeof(DateTime)) {
					customizer.Type<UtcToLocalDateTimeType>();
				}

				if (propertyType.IsValueType && !propertyType.IsNullable()) {
					customizer.NotNullable(true);
				}
			};
			mapper.BeforeMapBag += (inspector, member, customizer) => {
				customizer.Key(k => k.Column(member.GetContainerEntity(inspector).Name + "Id"));
			};
			mapper.BeforeMapManyToOne += (inspector, member, customizer) => {
				customizer.Column(member.LocalMember.Name + "Id");
				customizer.NotFound(NotFoundMode.Ignore);
			};
			var assembly = typeof(Offer).Assembly;
			var types = assembly.GetTypes().Where(t => !t.IsAbstract && t.GetProperty("Id") != null || t == typeof(MinOrderSumRule));
			var mapping = mapper.CompileMappingFor(types);
			if (debug)
				Console.WriteLine(mapping.AsString());

			var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
			var driver = "NHibernate.Driver.MySqlDataDriver";
			var dialect = typeof(DevartMySqlDialect).AssemblyQualifiedName;

			if (connectionString.Contains("Embedded=True")) {
				connectionString = FixRelativePaths(connectionString);
				driver = typeof(DevartDriver).AssemblyQualifiedName;
			}

			Configuration = new Configuration();
			Configuration.AddProperties(new Dictionary<string, string> {
				{Environment.Dialect, dialect},
				{Environment.ConnectionDriver, driver},
				{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
				{Environment.ConnectionString, connectionString},
				{Environment.Hbm2ddlKeyWords, "none"},
				{Environment.FormatSql, "true"},
				//раскомментировать если нужно отладить запросы хибера
				//{Environment.ShowSql, "true"},
				{Environment.ProxyFactoryFactoryClass, typeof(ProxyFactoryFactory).AssemblyQualifiedName},
			});
			Configuration.SetNamingStrategy(new PluralizeNamingStrategy());
			Configuration.AddDeserializedMapping(mapping, assembly.GetName().Name);
			Factory = Configuration.BuildSessionFactory();
		}

		public static string FixRelativePaths(string connectionString)
		{
			var builder = new MySqlConnectionStringBuilder(connectionString);
			var parameters = builder.ServerParameters;
			if (String.IsNullOrEmpty(parameters))
				return connectionString;
			var dictionary = parameters
				.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('='))
				.ToDictionary(p => p[0], p => p[1]);

			var dirKeys = dictionary.Keys.Where(k => k.EndsWith("dir")).ToArray();
			foreach (var key in dirKeys) {
				var value = dictionary[key];
				dictionary[key] = Path.GetFullPath(FileHelper.MakeRooted(value)).Replace("\\", "/");
			}

			builder.ServerParameters = dictionary.Select(k => k.Key + "=" + k.Value).Implode(";");
			log.DebugFormat("Строка подключения {0}", builder);
			return builder.ToString();
		}
	}
}