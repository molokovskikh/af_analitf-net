alter table Logs.ProducerLogs
add index LogTimeAndOperationIndex (LogTime, Operation);
