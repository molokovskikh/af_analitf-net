create table Customers.AnalitFNetPriceReplications
(
	Id int unsigned not null auto_increment,
	PriceId int unsigned not null,
	UserId int(10) unsigned not null,
	UpdateTime datetime not null,
	primary key(Id)
);
alter table Customers.AnalitFNetPriceReplications
add constraint FK_AnalitFNetPriceReplications_PriceId foreign key (PriceId) references UserSettings.PricesData(PriceCode) on delete cascade,
add constraint FK_AnalitFNetPriceReplications_UserId foreign key (UserId) references Customers.Users(Id) on delete cascade;

CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER AnalitFReplicationInfoUpdate AFTER UPDATE ON UserSettings.AnalitFReplicationInfo
FOR EACH ROW BEGIN
	if NEW.ForceReplication = 1 then
		update Customers.AnalitFNetPriceReplications r
			join UserSettings.PricesData pd on pd.PriceCode = r.PriceId
		set UpdateTime = now()
		where pd.FirmCode = OLD.FirmCode
			and r.UserId = OLD.UserId;
	end if;
END
