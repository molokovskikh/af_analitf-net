alter table OrderSendRules.SmartOrderLimits drop foreign key FK_OrderSendRules_SmartOrderLimits_AddressId;
alter table OrderSendRules.SmartOrderLimits drop foreign key FK_OrderSendRules_SmartOrderLimits_SupplierId;
drop table if exists OrderSendRules.SmartOrderLimits;
