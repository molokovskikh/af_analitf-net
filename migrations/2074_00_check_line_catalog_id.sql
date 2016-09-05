alter table Inventory.CheckLines
add column CatalogId int unsigned,
add column SerialNumber VARCHAR(255),
add column Certificates VARCHAR(255),
add column ProducerCost DECIMAL(19,5),
add column RegistryCost DECIMAL(19,5),
add column RetailMarkup DECIMAL(19,5),
add column SupplierCost DECIMAL(19,5),
add column SupplierCostWithoutNds DECIMAL(19,5),
add column SupplierPriceMarkup DECIMAL(19,5),
add column ExciseTax DECIMAL(19,5),
add column BillOfEntryNumber VARCHAR(255),
add column VitallyImportant TINYINT(1),
add column SupplyQuantity DECIMAL(19,5) default 0  not null;
