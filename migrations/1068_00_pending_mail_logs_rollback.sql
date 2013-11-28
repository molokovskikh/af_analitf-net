alter table Logs.PendingMailLogs drop foreign key FK_Logs_PendingMailLogs_UserId;
alter table Logs.PendingMailLogs drop foreign key FK_Logs_PendingMailLogs_SendLogId;
drop table if exists Logs.PendingMailLogs;
