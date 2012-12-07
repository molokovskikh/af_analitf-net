using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using AnalitF.Net.Client.Models;
using Common.Tools;
using Devart.Data.MySql;
using NHibernate;
using NHibernate.Bytecode;
using NHibernate.Engine;
using NHibernate.Mapping.ByCode;
using NHibernate.Proxy;
using NHibernate.Proxy.DynamicProxy;
using NHibernate.Type;
using Cascade = NHibernate.Mapping.ByCode.Cascade;
using Configuration = NHibernate.Cfg.Configuration;
using Environment = NHibernate.Cfg.Environment;
using IInterceptor = NHibernate.IInterceptor;

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

			mapper.Class<Price>(m => {
				m.Property(p => p.ContactInfo, c => c.Length(10000));
				m.Property(p => p.OperativeInfo, c => c.Length(10000));
			});
			mapper.Class<Order>(m => m.Bag(o => o.Lines, c => {
				c.Cascade(Cascade.DeleteOrphans | Cascade.All);
				c.Inverse(true);
			}));
			mapper.Class<SentOrder>(m => m.Bag(o => o.Lines, c => {
				c.Cascade(Cascade.DeleteOrphans | Cascade.All);
				c.Inverse(true);
			}));

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

			var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
			var driver = "NHibernate.Driver.MySqlDataDriver";
			var dialect = typeof(DevartMySqlDialect).AssemblyQualifiedName;

			if (connectionString.Contains("Embedded=True")) {
				connectionString = FixRelativePaths(connectionString);
				driver = typeof(DevArtDriver).AssemblyQualifiedName;
			}

			Configuration = new Configuration();
			Configuration.AddProperties(new Dictionary<string, string> {
				{Environment.Dialect, dialect},
				{Environment.ConnectionDriver, driver},
				{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
				{Environment.ConnectionString, connectionString},
				{Environment.Hbm2ddlKeyWords, "none"},
				//раскомментировать если нужно отладить запросы хибера
				//{Environment.ShowSql, "true"},
				{Environment.ProxyFactoryFactoryClass, typeof(ProxyFactoryFactory).AssemblyQualifiedName},
				{Environment.ReleaseConnections, "on_close"}
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
			return builder.ToString();
		}
	}

	public class LazyInitializer : DefaultLazyInitializer,  global::NHibernate.Proxy.DynamicProxy.IInterceptor
	{
		public virtual event PropertyChangedEventHandler PropertyChanged;

		public LazyInitializer(string entityName, Type persistentClass, object id, MethodInfo getIdentifierMethod, MethodInfo setIdentifierMethod, IAbstractComponentType componentIdType, ISessionImplementor session)
			: base(entityName, persistentClass, id, getIdentifierMethod, setIdentifierMethod, componentIdType, session)
		{
		}

		public new object Intercept(InvocationInfo info)
		{
			if (info.TargetMethod.Name.Contains("PropertyChanged")) {
				var propertyChangedEventHandler = (PropertyChangedEventHandler)info.Arguments[0];
				if (info.TargetMethod.Name.StartsWith("add_"))
					PropertyChanged += propertyChangedEventHandler;
				else
					PropertyChanged -= propertyChangedEventHandler;
			}
			var result = base.Intercept(info);

			if (info.TargetMethod.Name.StartsWith("set_") && PropertyChanged != null)
				PropertyChanged(info.Target, new PropertyChangedEventArgs(info.TargetMethod.Name.Substring(4)));

			return result;
		}
	}

	public class ProxyFactoryFactory : DefaultProxyFactoryFactory, IProxyFactoryFactory
	{
		public new IProxyFactory BuildProxyFactory()
		{
			return new ProxyFactory();
		}
	}

	public class ProxyFactory : DefaultProxyFactory, IProxyFactory
	{
		private readonly global::NHibernate.Proxy.DynamicProxy.ProxyFactory factory
			= new global::NHibernate.Proxy.DynamicProxy.ProxyFactory();

		public new INHibernateProxy GetProxy(object id, ISessionImplementor session)
		{
			if (!IsClassProxy || !typeof(INotifyPropertyChanged).IsAssignableFrom(PersistentClass))
				return base.GetProxy(id, session);

			try {
				var initializer = new LazyInitializer(EntityName, PersistentClass, id, GetIdentifierMethod, SetIdentifierMethod, ComponentIdType, session);
				return (INHibernateProxy)factory.CreateProxy(PersistentClass, initializer, Interfaces);
			}
			catch (Exception ex) {
				log.Error("Creating a proxy instance failed", ex);
				throw new HibernateException("Creating a proxy instance failed", ex);
			}
		}
	}
}