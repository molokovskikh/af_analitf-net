using System;
using System.ComponentModel;
using System.Reflection;
using NHibernate;
using NHibernate.Bytecode;
using NHibernate.Engine;
using NHibernate.Proxy;
using NHibernate.Proxy.DynamicProxy;
using NHibernate.Type;
using IInterceptor = NHibernate.Proxy.DynamicProxy.IInterceptor;

namespace AnalitF.Net.Client.Config.NHibernate
{
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

	public class LazyInitializer : DefaultLazyInitializer,  IInterceptor
	{
		private event PropertyChangedEventHandler PropertyChanged;
		private object proxy;
		private bool isRegistred;

		public LazyInitializer(string entityName, Type persistentClass, object id, MethodInfo getIdentifierMethod, MethodInfo setIdentifierMethod, IAbstractComponentType componentIdType, ISessionImplementor session)
			: base(entityName, persistentClass, id, getIdentifierMethod, setIdentifierMethod, componentIdType, session)
		{
		}

		public override void Initialize()
		{
			base.Initialize();

			if (!isRegistred) {
				isRegistred = true;
				var changed = Target as INotifyPropertyChanged;
				if (changed != null)
					changed.PropertyChanged += PatchTarget;
			}
		}

		public new object Intercept(InvocationInfo info)
		{
			if (info.TargetMethod.Name.Contains("PropertyChanged")) {
				if (proxy == null)
					proxy = info.Target;
				var propertyChangedEventHandler = (PropertyChangedEventHandler)info.Arguments[0];
				if (info.TargetMethod.Name.StartsWith("add_"))
					PropertyChanged += propertyChangedEventHandler;
				else
					PropertyChanged -= propertyChangedEventHandler;
				return null;
			}
			else {
				return base.Intercept(info);
			}
		}

		public void PatchTarget(object sender, PropertyChangedEventArgs args)
		{
			if (PropertyChanged != null && proxy != null)
				PropertyChanged(proxy, args);
		}
	}
}