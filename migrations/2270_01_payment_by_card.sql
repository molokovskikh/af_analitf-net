alter  table Inventory.Checks
add column PaymentByCard DECIMAL(19,5) default 0  not null;
