alter table Customers.Users add column CheckClientToken TINYINT(1) default 0 not null;
alter table Customers.AnalitfNetDatas add column ClientToken VARCHAR(255);
alter table Customers.AnalitfNetDatas add column ClientVersion VARCHAR(255);
