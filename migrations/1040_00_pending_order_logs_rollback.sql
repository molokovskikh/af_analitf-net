alter table Logs.PendingOrderLogs drop foreign key FK_Logs_PendingOrderLogs_OrderId;
alter table Logs.PendingOrderLogs drop foreign key FK_Logs_PendingOrderLogs_UserId;
drop table if exists Logs.PendingOrderLogs;
