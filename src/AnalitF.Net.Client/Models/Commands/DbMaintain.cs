using System;
using System.Threading;
using Common.Tools;
using NHibernate;

namespace AnalitF.Net.Client.Models.Commands
{
	public class DbMaintain
	{
		public static void UpdateLeaders(IStatelessSession statelessSession, Settings settings)
		{
			statelessSession.CreateSQLQuery(@"
update Prices p
	join DelayOfPayments d on d.PriceId = p.PriceId and p.RegionId = d.RegionId and d.DayOfWeek = :dayOfWeek
set p.CostFactor = ifnull(1 + d.OtherDelay / 100, 1),
	p.VitallyImportantCostFactor = ifnull(1 + d.VitallyImportantDelay / 100, 1);

drop temporary table if exists Leaders;
create temporary table Leaders (
	ProductId int unsigned,
	PriceId int unsigned,
	RegionId bigint unsigned,
	Cost decimal(8,2),
	index (ProductId, RegionId, PriceId)
);

insert into Leaders(ProductId, RegionId, Cost)
select o.ProductId,
	o.RegionId,
	min(if(d.Id is null, o.Cost, o.Cost * (1 + if(o.VitallyImportant, d.VitallyImportantDelay, d.OtherDelay) / 100))) as Cost
from Offers o
	left join DelayOfPayments d on d.PriceId = o.PriceId and d.RegionId = o.RegionId and d.DayOfWeek = :dayOfWeek
where o.Junk = 0
group by o.ProductId, o.RegionId;

update Leaders l
	join Offers o on o.RegionId = l.RegionId and o.ProductId = l.ProductId
	left join DelayOfPayments d on d.PriceId = o.PriceId and d.RegionId = o.RegionId and d.DayOfWeek = :dayOfWeek
set l.PriceId = o.PriceId
where l.Cost = round(if(d.Id is null, o.Cost, o.Cost * (1 + if(o.VitallyImportant, d.VitallyImportantDelay, d.OtherDelay) / 100)), 2);

update Offers o
	join Leaders l on o.ProductId = l.ProductId and o.RegionId = l.RegionId
set o.LeaderPriceId = l.PriceId, o.LeaderRegionId = l.RegionId, o.LeaderCost = l.Cost;

drop temporary table Leaders;")
				.SetParameter("dayOfWeek", DateTime.Today.DayOfWeek)
				.ExecuteUpdate();
			settings.LastLeaderCalculation = DateTime.Today;
		}
	}
}