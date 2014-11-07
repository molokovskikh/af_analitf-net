alter table logs.DescriptionLogs
add index LogTimeAndOperation (LogTime, Operation);
