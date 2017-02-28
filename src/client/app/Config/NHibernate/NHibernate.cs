using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Reports;
using Common.MySql;
using Common.Tools;
using Devart.Data.MySql;
using log4net;
using NHibernate;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NHibernate.Type;
using Cascade = NHibernate.Mapping.ByCode.Cascade;
using Configuration = NHibernate.Cfg.Configuration;
using Environment = NHibernate.Cfg.Environment;
using Settings = AnalitF.Net.Client.Models.Settings;

namespace AnalitF.Net.Client.Config.NHibernate
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public class MEMORYSTATUSEX
	{
		public uint dwLength;
		public uint dwMemoryLoad;
		public ulong ullTotalPhys;
		public ulong ullAvailPhys;
		public ulong ullTotalPageFile;
		public ulong ullAvailPageFile;
		public ulong ullTotalVirtual;
		public ulong ullAvailVirtual;
		public ulong ullAvailExtendedVirtual;

		public MEMORYSTATUSEX()
		{
			this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
		}
	}

	public class IgnoreAttribute : Attribute
	{}

	public class NHibernate
	{
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

		private static ILog log = LogManager.GetLogger(typeof(NHibernate));
		private List<PropertyInfo> indexes = new List<PropertyInfo>();

		public ISessionFactory Factory;
		public Configuration Configuration;
		public int MappingHash;
		public bool UseRelativePath;

		public void Init(string connectionStringName = "local", bool debug = false)
		{
			//ilmerge
			//если сборки объединены то логика определения системы протоколирование не работает
			//нужно вручную настроить ее
			LoggerProvider.SetLoggersFactory(new Log4NetLoggerFactory());

			var mappingDialect = new MySQL5Dialect();
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
			Index<WaybillLine>(w => w.EAN13);
			Index<Reject>(w => w.ProductId);
			Index<Reject>(w => w.ProducerId);
			Index<Reject>(w => w.Product);
			Index<Reject>(w => w.Series);
			Index<Offer>(o => o.ProductId);
			Index<Offer>(o => o.CatalogId);
			//индекс для восстановления заявок
			Index<Offer>(o => o.ProductSynonymId);
			Index<Offer>(o => o.PriceId);
			Index<Offer>(o => o.Id.RegionId);
			Index<SentOrder>(o => o.SentOn);
			Index<SentOrder>(o => o.ServerId);
			Index<DeletedOrder>(o => o.DeletedOn);
			Index<DeletedOrder>(o => o.ServerId);
			Index<MinCost>(r => r.Diff);
			Index<Catalog>(r => r.Name);
			Index<Drug>(r => r.EAN);
			Index<BarCode>(r => r.Value);

			mapper.Class<Drug>(x => x.Id(y => y.DrugId));
			mapper.Class<Settings>(m => {
				m.Bag(o => o.Markups, c => {
					c.Inverse(true);
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
				});
				m.Bag(o => o.PriceTags, c => {
					c.Inverse(true);
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
				});
				m.Bag(o => o.Waybills, c => c.Cascade(Cascade.DeleteOrphans | Cascade.All));
				m.Property(x => x.ClientTokenV2, c => c.Length(10000));
			});
			mapper.Class<MarkupConfig>(m => {
				m.Property(o => o.Begin, om => om.Access(Accessor.Field));
				m.Property(o => o.End, om => om.Access(Accessor.Field));
			});
			mapper.Class<PriceTagSettings>(o => {
				o.Id(r => r.Id);
			});
			mapper.Class<PriceTag>(m => {
				m.Bag(o => o.Items, c => {
					c.Inverse(true);
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
				});
			});
			mapper.Class<PriceTagItem>(o => {
				o.Id(r => r.Id);
			});
			mapper.Class<MinOrderSumRule>(m => {
				m.ComposedId(c => {
					c.ManyToOne(p => p.Address);
					c.ManyToOne(p => p.Price, t => t.Columns(cm => cm.Name("PriceId"), cm => cm.Name("RegionId")));
				});
				m.Property(p => p.MinOrderSum);
			});
			mapper.Class<Limit>(m => {
				m.ComposedId(c => {
					c.ManyToOne(p => p.Address);
					c.ManyToOne(p => p.Price, t => t.Columns(cm => cm.Name("PriceId"), cm => cm.Name("RegionId")));
				});
				m.Property(p => p.Value);
			});
			mapper.Class<WaybillOrder>(m => {
				m.ComposedId(c => {
					c.Property(p => p.OrderLineId);
					c.Property(p => p.DocumentLineId);
				});
			});

			mapper.Class<Promotion>(m => {
				m.Bag(o => o.Catalogs, c => {
					c.Table("PromotionCatalogs");
					c.Key(km => km.Column("PromotionId"));
				}, cm => {
					cm.ManyToMany(km => km.Column("CatalogId"));
				});
			});

			mapper.Class<ProducerPromotion>(m => {
				m.Bag(o => o.Catalogs, c => {
					c.Table("ProducerPromotionCatalogs");
					c.Key(km => km.Column("PromotionId"));
				}, cm => {
					cm.ManyToMany(km => km.Column("CatalogId"));
				});
			});

			mapper.Class<ProducerPromotion>(m => {
				m.Bag(o => o.Suppliers, c => {
					c.Table("ProducerPromotionSuppliers");
					c.Key(km => km.Column("PromotionId"));
				}, cm => {
					cm.ManyToMany(km => km.Column("SupplierId"));
				});
			});

			mapper.Class<Price>(m => {
				m.ComponentAsId(c => c.Id);
				m.Property(p => p.ContactInfo, c => c.Length(10000));
				m.Property(p => p.OperativeInfo, c => c.Length(10000));
				m.Property(p => p.RegionId, c => c.Insert(false));
				m.Version(p => p.Timestamp, c => {
					c.Type(new TimestampType());
					c.Column(cc => cc.Default("'0001-01-01 00:00:00'"));
				});
			});
			mapper.Class<Check>(m => {
				m.Property(x => x.ServerId, p => p.UniqueKey("ServerIdUniq"));
				m.Version(p => p.Timestamp, c => {
					c.Type(new TimestampType());
					c.Column(cc => cc.Default("'0001-01-01 00:00:00'"));
				});
			});

			mapper.Class<Mail>(m => {
				m.Property(p => p.Subject, c => c.Length(10000));
				m.Property(p => p.Body, c => c.Length(10000));
			});

			mapper.Class<Order>(m => {
				m.Property(o => o.Frozen, om => om.Access(Accessor.Field));
				m.ManyToOne(o => o.MinOrderSum, c => {
					c.Columns(cm => cm.Name("AddressId"), cm => cm.Name("PriceId"), cm => cm.Name("RegionId"));
					c.Insert(false);
					c.Update(false);
				});
				m.ManyToOne(o => o.Limit, c => {
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
				//при миграции могут если поставщик отсутствует nhibernate будет перезаписывать
				m.ManyToOne(x => x.Address, x => x.Update(false));
				m.ManyToOne(x => x.Supplier, x => x.Update(false));
				m.Bag(o => o.Lines, c => {
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
					c.Inverse(true);
				});
			});
			mapper.Class<WaybillLine>(m => {
				m.Property(l => l.RetailCost, p => p.Access(Accessor.Field));
				m.Property(l => l.RetailMarkup, p => p.Access(Accessor.Field));
				m.Property(l => l.RealRetailMarkup, p => p.Access(Accessor.Field));
				m.Property(l => l.MaxRetailMarkup, p => p.Access(Accessor.Field));
				m.Bag(l => l.CertificateFiles, c => {
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
				});
			});
			mapper.Class<Address>(m => m.Bag(o => o.Orders, c => {
				c.Cascade(Cascade.All | Cascade.DeleteOrphans);
				c.Inverse(true);
			}));
			mapper.Class<InventoryDoc>(m => {
				m.Property(x => x.ServerId, p => p.UniqueKey("ServerIdUniq"));
				m.Bag(o => o.Lines, c => {
					c.Cascade(Cascade.All | Cascade.DeleteOrphans);
				});
			});
			mapper.Class<UnpackingDoc>(m => {
				m.Property(x => x.ServerId, p => p.UniqueKey("ServerIdUniq"));
				m.Bag(o => o.Lines, c => {
					c.Cascade(Cascade.All | Cascade.DeleteOrphans);
				});
			});
			mapper.Class<UnpackingLine>(m => {
				m.ManyToOne(x => x.DstStock, p => p.Cascade(Cascade.All));
				m.ManyToOne(x => x.SrcStock, p => p.Cascade(Cascade.All));
			});

			mapper.Class<WriteoffDoc>(m => {
				m.Property(x => x.ServerId, p => p.UniqueKey("ServerIdUniq"));
				m.Bag(o => o.Lines, c => {
					c.Cascade(Cascade.All | Cascade.DeleteOrphans);
				});
			});
			mapper.Class<ReturnDoc>(m => {
				m.Property(x => x.ServerId, p => p.UniqueKey("ServerIdUniq"));
				m.Bag(o => o.Lines, c => {
					c.Cascade(Cascade.All | Cascade.DeleteOrphans);
				});
			});
			mapper.Class<DisplacementDoc>(m => {
				m.Property(x => x.ServerId, p => p.UniqueKey("ServerIdUniq"));
				m.Bag(o => o.Lines, c => {
					c.Cascade(Cascade.All | Cascade.DeleteOrphans);
				});
			});
			mapper.Class<ReassessmentDoc>(m => {
				m.Property(x => x.ServerId, p => p.UniqueKey("ServerIdUniq"));
				m.Bag(o => o.Lines, c => {
					c.Cascade(Cascade.All | Cascade.DeleteOrphans);
				});
			});
			mapper.Class<ReassessmentLine>(m => m.ManyToOne(x => x.DstStock, p => p.Cascade(Cascade.All)));

			mapper.Class<Offer>(m => {
				m.Property(l => l.RetailMarkup, p => p.Access(Accessor.Field));
				m.Property(l => l.RetailPrice, p => p.Access(Accessor.Field));
				m.ManyToOne(o => o.Price, c => {
					c.Insert(false);
					c.Update(false);
				});
				m.ManyToOne(o => o.LeaderPrice,
					c => c.Columns(cm => cm.Name("LeaderPriceId"),
					cm => cm.Name("LeaderRegionId")));
			});
			mapper.Class<OrderLine>(m => {
				m.Property(l => l.RetailMarkup, p => p.Access(Accessor.Field));
				m.Property(l => l.RetailPrice, p => p.Access(Accessor.Field));
			});
			mapper.Class<SentOrder>(m => {
				m.Bag(o => o.Lines, c => {
					c.Key(k => k.Column("OrderId"));
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
					c.Inverse(true);
				});
			});
			mapper.Class<SentOrderLine>(m => {
				m.Property(l => l.RetailMarkup, p => p.Access(Accessor.Field));
				m.Property(l => l.RetailPrice, p => p.Access(Accessor.Field));
			});
			mapper.Class<DeletedOrder>(m => {
				m.Bag(o => o.Lines, c => {
					c.Key(k => k.Column("OrderId"));
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
					c.Inverse(true);
				});
			});
			mapper.Class<DeletedOrderLine>(m => {
				m.Property(l => l.RetailMarkup, p => p.Access(Accessor.Field));
				m.Property(l => l.RetailPrice, p => p.Access(Accessor.Field));
			});
			mapper.Class<Mail>(m => {
				m.Bag(o => o.Attachments, c => {
					c.Cascade(Cascade.DeleteOrphans | Cascade.All);
				});
			});
			mapper.Class<BatchLine>(m => {
				m.Property(l => l.Comment, c => c.Length(10000));
				m.Property(l => l.ServiceFields, c => c.Length(10000));
			});
			mapper.Class<AwaitedItem>(i => {
				i.ManyToOne(l => l.Catalog, c => c.Index("Catalog"));
				i.ManyToOne(l => l.Producer, c => c.Index("Producer"));
			});

			mapper.Class<Stock>(m => {
				m.Property(x => x.ServerId, p => p.UniqueKey("ServerIdUniq"));
				m.Property(x => x.RetailCost, p => p.Access(Accessor.Field));
				m.Property(x => x.RetailMarkup, p => p.Access(Accessor.Field));
				m.ManyToOne(x => x.Catalog, c => c.Index("Catalog"));
			});
			mapper.Class<StockAction>(m => {
				m.Version(p => p.Timestamp, c => {
					c.Type(new TimestampType());
					c.Column(cc => cc.Default("'0001-01-01 00:00:00'"));
				});
			});

			mapper.BeforeMapClass += (inspector, type, customizer) => {
				customizer.Id(m => m.Generator(Generators.Native));
				if (type == typeof(RegulatorRegistry)) {
					customizer.Table("RegulatorRegistry");
				}
			};
			mapper.BeforeMapProperty += (inspector, member, customizer) => {
				var propertyInfo = ((PropertyInfo)member.LocalMember);
				var propertyType = propertyInfo.PropertyType;
				if (member.GetContainerEntity(inspector) == typeof(ProductDescription)) {
					if (propertyType == typeof(string)) {
						customizer.Length(10000);
					}
				}

				if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?)) {
					customizer.Type<UtcToLocalDateTimeType>();
				}

				if (propertyType.IsValueType && !propertyType.IsNullable()) {
					customizer.Column(c => c.Default(GetDefaultValue(propertyInfo, mappingDialect)));
					customizer.NotNullable(true);
				}

				if (indexes.Any(m => m.MetadataToken == propertyInfo.MetadataToken && m.Module == propertyInfo.Module))
					customizer.Index(propertyInfo.Name);
			};
			mapper.BeforeMapManyToMany += (inspector, member, customizer) => {
				//myisam не поддерживает внешние ключи
				customizer.ForeignKey("none");
			};
			mapper.BeforeMapBag += (inspector, member, customizer) => {
				customizer.Key(k => {
					k.Column(member.GetContainerEntity(inspector).Name + "Id");
					//myisam не поддерживает внешние ключи
					k.ForeignKey("none");
				});
			};
			mapper.BeforeMapManyToOne += (inspector, member, customizer) => {
				var propertyInfo = ((PropertyInfo)member.LocalMember);
				if (propertyInfo.PropertyType == typeof(Price)) {
					customizer.Columns(cm => cm.Name("PriceId"), cm => cm.Name("RegionId"));
				}
				else {
					customizer.Column(member.LocalMember.Name + "Id");
					if (indexes.Contains(propertyInfo)){
						customizer.Column(m => m.Index(member.LocalMember.Name + "Id"));
					}
				}
				customizer.NotFound(NotFoundMode.Ignore);
				//myisam не поддерживает внешние ключи
				customizer.ForeignKey("none");
			};
			var assembly = typeof(Offer).Assembly;
			var types = assembly.GetTypes()
				.Where(t => t.Namespace != null && t.Namespace.StartsWith("AnalitF.Net.Client.Models"))
				.Where(t => !t.IsAbstract && !t.IsInterface && t.GetProperty("Id") != null
					|| t == typeof(MinOrderSumRule)
					|| t == typeof(WaybillOrder)
					|| t == typeof(Limit)
					|| t == typeof(Drug));
			var mapping = mapper.CompileMappingFor(types);

			PatchComponentColumnName(mapping);

			MappingHash = mapping.AsString().GetHashCode();

			if (debug) {
				Console.WriteLine("MappingHash = {0}", MappingHash);
				Console.WriteLine(mapping.AsString());
			}

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
#if DEBUG
				//если нужно отладить запросы хибера
				//для запросов в AddAwaited падает
				//{Environment.FormatSql, "true"},
#endif
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

		private static object GetDefaultValue(PropertyInfo propertyInfo, Dialect dialect)
		{
			var instance = Activator.CreateInstance(propertyInfo.ReflectedType);
			var defaultValue = propertyInfo.GetValue(instance, null);
			if (defaultValue is bool)
				return Convert.ToInt32(defaultValue);
			if (defaultValue is Enum)
				return Convert.ToInt32(defaultValue);
			if (defaultValue is DateTime)
				return "'" + ((DateTime)defaultValue).ToString(MySqlConsts.MySQLDateFormat) + "'";
			if (defaultValue is TimeSpan)
				return new TimeSpanType().ObjectToSQLString(defaultValue, dialect);
			if (Util.IsNumeric(defaultValue))
				return ((IFormattable)defaultValue).ToString(null, CultureInfo.InvariantCulture);
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

			var status = new MEMORYSTATUSEX();
			if (GlobalMemoryStatusEx(status)) {
				ulong min = 32*1024*1024;
				//нет смысла просить больше тк в 32 битном процессе всего скорее не сможем выделить блок больше из-за
				//фрагментации
				ulong max = 500*1024*1024;
				var size = Math.Min(Math.Max(min, status.ullAvailVirtual / 3), max);
				dictionary.Add("--max_heap_table_size", size.ToString());
				dictionary.Add("--tmp_table_size", size.ToString());
			}

			builder.ServerParameters = dictionary.Select(k => k.Key + "=" + k.Value).Implode(";");
			log.DebugFormat("Строка подключения {0}", builder);
			return builder.ToString();
		}
	}
}