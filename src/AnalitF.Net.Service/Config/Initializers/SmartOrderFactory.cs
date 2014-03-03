using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Common.Models;
using Common.Models.Repositories;
using Common.MySql;
using NHibernate.Mapping.Attributes;
using SmartOrderFactory;
using SmartOrderFactory.Domain;
using SmartOrderFactory.Repositories;
using With = Common.MySql.With;

namespace AnalitF.Net.Service.Config.Initializers
{
	public class SmartOrderFactory
	{
		public void Init(NHibernate nhibernate)
		{
			With.DefaultConnectionStringName = "local";
			IoC.Initialize(new WindsorContainer()
				.Register(
					Component.For<ISessionFactoryHolder>().Instance(new SessionFactoryHolder(nhibernate.Factory)),
					Component.For<RepositoryInterceptor>(),
					Component.For(typeof(IRepository<>)).ImplementedBy(typeof(Repository<>)),
					Component.For<IOrderFactoryRepository>().ImplementedBy<OrderFactoryRepository>(),
					Component.For<IOfferRepository>().ImplementedBy<OfferRepository>(),
					Component.For<ISmartOrderFactoryRepository>().ImplementedBy<SmartOrderFactoryRepository>(),
					Component.For<ISmartOfferRepository>().ImplementedBy<SmartOfferRepository>(),
					Component.For<IOrderFactory>().ImplementedBy<global::SmartOrderFactory.SmartOrderFactory>()));
		}
	}
}