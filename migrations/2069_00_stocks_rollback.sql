alter table Inventory.Stocks drop foreign key FK_Inventory_Stocks_CreatedByUserId;
alter table Inventory.Stocks drop foreign key FK_Inventory_Stocks_AddressId;
drop table if exists Inventory.Stocks;
