alter table Logs.PendingLimitLogs drop foreign key FK_Logs_PendingLimitLogs_LimitId;
alter table Logs.PendingLimitLogs drop foreign key FK_Logs_PendingLimitLogs_RequestId;
drop table if exists Logs.PendingLimitLogs;
