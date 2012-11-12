alter table logs.RequestLogs drop foreign key FK_logs_RequestLogs_UserId;
drop table if exists logs.RequestLogs;
