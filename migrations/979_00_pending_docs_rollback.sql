alter table Logs.PendingDocLogs drop foreign key FK_Logs_PendingDocLogs_UserId;
alter table Logs.PendingDocLogs drop foreign key FK_Logs_PendingDocLogs_SendLogId;
drop table if exists Logs.PendingDocLogs;
