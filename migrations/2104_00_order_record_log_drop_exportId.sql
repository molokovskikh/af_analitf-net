alter table Logs.OrderRecordLogs drop column ExportId;
alter table Logs.OrderRecordLogs add column RequestId INT(10);