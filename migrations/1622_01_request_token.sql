alter table Logs.ClientAppLogs add column RequestToken VARCHAR(255);
alter table Logs.RequestLogs add column RequestToken VARCHAR(255);
