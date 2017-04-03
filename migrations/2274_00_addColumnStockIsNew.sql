alter table Inventory.InventoryLines add column StockIsNew TINYINT(1) NOT NULL DEFAULT '0' after StockId;
