alter table Logs.ClientAppLogs add column RequestId INTEGER UNSIGNED;
alter table Logs.ClientAppLogs add index (RequestId), add constraint FK_Logs_ClientAppLogs_RequestId foreign key (RequestId) references Logs.RequestLogs (Id) on delete set null;
