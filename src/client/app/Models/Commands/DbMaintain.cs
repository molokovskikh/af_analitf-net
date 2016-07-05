using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Tools;
using NHibernate;
using log4net;

namespace AnalitF.Net.Client.Models.Commands
{
	public class DbMaintain
	{
		public static void UpdateLeaders()
		{
			var statelessSession = AppBootstrapper.NHibernate?.Factory.OpenSession();
			var trancate = statelessSession?.BeginTransaction();
			try {
				statelessSession?.CreateSQLQuery(@"
update Prices p
	join DelayOfPayments d on d.PriceId = p.PriceId and p.RegionId = d.RegionId and d.DayOfWeek = :dayOfWeek
set p.CostFactor = ifnull(1 + d.OtherDelay / 100, 1),
	p.VitallyImportantCostFactor = ifnull(1 + d.VitallyImportantDelay / 100, 1);

drop temporary table if exists Leaders;
create temporary table Leaders (
	ProductId int unsigned,
	CatalogId int unsigned,
	PriceId int unsigned,
	RegionId bigint unsigned,
	Cost decimal(8,2),
	index (ProductId, RegionId)
) engine=memory;

insert into Leaders(ProductId, CatalogId, RegionId, Cost)
select o.ProductId,
	o.CatalogId,
	o.RegionId,
	min(round(o.Cost * if(o.VitallyImportant, p.VitallyImportantCostFactor, p.CostFactor), 2)) as Cost
from Offers o
	join Prices p on p.PriceId = o.PriceId and p.RegionId = o.RegionId
where o.Junk = 0
group by o.ProductId, o.RegionId;

update Leaders l
	join Offers o on o.RegionId = l.RegionId and o.ProductId = l.ProductId
	join Prices p on p.PriceId = o.PriceId and p.RegionId = o.RegionId
set l.PriceId = o.PriceId
where l.Cost = round(o.Cost * if(o.VitallyImportant, p.VitallyImportantCostFactor, p.CostFactor), 2);

update Offers o
	join Leaders l on o.ProductId = l.ProductId and o.RegionId = l.RegionId
set o.LeaderPriceId = l.PriceId, o.LeaderRegionId = l.RegionId, o.LeaderCost = l.Cost;

truncate table MinCosts;
insert into MinCosts(ProductId, CatalogId, Cost, PriceId, RegionId)
select l.ProductId, l.CatalogId, l.Cost, l.PriceId, l.RegionId
from Leaders l;

create temporary table NextMinCosts (
	NextCost decimal(8,2) unsigned,
	ProductId int unsigned,
	RegionId bigint unsigned,
	unique (ProductId, RegionId)
) engine = memory;

insert into NextMinCosts(NextCost, ProductId, RegionId)
select min(round(o.Cost * if(o.VitallyImportant, p.VitallyImportantCostFactor, p.CostFactor), 2)),
	m.ProductId,
	m.RegionId
from Offers o
	join Prices p on p.PriceId = o.PriceId and p.RegionId = o.RegionId
	join MinCosts m on m.ProductId = o.ProductId and m.RegionId = o.RegionId
where round(o.Cost * if(o.VitallyImportant, p.VitallyImportantCostFactor, p.CostFactor), 2) > m.Cost
	and o.Junk = 0
group by m.ProductId, m.RegionId;

update MinCosts m
	join NextMinCosts n on n.ProductId = m.ProductId and n.RegionId = m.RegionId
set m.NextCost = n.NextCost,
	m.Diff = round((n.NextCost / m.Cost - 1) * 100, 2);

drop temporary table NextMinCosts;
drop temporary table Leaders;

update Settings set  LastLeaderCalculation = :today
")
					.SetParameter("dayOfWeek", DateTime.Today.DayOfWeek)
					.SetParameter("today", DateTime.Today)
					.ExecuteUpdate();
				trancate?.Commit();
			} catch (Exception exc) {
				trancate?.Rollback();
				LogManager.GetLogger(typeof (DbMaintain)).Warn($"Не удалось вычислить лидеров во время импорта данных {DateTime.Now}", exc);
			} finally {
				statelessSession?.Close();
			}
		}


		public static void CalcJunk(IStatelessSession session, Settings settings)
		{
			session.CreateSQLQuery(@"
update Offers
set Junk = OriginalJunk or (Exp is not null and Exp < :end);

update OrderLines
set Junk = OriginalJunk or (Exp is not null and Exp < :end);")
				.SetParameter("end", DateTime.Now.AddMonths(settings.JunkPeriod))
				.ExecuteUpdate();
		}
	}
}