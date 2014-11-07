alter table Logs.MnnLogs
add index LogTimeAndOperation(LogTime, Operation);
