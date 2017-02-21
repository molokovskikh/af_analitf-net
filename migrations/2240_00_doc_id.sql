use Inventory;
alter table DisplacementLines
add column ClientDocId int unsigned,
add column UserId int unsigned,
add index (ClientDocId),
add CONSTRAINT `FK_DisplacementLines_UserId` FOREIGN KEY (`UserId`) REFERENCES `Customers`.`Users` (`Id`)
on delete set null,
add CONSTRAINT `FK_DisplacementLines_DocId` FOREIGN KEY (`DisplacementDocId`) REFERENCES `Inventory`.`DisplacementDocs` (`Id`)
on delete  cascade
;

alter table InventoryLines
add column ClientDocId int unsigned,
add column UserId int unsigned,
add index (ClientDocId),
add CONSTRAINT `FK_InventoryLines_UserId` FOREIGN KEY (`UserId`) REFERENCES `Customers`.`Users` (`Id`)
on delete set null,
add CONSTRAINT `FK_InventoryLines_DocId` FOREIGN KEY (`InventoryDocId`) REFERENCES `Inventory`.`InventoryDocs` (`Id`)
on delete  cascade
;

alter table ReassessmentLines
add column ClientDocId int unsigned,
add column UserId int unsigned,
add index (ClientDocId),
add CONSTRAINT `FK_ReassessmentLines_UserId` FOREIGN KEY (`UserId`) REFERENCES `Customers`.`Users` (`Id`)
on delete set null,
add CONSTRAINT `FK_ReassessmentLines_DocId` FOREIGN KEY (`ReassessmentDocId`) REFERENCES `Inventory`.`ReassessmentDocs` (`Id`)
on delete  cascade
;

alter table ReturnLines
add column ClientDocId int unsigned,
add column UserId int unsigned,
add index (ClientDocId),
add CONSTRAINT `FK_ReturnLines_UserId` FOREIGN KEY (`UserId`) REFERENCES `Customers`.`Users` (`Id`)
on delete set null,
add CONSTRAINT `FK_ReturnLines_DocId` FOREIGN KEY (`ReturnDocId`) REFERENCES `Inventory`.`ReturnDocs` (`Id`)
on delete  cascade
;

alter table UnpackingLines
add column ClientDocId int unsigned,
add column UserId int unsigned,
add index (ClientDocId),
add CONSTRAINT `FK_UnpackingLines_UserId` FOREIGN KEY (`UserId`) REFERENCES `Customers`.`Users` (`Id`)
on delete set null,
add CONSTRAINT `FK_UnpackingLines_DocId` FOREIGN KEY (`UnpackingDocId`) REFERENCES `Inventory`.`UnpackingDocs` (`Id`)
on delete  cascade
;

alter table WriteoffLines
add column ClientDocId int unsigned,
add column UserId int unsigned,
add index (ClientDocId),
add CONSTRAINT `FK_WriteoffLines_UserId` FOREIGN KEY (`UserId`) REFERENCES `Customers`.`Users` (`Id`)
on delete set null,
add CONSTRAINT `FK_WriteoffLines_DocId` FOREIGN KEY (`WriteoffDocId`) REFERENCES `Inventory`.`WriteoffDocs` (`Id`)
on delete  cascade
;
