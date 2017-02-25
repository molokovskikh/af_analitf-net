alter table Inventory.CheckLines
add column ClientDocId int unsigned,
add column UserId int unsigned,
add CONSTRAINT `FK_CheckLines_UserId` FOREIGN KEY (`UserId`) REFERENCES `Customers`.`Users` (`Id`)
on delete set null,
add index (ClientDocId);
