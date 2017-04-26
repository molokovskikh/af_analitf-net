using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Common.Models;
using Common.Models.Repositories;
using NHibernate;
using SmartOrderFactory;
using SmartOrderFactory.Repositories;

namespace AnalitF.Net.Service.Config.Initializers
{
	public class SmartOrderFactory
	{
		public void Init(ISessionFactory factory)
		{
			IoC.Initialize(new WindsorContainer()
				.Register(
					Component.For<ISessionFactoryHolder>().Instance(new SessionFactoryHolder(factory)),
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