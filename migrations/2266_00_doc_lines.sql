alter table Inventory.CheckLines
  add column `Period` varchar(255) DEFAULT NULL,
  add column `Exp` datetime DEFAULT NULL;

alter table Inventory.ReturnLines
  add column `Period` varchar(255) DEFAULT NULL,
  add column `Exp` datetime DEFAULT NULL;

alter table Inventory.UnpackingLines
  add column `Period` varchar(255) DEFAULT NULL,
  add column `Exp` datetime DEFAULT NULL;

alter table Inventory.InventoryLines
  add column `Exp` datetime DEFAULT NULL;

alter table Inventory.DisplacementLines
  add column `Exp` datetime DEFAULT NULL;
