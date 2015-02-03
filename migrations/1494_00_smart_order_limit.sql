create table OrderSendRules.SmartOrderLimits (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Value DECIMAL(19,2) not null default 0,
       SupplierId INTEGER UNSIGNED,
       AddressId INTEGER UNSIGNED,
       primary key (Id)
    );
alter table OrderSendRules.SmartOrderLimits add index (SupplierId), add constraint FK_OrderSendRules_SmartOrderLimits_SupplierId foreign key (SupplierId) references Customers.Suppliers (Id) on delete cascade;
alter table OrderSendRules.SmartOrderLimits add index (AddressId), add constraint FK_OrderSendRules_SmartOrderLimits_AddressId foreign key (AddressId) references Customers.Addresses (Id) on delete cascade;
