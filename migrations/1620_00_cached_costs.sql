create table Farm.CachedCostKeys (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       UserId INTEGER UNSIGNED,
       ClientId INTEGER UNSIGNED,
       PriceId INTEGER UNSIGNED,
       RegionId BIGINT UNSIGNED default 0  not null,
       Date DATETIME default '0001-01-01'  not null,
       primary key (Id)
    );
alter table Farm.CachedCostKeys add index (UserId), add constraint FK_Farm_CachedCostKeys_UserId foreign key (UserId) references Customers.Users (Id) on delete cascade;
alter table Farm.CachedCostKeys add index (ClientId), add constraint FK_Farm_CachedCostKeys_ClientId foreign key (ClientId) references Customers.Clients (Id) on delete cascade;
alter table Farm.CachedCostKeys add index (PriceId), add constraint FK_Farm_CachedCostKeys_PriceId foreign key (PriceId) references usersettings.pricesdata (PriceCode) on delete cascade;
alter table Farm.CachedCostKeys add index (RegionId), add constraint FK_Farm_CachedCostKeys_RegionId foreign key (RegionId) references Farm.Regions (RegionCode) on delete cascade;

create table Farm.CachedCosts (
	Id integer unsigned not null auto_increment,
	Cost decimal(12, 2) not null,
	CoreId bigint unsigned not null,
	KeyId int unsigned not null,
	primary key (Id)
);

alter table Farm.CachedCosts add index (KeyId), add constraint FK_Farm_CachedCosts_KeyId foreign key (KeyId) references Farm.CachedCostKeys(Id) on delete cascade;
alter table Farm.CachedCosts add index (CoreId), add constraint FK_Farm_CachedCosts_CoreId foreign key (CoreId) references Farm.Core0(Id) on delete cascade;
