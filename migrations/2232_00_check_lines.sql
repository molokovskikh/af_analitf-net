alter table Inventory.CheckLines
add column WaybillLineId int unsigned,
add column CatalogId int unsigned,
add column SerialNumber varchar(255),
add column `Certificates` varchar(255) DEFAULT NULL,
add column `ProducerCost` decimal(19,5) DEFAULT NULL,
add column `RegistryCost` decimal(19,5) DEFAULT NULL,
add column `RetailMarkup` decimal(19,5) DEFAULT NULL,
add column `SupplierCost` decimal(19,5) DEFAULT NULL,
add column `SupplierCostWithoutNds` decimal(19,5) DEFAULT NULL,
add column `SupplierPriceMarkup` decimal(19,5) DEFAULT NULL,
add column `ExciseTax` decimal(19,5) DEFAULT NULL,
add column `BillOfEntryNumber` varchar(255) DEFAULT NULL,
add column `VitallyImportant` tinyint(1) DEFAULT NULL,
add column `SupplyQuantity` decimal(19,5) NOT NULL DEFAULT '0.00000';

alter table Inventory.Checks
add column `AddressId` int(10) unsigned DEFAULT NULL,
add column `Payment` decimal(19,5) NOT NULL DEFAULT '0.00000',
add column `Charge` decimal(19,5) NOT NULL DEFAULT '0.00000';
