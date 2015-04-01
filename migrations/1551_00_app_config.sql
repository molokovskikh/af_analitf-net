use customers;
create table AppConfig
(
	Id int unsigned not null auto_increment,
	`Key` varchar(255),
	`Value` varchar(255),
	primary key(Id)
)
