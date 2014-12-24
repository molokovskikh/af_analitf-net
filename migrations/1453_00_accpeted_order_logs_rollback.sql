alter table Logs.AcceptedOrderLogs drop foreign key FK_Logs_AcceptedOrderLogs_OrderId;
alter table Logs.AcceptedOrderLogs drop foreign key FK_Logs_AcceptedOrderLogs_RequestId;
drop table if exists Logs.AcceptedOrderLogs;
