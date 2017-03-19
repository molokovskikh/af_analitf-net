use Inventory;
alter table Inventory.Checks
  add column `NumberDoc` VARCHAR(255) NOT NULL DEFAULT '0.00000';

alter table Inventory.displacementdocs
  add column `NumberDoc` VARCHAR(255) NOT NULL DEFAULT '0.00000';
  
alter table Inventory.inventorydocs
  add column `NumberDoc` VARCHAR(255) NOT NULL DEFAULT '0.00000';
  
alter table Inventory.reassessmentdocs
  add column `NumberDoc` VARCHAR(255) NOT NULL DEFAULT '0.00000';
  
alter table Inventory.returndocs
  add column `NumberDoc` VARCHAR(255) NOT NULL DEFAULT '0.00000';
    
alter table Inventory.unpackingdocs
  add column `NumberDoc` VARCHAR(255) NOT NULL DEFAULT '0.00000';
    
alter table Inventory.writeoffdocs
  add column `NumberDoc` VARCHAR(255) NOT NULL DEFAULT '0.00000';
  
alter table Inventory.inventorylines  
  add column `DocId` INT(10) UNSIGNED NULL DEFAULT NULL;

alter table Inventory.checklines  
  add column `DocId` INT(10) UNSIGNED NULL DEFAULT NULL;