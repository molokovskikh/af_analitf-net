create table Inventory.StockedWaybills
(
	Id int unsigned not null auto_increment,
	UserId int unsigned,
	DownloadId int unsigned,
	ClientTimestamp datetime not null,
	Timestamp timestamp not null default current_timestamp,
	primary key(Id)
);
