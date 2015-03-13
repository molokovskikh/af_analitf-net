alter table Farm.Rejects
add index (UpdateTime);

alter table Logs.RejectLogs
add index (LogTime, Operation);
