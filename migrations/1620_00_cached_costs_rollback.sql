alter table Farm.CachedCosts drop foreign key FK_Farm_CachedCosts_KeyId;
alter table Farm.CachedCosts drop foreign key FK_Farm_CachedCosts_CoreId;
drop table if exists Farm.CachedCosts;
alter table Farm.CachedCostKeys drop foreign key FK_Farm_CachedCostKeys_PriceId;
alter table Farm.CachedCostKeys drop foreign key FK_Farm_CachedCostKeys_ClientId;
alter table Farm.CachedCostKeys drop foreign key FK_Farm_CachedCostKeys_UserId;
alter table Farm.CachedCostKeys drop foreign key FK_Farm_CachedCostKeys_RegionId;
drop table if exists Farm.CachedCostKeys;
