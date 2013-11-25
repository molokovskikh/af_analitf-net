using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AnalitF.Net.Client.Models;
using Common.MySql;
using Common.Tools;
using Devart.Data.MySql;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Proxy;
using NHibernate.Type;
using log4net;
using NHibernate.Cfg.MappingSchema;
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
		private List<PropertyInfo> indexes = new List<PropertyInfo>();

		public ISessionFactory Factory;
		public Configuration Configuration;
		public int MappingHash;
		public bool UseRelativePath;

		public void Init(string connectionStringName = "local", bool debug = false)
		{
			var mapper = new ConventionModelMapper();
			var baseInspector = new SimpleModelInspector();
			var simpleModelInspector = ((SimpleModelInspector)mapper.ModelInspector);
			simpleModelInspector.IsPersistentProperty((m, declared) => {
				return ((IModelInspector)baseInspector).IsPersistentProperty(m)
					&& m.GetCustomAttributes(typeof(IgnoreAttribute), false).Length == 0;
			});
			simpleModelInspector.IsRootEntity((type, declared) => {
				var modelInspector = ((IModelInspector)simpleModelInspector);
				return declared || (type.IsClass && type.BaseType != null
					//если наследуемся от класса который не маплен то это простое наследование
					&& (typeof(object) == type.BaseType || !modelInspector.IsEntity(type.BaseType))
						|| type.BaseType == typeof(BaseStatelessObject))
					&& modelInspector.IsEntity(type);
			});

			Index<Waybill>(w => w.WriteTime);
			Index<WaybillLine>(w => w.ProductId);
			Index<WaybillLine>(w => w.ProducerId);
			Index<WaybillLine>(w => w.Product);
			Index<WaybillLine>(w => w.SerialNumber);
			Index<WaybillLine>(w => w.RejectId);
			Index<Reject>(w => w.ProductId);
			Index<Reject>(w => w.ProducerId);
			Index<Reject>(w => w.Product);
			Index<Reject>(w => w.Series);
			Index<Offer>(o => o.ProductId);
			Index<Offer>(o => o.CatalogId);

			mapper.Class<Settings>(m => {
				m.Bag(o => o.Markups, c => {
					c.Inverse(true);
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
				});
				m.Bag(o => o.Waybills, c => c.Cascade(Cascade.DeleteOrphans | Cascade.All));
			});
			mapper.Class<MarkupConfig>(m => {
				m.Property(o => o.Begin, om => om.Access(Accessor.Field));
				m.Property(o => o.End, om => om.Access(Accessor.Field));
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
				m.Property(o => o.Frozen, om => om.Access(Accessor.Field));
				m.ManyToOne(o => o.Price, c => c.Columns(cm => cm.Name("PriceId"), cm => cm.Name("RegionId")));
				m.ManyToOne(o => o.MinOrderSum, c => {
					c.Columns(cm => cm.Name("AddressId"), cm => cm.Name("PriceId"), cm => cm.Name("RegionId"));
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
			mapper.Class<WaybillLine>(m => {
				m.Property(l => l.RetailCost, p => p.Access(Accessor.Field));
				m.Property(l => l.RetailMarkup, p => p.Access(Accessor.Field));
				m.Property(l => l.RealRetailMarkup, p => p.Access(Accessor.Field));
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
				m.ManyToOne(o => o.LeaderPrice,
					c => c.Columns(cm => cm.Name("LeaderPriceId"),
					cm => cm.Name("LeaderRegionId")));
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
				var propertyInfo = ((PropertyInfo)member.LocalMember);
				var propertyType = propertyInfo.PropertyType;
				if (member.GetContainerEntity(inspector) == typeof(ProductDescription)) {
					if (propertyType == typeof(string)) {
						customizer.Length(10000);
					}
				}

				if (propertyType == typeof(DateTime)) {
					customizer.Type<UtcToLocalDateTimeType>();
				}

				if (propertyType.IsValueType && !propertyType.IsNullable()) {
					customizer.Column(c => c.Default(GetDefaultValue(propertyInfo)));
					customizer.NotNullable(true);
				}

				if (indexes.Contains(propertyInfo))
					customizer.Index(propertyInfo.Name);
			};
			mapper.BeforeMapBag += (inspector, member, customizer) => {
				customizer.Key(k => k.Column(member.GetContainerEntity(inspector).Name + "Id"));
			};
			mapper.BeforeMapManyToOne += (inspector, member, customizer) => {
				customizer.Column(member.LocalMember.Name + "Id");
				customizer.NotFound(NotFoundMode.Ignore);
			};
			var assembly = typeof(Offer).Assembly;
			var types = assembly.GetTypes().Where(t => !t.IsAbstract && t.GetProperty("Id") != null
				|| t == typeof(MinOrderSumRule));
			var mapping = mapper.CompileMappingFor(types);

			PatchComponentColumnName(mapping);

			MappingHash = mapping.AsString().GetHashCode();

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

		private void Index<T>(Expression<Func<T, object>> expr)
		{
			var info = expr.GetPropertyInfo();
			if (info != null)
				indexes.Add(info);
		}

		private static void PatchComponentColumnName(HbmMapping mapping)
		{
			var components = mapping.RootClasses.SelectMany(c => c.Properties).OfType<HbmComponent>()
				.Where(c => c.Name != "OfferId");
			foreach (var component in components) {
				var columns = component.Properties.OfType<HbmProperty>();
				foreach (var property in columns) {
					if (!property.Columns.Any())
						property.column = component.Name + property.name;
					else
						property.Columns.Each(c => c.name = component.Name + c.name);
				}
			}
		}

		private static object GetDefaultValue(PropertyInfo propertyInfo)
		{
			var instance = Activator.CreateInstance(propertyInfo.DeclaringType);
			var defaultValue = propertyInfo.GetValue(instance, null);
			if (defaultValue is bool)
				return Convert.ToInt32(defaultValue);
			if (defaultValue is Enum)
				return Convert.ToInt32(defaultValue);
			if (defaultValue is int || defaultValue is uint || defaultValue is UInt64)
				return defaultValue.ToString();
			if (defaultValue is DateTime)
				return "'" + ((DateTime)defaultValue).ToString(MySqlConsts.MySQLDateFormat) + "'";
			if (defaultValue is decimal)
				return ((decimal)defaultValue).ToString(CultureInfo.InvariantCulture);
			throw new Exception(propertyInfo.PropertyType.ToString());
		}

		public string FixRelativePaths(string connectionString)
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
				string path;
				if (UseRelativePath)
					path = Path.GetFullPath(value);
				else
					path = Path.GetFullPath(FileHelper.MakeRooted(value));
				dictionary[key] = path.Replace("\\", "/");
			}

			builder.ServerParameters = dictionary.Select(k => k.Key + "=" + k.Value).Implode(";");
			log.DebugFormat("Строка подключения {0}", builder);
			return builder.ToString();
		}
	}
}