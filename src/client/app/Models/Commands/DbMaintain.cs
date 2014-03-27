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
	index (ProductId, RegionId)
) engine=memory;

insert into Leaders(ProductId, RegionId, Cost)
select o.ProductId,
	o.RegionId,
	min(o.Cost * if(o.VitallyImportant, p.VitallyImportantCostFactor, p.CostFactor)) as Cost
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

drop temporary table Leaders;")
				.SetParameter("dayOfWeek", DateTime.Today.DayOfWeek)
				.ExecuteUpdate();
			settings.LastLeaderCalculation = DateTime.Today;
		}
	}
}